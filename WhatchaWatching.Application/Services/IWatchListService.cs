using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Application.Services;

/// <summary>
/// A user's list of anime, movies and TV series to watch. A title is identified by its type and its provider id
/// (the Jikan id for an anime): the id alone is not unique.
/// </summary>
public interface IWatchListService
{
    /// <param name="filter">Its <see cref="WatchListFilterDto.Type"/> keeps one type of title; null keeps them all.</param>
    Task<IReadOnlyList<MediaSummaryDto>> GetAsync(
        int userId,
        WatchListFilterDto? filter = null,
        WatchListSort sort = WatchListSort.RecentlyAdded,
        CancellationToken cancellationToken = default);

    /// <summary>Names of the genres found in the user's list, sorted. Of one type of title, or of all when null.</summary>
    Task<IReadOnlyList<string>> GetGenresAsync(
        int userId, MediaType? type = null, CancellationToken cancellationToken = default);

    /// <summary>Names of the streaming platforms that offer the titles of the list, in the country of the user, sorted.</summary>
    Task<IReadOnlyList<string>> GetPlatformsAsync(
        int userId, MediaType? type = null, CancellationToken cancellationToken = default);

    /// <summary>Of the given provider ids, those of this type that are in the user's list.</summary>
    Task<IReadOnlySet<int>> GetListedIdsAsync(
        int userId, MediaType type, IReadOnlyCollection<int> externalIds, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.MediaProviderUnavailableException">The title is not stored and its provider cannot be reached.</exception>
    Task<WatchListAddResult> AddAsync(int userId, MediaType type, int externalId, CancellationToken cancellationToken = default);

    /// <returns>True when the title was in the list and has been removed.</returns>
    Task<bool> RemoveAsync(int userId, MediaType type, int externalId, CancellationToken cancellationToken = default);
}
