using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Middleware;
using WhatchaWatching.Middleware.AniList;
using WhatchaWatching.Middleware.Jikan;

namespace WhatchaWatching.Tests.TestSupport;

/// <summary>A provider that answers with a result, or with an exception, and counts its calls.</summary>
public class StubProvider : IAnimeProvider
{
    public int Calls { get; private set; }

    public Exception? Failure { get; set; }

    public ExternalAnimeDto? Anime { get; set; }

    public Task<PagedResultDto<ExternalAnimeDto>> SearchAnimeAsync(string query, int page, CancellationToken cancellationToken = default)
    {
        Calls++;
        if (Failure is not null)
        {
            throw Failure;
        }

        IReadOnlyList<ExternalAnimeDto> items = Anime is null ? [] : [Anime];
        return Task.FromResult(new PagedResultDto<ExternalAnimeDto>(items, page, 1, items.Count));
    }

    public Task<ExternalAnimeDto?> GetAnimeAsync(int externalId, CancellationToken cancellationToken = default)
    {
        Calls++;
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(Anime);
    }
}

public sealed class StubJikanClient : StubProvider, IJikanClient
{
    public Task<IReadOnlyList<ExternalGenreDto>> GetGenresAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ExternalGenreDto>>([]);
}

public sealed class StubAniListClient : StubProvider, IAniListClient;
