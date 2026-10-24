using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Components.Shared;

/// <summary>The labels of the filter and sort dropdowns, and what they mean for the watchlist services.</summary>
public static class FilterOptions
{
    private static readonly (string Label, DurationRange Range)[] Durations =
    [
        ($"Under {WatchListConstants.SHORT_DURATION_MAX_MINUTES} min", DurationRange.Short),
        ($"{WatchListConstants.SHORT_DURATION_MAX_MINUTES} - {WatchListConstants.LONG_DURATION_MIN_MINUTES} min", DurationRange.Medium),
        ($"Over {WatchListConstants.LONG_DURATION_MIN_MINUTES} min", DurationRange.Long)
    ];

    private static readonly (string Label, WatchListSort Sort)[] Sorts =
    [
        ("Recently added", WatchListSort.RecentlyAdded),
        ("Title (A-Z)", WatchListSort.Title),
        ("Shortest first", WatchListSort.ShortestFirst)
    ];

    public static IReadOnlyList<string> DurationLabels { get; } = Durations.Select(d => d.Label).ToList();

    public static IReadOnlyList<string> SortLabels { get; } = Sorts.Select(s => s.Label).ToList();

    public static WatchListFilterDto ToFilter(IReadOnlyCollection<string> genres, IReadOnlyCollection<string> duration) =>
        new(genres, Durations.FirstOrDefault(d => duration.Contains(d.Label)) is { Label: not null } match
            ? match.Range
            : null);

    public static WatchListSort ToSort(IReadOnlyCollection<string> sort) =>
        Sorts.FirstOrDefault(s => sort.Contains(s.Label)) is { Label: not null } match
            ? match.Sort
            : WatchListSort.RecentlyAdded;
}
