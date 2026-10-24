using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Application.Queries;

internal static class WatchListQueryExtensions
{
    public static IQueryable<WatchListItemEntity> ForUser(this IQueryable<WatchListItemEntity> items, int userId) =>
        items.Where(i => i.UserId == userId);

    public static IQueryable<WatchListItemEntity> ApplyFilter(
        this IQueryable<WatchListItemEntity> items, WatchListFilterDto? filter)
    {
        if (filter is null)
        {
            return items;
        }

        if (filter.Genres.Count > 0)
        {
            var genres = filter.Genres.ToList();
            items = items.Where(i => i.Media.MediaGenres.Any(mg => genres.Contains(mg.Genre.Name)));
        }

        // An anime without a known duration never matches a duration filter.
        return filter.Duration switch
        {
            DurationRange.Short => items.Where(i =>
                i.Media.EpisodeDurationMinutes < WatchListConstants.SHORT_DURATION_MAX_MINUTES),
            DurationRange.Medium => items.Where(i =>
                i.Media.EpisodeDurationMinutes >= WatchListConstants.SHORT_DURATION_MAX_MINUTES &&
                i.Media.EpisodeDurationMinutes <= WatchListConstants.LONG_DURATION_MIN_MINUTES),
            DurationRange.Long => items.Where(i =>
                i.Media.EpisodeDurationMinutes > WatchListConstants.LONG_DURATION_MIN_MINUTES),
            _ => items
        };
    }

    public static IQueryable<WatchListItemEntity> ApplySort(
        this IQueryable<WatchListItemEntity> items, WatchListSort sort) => sort switch
    {
        WatchListSort.Title => items.OrderBy(i => i.Media.Title).ThenBy(i => i.Id),
        WatchListSort.ShortestFirst => items
            .OrderBy(i => i.Media.EpisodeDurationMinutes == null)
            .ThenBy(i => i.Media.EpisodeDurationMinutes)
            .ThenBy(i => i.Id),
        _ => items.OrderByDescending(i => i.AddedAt).ThenByDescending(i => i.Id)
    };
}
