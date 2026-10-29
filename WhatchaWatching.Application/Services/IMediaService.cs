using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Application.Services;

public interface IMediaService
{
    /// <summary>
    /// Searches the providers of this type (Jikan, then AniList for anime; TMDB for movies and TV series). When none answers, searches the titles
    /// already stored on this site and says so in <see cref="MediaSearchResultDto.IsLocalOnly"/>.
    /// </summary>
    /// <param name="type">Anime, movie or TV series (TMDB provides the last two).</param>
    /// <param name="includeAdult">
    /// False (the default, and what anonymous visitors get) keeps adult titles out. Pass the user's own setting.
    /// </param>
    /// <exception cref="Exceptions.MediaProviderUnavailableException">No provider answered and no stored title matches.</exception>
    /// <exception cref="NotSupportedException">The type is neither anime, movie nor TV series.</exception>
    Task<MediaSearchResultDto> SearchAsync(
        MediaType type, string query, int page, bool includeAdult = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the title of this type with this provider id (the MyAnimeList id for an anime), from the local copy when
    /// it is recent enough, otherwise from the providers (which refreshes the local copy). Returns null when it does not
    /// exist, or is adult and <paramref name="includeAdult"/> is false (nothing is stored in that case).
    /// </summary>
    /// <exception cref="Exceptions.MediaProviderUnavailableException">No provider answered and nothing is stored.</exception>
    /// <exception cref="NotSupportedException">The type is neither anime, movie nor TV series.</exception>
    Task<MediaSummaryDto?> GetAsync(
        MediaType type, int externalId, bool includeAdult = false, CancellationToken cancellationToken = default);
}
