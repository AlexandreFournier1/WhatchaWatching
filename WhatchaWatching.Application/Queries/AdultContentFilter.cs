using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Application.Queries;

internal static class AdultContentFilter
{
    /// <summary>Hides adult anime from a list unless its user chose to see them.</summary>
    public static IQueryable<WatchListItemEntity> ApplyAdultContent(
        this IQueryable<WatchListItemEntity> items, bool showAdult) =>
        showAdult ? items : items.Where(i => !i.Media.IsAdult);
}
