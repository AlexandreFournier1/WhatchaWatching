using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Middleware;

/// <summary>A source of anime data. Anime are identified by their MyAnimeList id, which Jikan and AniList both know.</summary>
public interface IAnimeProvider
{
    /// <exception cref="ExternalApiException">The provider cannot be reached or fails.</exception>
    /// <param name="includeAdult">False asks the provider for safe-for-work anime only.</param>
    Task<PagedResultDto<ExternalAnimeDto>> SearchAnimeAsync(
        string query, int page, bool includeAdult = false, CancellationToken cancellationToken = default);

    /// <summary>Returns null when the provider has no anime with this id.</summary>
    /// <exception cref="ExternalApiException">The provider cannot be reached or fails.</exception>
    Task<ExternalAnimeDto?> GetAnimeAsync(int externalId, CancellationToken cancellationToken = default);
}
