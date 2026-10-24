using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Middleware.Jikan;

public interface IJikanClient
{
    Task<PagedResultDto<ExternalAnimeDto>> SearchAnimeAsync(string query, int page, CancellationToken cancellationToken = default);

    /// <summary>Returns null when Jikan has no anime with this id.</summary>
    Task<ExternalAnimeDto?> GetAnimeAsync(int externalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalGenreDto>> GetGenresAsync(CancellationToken cancellationToken = default);
}
