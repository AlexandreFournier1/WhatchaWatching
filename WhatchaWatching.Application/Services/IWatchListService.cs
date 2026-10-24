using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Application.Services;

/// <summary>A user's list of anime to watch. Anime are identified by their Jikan id.</summary>
public interface IWatchListService
{
    Task<IReadOnlyList<AnimeSummaryDto>> GetAsync(
        int userId,
        WatchListFilterDto? filter = null,
        WatchListSort sort = WatchListSort.RecentlyAdded,
        CancellationToken cancellationToken = default);

    /// <summary>Names of the genres found in the user's list, sorted.</summary>
    Task<IReadOnlyList<string>> GetGenresAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Of the given anime ids, those that are in the user's list.</summary>
    Task<IReadOnlySet<int>> GetListedIdsAsync(
        int userId, IReadOnlyCollection<int> externalIds, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.AnimeProviderUnavailableException">The anime is not stored and Jikan cannot be reached.</exception>
    Task<WatchListAddResult> AddAsync(int userId, int externalId, CancellationToken cancellationToken = default);

    /// <returns>True when the anime was in the list and has been removed.</returns>
    Task<bool> RemoveAsync(int userId, int externalId, CancellationToken cancellationToken = default);
}
