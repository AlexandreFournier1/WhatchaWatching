using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Application.Services;

/// <summary>
/// Anime that a user adds by hand because the providers do not have them. Each one is private to the user who
/// created it: it has a negative id (the providers' ids are positive) and only appears in that user's list.
/// </summary>
public interface ICustomAnimeService
{
    /// <summary>Creates the anime and puts it in the user's list.</summary>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
    /// A value is invalid, a genre is unknown, or the user reached the limit of anime added by hand.
    /// </exception>
    Task<AnimeSummaryDto> CreateAsync(int userId, CustomAnimeDto input, CancellationToken cancellationToken = default);

    /// <summary>Returns the user's own anime with this id, or null (another user's anime is never returned).</summary>
    Task<AnimeSummaryDto?> GetAsync(int userId, int externalId, CancellationToken cancellationToken = default);
}
