using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Application.Services;

public interface IAnimeService
{
    /// <summary>
    /// Searches the anime providers (Jikan, then AniList). When none answers, searches the anime already stored
    /// on this site and says so in <see cref="AnimeSearchResultDto.IsLocalOnly"/>.
    /// </summary>
    /// <exception cref="Exceptions.AnimeProviderUnavailableException">No provider answered and no stored anime matches.</exception>
    Task<AnimeSearchResultDto> SearchAsync(string query, int page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the anime with this MyAnimeList id, from the local copy when it is recent enough, otherwise from the
    /// providers (which refreshes the local copy). Returns null when the anime does not exist.
    /// </summary>
    /// <exception cref="Exceptions.AnimeProviderUnavailableException">No provider answered and nothing is stored.</exception>
    Task<AnimeSummaryDto?> GetAsync(int externalId, CancellationToken cancellationToken = default);
}
