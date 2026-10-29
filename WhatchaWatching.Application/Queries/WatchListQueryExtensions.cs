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

        if (filter.Type is { } type)
        {
            items = items.Where(i => i.Media.Type == type);
        }

        if (filter.Genres.Count > 0)
        {
            var genres = filter.Genres.ToList();
            items = items.Where(i => i.Media.MediaGenres.Any(mg => genres.Contains(mg.Genre.Name)));
        }

        if (filter.Platforms.Count > 0)
        {
            // An offer without a country (an anime) counts everywhere; the others only in the country asked for.
            var platforms = filter.Platforms.ToList();
            var region = filter.Region;
            items = items.Where(i => i.Media.StreamingLinks.Any(l =>
                platforms.Contains(l.Site) && (l.Region == null || l.Region == region)));
        }

        // A title without a score (or a year, or a duration) never matches a condition on it.
        if (filter.MinScore is { } minScore)
        {
            items = items.Where(i => i.Media.Score >= minScore);
        }

        items = ApplyPeriod(items, filter.Period);

        return ApplyDuration(items, filter.Duration);
    }

    private static IQueryable<WatchListItemEntity> ApplyPeriod(IQueryable<WatchListItemEntity> items, ReleasePeriod? period) =>
        period switch
        {
            ReleasePeriod.Since2020 => items.Where(i => i.Media.Year >= WatchListConstants.RECENT_FROM_YEAR),
            ReleasePeriod.From2010To2019 => items.Where(i =>
                i.Media.Year >= WatchListConstants.TWENTY_TEN_FROM_YEAR && i.Media.Year < WatchListConstants.RECENT_FROM_YEAR),
            ReleasePeriod.From2000To2009 => items.Where(i =>
                i.Media.Year >= WatchListConstants.TWO_THOUSAND_FROM_YEAR && i.Media.Year < WatchListConstants.TWENTY_TEN_FROM_YEAR),
            ReleasePeriod.Before2000 => items.Where(i => i.Media.Year < WatchListConstants.TWO_THOUSAND_FROM_YEAR),
            _ => items
        };

    // What is short is not the same for a movie, a series episode and an anime episode: each type has its own limits.
    private static IQueryable<WatchListItemEntity> ApplyDuration(IQueryable<WatchListItemEntity> items, DurationRange? duration)
    {
        var (animeShort, animeLong) = WatchListConstants.DurationLimits(MediaType.Anime);
        var (seriesShort, seriesLong) = WatchListConstants.DurationLimits(MediaType.TVShow);
        var (movieShort, movieLong) = WatchListConstants.DurationLimits(MediaType.Movie);

        return duration switch
        {
            DurationRange.Short => items.Where(i =>
                (i.Media.Type == MediaType.Movie && i.Media.EpisodeDurationMinutes < movieShort) ||
                (i.Media.Type == MediaType.TVShow && i.Media.EpisodeDurationMinutes < seriesShort) ||
                (i.Media.Type != MediaType.Movie && i.Media.Type != MediaType.TVShow && i.Media.EpisodeDurationMinutes < animeShort)),
            DurationRange.Medium => items.Where(i =>
                (i.Media.Type == MediaType.Movie && i.Media.EpisodeDurationMinutes >= movieShort && i.Media.EpisodeDurationMinutes <= movieLong) ||
                (i.Media.Type == MediaType.TVShow && i.Media.EpisodeDurationMinutes >= seriesShort && i.Media.EpisodeDurationMinutes <= seriesLong) ||
                (i.Media.Type != MediaType.Movie && i.Media.Type != MediaType.TVShow &&
                 i.Media.EpisodeDurationMinutes >= animeShort && i.Media.EpisodeDurationMinutes <= animeLong)),
            DurationRange.Long => items.Where(i =>
                (i.Media.Type == MediaType.Movie && i.Media.EpisodeDurationMinutes > movieLong) ||
                (i.Media.Type == MediaType.TVShow && i.Media.EpisodeDurationMinutes > seriesLong) ||
                (i.Media.Type != MediaType.Movie && i.Media.Type != MediaType.TVShow && i.Media.EpisodeDurationMinutes > animeLong)),
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
        WatchListSort.BestRated => items
            .OrderBy(i => i.Media.Score == null)
            .ThenByDescending(i => i.Media.Score)
            .ThenBy(i => i.Id),
        WatchListSort.Newest => items
            .OrderBy(i => i.Media.Year == null)
            .ThenByDescending(i => i.Media.Year)
            .ThenBy(i => i.Id),
        _ => items.OrderByDescending(i => i.AddedAt).ThenByDescending(i => i.Id)
    };
}
