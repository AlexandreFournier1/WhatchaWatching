using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware;
using WhatchaWatching.Middleware.Tmdb;

namespace WhatchaWatching.Application.Services;

public class MediaExtrasService(
    ITmdbClient tmdbClient,
    IMemoryCache cache,
    ILogger<MediaExtrasService> logger) : IMediaExtrasService
{
    // Cast and suggestions change slowly, and every visitor of a popular title would ask for the same thing.
    private static readonly TimeSpan CacheFor = TimeSpan.FromHours(24);

    public async Task<MediaExtrasDto> GetAsync(
        MediaType type, int externalId, bool includeAdult = false, CancellationToken cancellationToken = default)
    {
        // Only TMDB has them, and a zero or negative id is never one of its titles.
        if (type is not (MediaType.Movie or MediaType.TVShow) || externalId <= 0)
        {
            return MediaExtrasDto.Empty;
        }

        var key = $"extras:{type}:{externalId}";
        if (!cache.TryGetValue(key, out MediaExtrasDto? extras) || extras is null)
        {
            try
            {
                extras = await tmdbClient.GetExtrasAsync(type, externalId, cancellationToken) ?? MediaExtrasDto.Empty;
            }
            catch (ExternalApiException ex)
            {
                // Not kept: the next visit asks again.
                logger.LogWarning(ex, "The extras of {Type} {ExternalId} could not be read.", type, externalId);
                return MediaExtrasDto.Empty;
            }

            cache.Set(key, extras, CacheFor);
        }

        // The adult titles are kept in the cache and left out here, because the answer depends on who asks.
        return includeAdult || extras.Similar.All(s => !s.IsAdult)
            ? extras
            : extras with { Similar = extras.Similar.Where(s => !s.IsAdult).ToList() };
    }
}
