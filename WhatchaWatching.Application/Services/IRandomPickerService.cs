using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Application.Services;

public interface IRandomPickerService
{
    /// <summary>
    /// Picks one anime at random from the user's list. When <paramref name="excludeExternalId"/> is set,
    /// that anime is skipped unless it is the only match. Returns null when nothing matches.
    /// </summary>
    Task<AnimeSummaryDto?> PickAsync(
        int userId,
        WatchListFilterDto? filter = null,
        int? excludeExternalId = null,
        CancellationToken cancellationToken = default);
}
