using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Application.Services;

public interface IRandomPickerService
{
    /// <summary>
    /// Picks one title at random from the user's list (of one type when the filter says so). When
    /// <paramref name="exclude"/> is set, that title is skipped unless it is the only match. Returns null when nothing matches.
    /// </summary>
    Task<MediaSummaryDto?> PickAsync(
        int userId,
        WatchListFilterDto? filter = null,
        MediaKey? exclude = null,
        CancellationToken cancellationToken = default);
}
