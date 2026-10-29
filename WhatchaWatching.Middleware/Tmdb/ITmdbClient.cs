using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Middleware.Tmdb;

/// <summary>TMDB, the source of movies and TV series. Both are identified by their TMDB id, which is only unique per type.</summary>
public interface ITmdbClient
{
    /// <param name="type"><see cref="MediaType.Movie"/> or <see cref="MediaType.TVShow"/>.</param>
    /// <param name="includeAdult">False asks TMDB to leave adult titles out.</param>
    /// <exception cref="TmdbApiException">TMDB is not configured, cannot be reached or fails.</exception>
    Task<PagedResultDto<ExternalMediaDto>> SearchAsync(
        MediaType type, string query, int page, bool includeAdult = false, CancellationToken cancellationToken = default);

    /// <summary>Returns null when TMDB has no title of this type with this id.</summary>
    /// <exception cref="TmdbApiException">TMDB is not configured, cannot be reached or fails.</exception>
    Task<ExternalMediaDto?> GetAsync(MediaType type, int externalId, CancellationToken cancellationToken = default);
}
