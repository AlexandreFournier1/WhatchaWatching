using Microsoft.Extensions.Logging;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Middleware.AniList;
using WhatchaWatching.Middleware.Jikan;

namespace WhatchaWatching.Middleware;

/// <summary>
/// Asks Jikan first and AniList when Jikan fails. After a failure Jikan is skipped for a minute, unless AniList
/// fails too. A "not found" from Jikan is an answer, not a failure: it does not trigger the fallback.
/// </summary>
public sealed class FallbackAnimeProvider(
    IJikanClient jikan,
    IAniListClient aniList,
    ProviderCooldown cooldown,
    ILogger<FallbackAnimeProvider> logger) : IAnimeProvider
{
    private const string JIKAN = "Jikan";
    private const string ANILIST = "AniList";

    public Task<PagedResultDto<ExternalAnimeDto>> SearchAnimeAsync(
        string query, int page, CancellationToken cancellationToken = default) =>
        AskAsync(provider => provider.SearchAnimeAsync(query, page, cancellationToken));

    public Task<ExternalAnimeDto?> GetAnimeAsync(int externalId, CancellationToken cancellationToken = default) =>
        AskAsync(provider => provider.GetAnimeAsync(externalId, cancellationToken));

    private async Task<T> AskAsync<T>(Func<IAnimeProvider, Task<T>> call)
    {
        (string Name, IAnimeProvider Provider)[] order = cooldown.IsCoolingDown(JIKAN)
            ? [(ANILIST, aniList), (JIKAN, jikan)]
            : [(JIKAN, jikan), (ANILIST, aniList)];

        var failures = new List<Exception>();
        foreach (var (name, provider) in order)
        {
            try
            {
                var result = await call(provider);
                if (name == JIKAN)
                {
                    cooldown.Clear(JIKAN);
                }

                return result;
            }
            catch (ExternalApiException ex)
            {
                failures.Add(ex);
                if (name == JIKAN)
                {
                    cooldown.Trip(JIKAN);
                }

                logger.LogWarning(ex, "{Provider} did not answer.", name);
            }
        }

        throw new ExternalApiException("No anime provider answered.", innerException: new AggregateException(failures));
    }
}
