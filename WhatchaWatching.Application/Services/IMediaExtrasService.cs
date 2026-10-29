using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Application.Services;

public interface IMediaExtrasService
{
    /// <summary>
    /// The trailer, the cast, the seasons and similar titles of a movie or a series. They are read from TMDB when a
    /// title page is opened and kept in memory for a day, not stored. It never throws: a page is complete without them,
    /// so an anime, an unknown title and a provider that does not answer all give <see cref="MediaExtrasDto.Empty"/>.
    /// </summary>
    /// <param name="includeAdult">False leaves adult titles out of the similar ones.</param>
    Task<MediaExtrasDto> GetAsync(
        MediaType type, int externalId, bool includeAdult = false, CancellationToken cancellationToken = default);
}
