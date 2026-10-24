using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Application.Services;

public interface IAnimeService
{
    /// <exception cref="Exceptions.AnimeProviderUnavailableException">Jikan cannot be reached.</exception>
    Task<PagedResultDto<AnimeSummaryDto>> SearchAsync(string query, int page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the anime with this Jikan id, from the local copy when it is recent enough, otherwise from Jikan
    /// (which refreshes the local copy). Returns null when the anime does not exist.
    /// </summary>
    /// <exception cref="Exceptions.AnimeProviderUnavailableException">Jikan cannot be reached and nothing is stored.</exception>
    Task<AnimeSummaryDto?> GetAsync(int externalId, CancellationToken cancellationToken = default);
}
