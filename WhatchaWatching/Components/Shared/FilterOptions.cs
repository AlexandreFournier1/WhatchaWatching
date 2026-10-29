using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Services.Localization;

namespace WhatchaWatching.Components.Shared;

/// <summary>
/// The choices of the filter and sort dropdowns, and what they mean for the watchlist services. The values are stable
/// keys; the label shown for each is the value itself, until the page translates it.
/// </summary>
public static class FilterOptions
{
    private static readonly (decimal Score, string Value)[] Scores =
    [
        (7m, "7 and above"),
        (8m, "8 and above"),
        (9m, "9 and above")
    ];

    private static readonly (string Value, ReleasePeriod Period)[] Periods =
    [
        ("2020 and after", ReleasePeriod.Since2020),
        ("2010 - 2019", ReleasePeriod.From2010To2019),
        ("2000 - 2009", ReleasePeriod.From2000To2009),
        ("Before 2000", ReleasePeriod.Before2000)
    ];

    private static readonly (string Value, WatchListSort Sort)[] Sorts =
    [
        ("Recently added", WatchListSort.RecentlyAdded),
        ("Title (A-Z)", WatchListSort.Title),
        ("Shortest first", WatchListSort.ShortestFirst),
        ("Best rated", WatchListSort.BestRated),
        ("Newest first", WatchListSort.Newest)
    ];

    /// <summary>The three lengths, for a type: under 15 minutes is short for an anime episode, under 90 for a movie.</summary>
    public static IReadOnlyList<string> DurationValues(MediaType type)
    {
        var (shortMax, longMin) = WatchListConstants.DurationLimits(type);

        return [$"Under {shortMax} min", $"{shortMax} - {longMin} min", $"Over {longMin} min"];
    }

    public static IReadOnlyList<string> ScoreValues { get; } = Scores.Select(s => s.Value).ToList();

    public static IReadOnlyList<string> PeriodValues { get; } = Periods.Select(p => p.Value).ToList();

    public static IReadOnlyList<string> SortValues { get; } = Sorts.Select(s => s.Value).ToList();

    /// <summary>Names that are shown as they are (genres and platforms).</summary>
    public static IReadOnlyList<FilterOption> Plain(IEnumerable<string> values) => FilterOption.OfNames(values);

    /// <summary>Fixed choices, shown in the language of the visit.</summary>
    public static IReadOnlyList<FilterOption> Localized(IEnumerable<string> values, ILocalizer localizer) =>
        FilterOption.Translated(values, localizer);

    /// <summary>The filter a page has built from the choices of its dropdowns, for one type of title.</summary>
    public static WatchListFilterDto ToFilter(
        MediaType type,
        IReadOnlyCollection<string> genres,
        IReadOnlyCollection<string> duration,
        IReadOnlyCollection<string>? score = null,
        IReadOnlyCollection<string>? period = null,
        IReadOnlyCollection<string>? platforms = null)
    {
        var durations = DurationValues(type);
        DurationRange? range = null;
        for (var index = 0; index < durations.Count; index++)
        {
            if (duration.Contains(durations[index]))
            {
                range = (DurationRange)index;
                break;
            }
        }

        return new WatchListFilterDto(genres, range)
        {
            Type = type,
            Platforms = platforms ?? [],
            MinScore = score is null ? null : Scores.FirstOrDefault(s => score.Contains(s.Value)) is { Value: not null } scored ? scored.Score : null,
            Period = period is null ? null : Periods.FirstOrDefault(p => period.Contains(p.Value)) is { Value: not null } matched ? matched.Period : null
        };
    }

    public static WatchListSort ToSort(IReadOnlyCollection<string> sort) =>
        Sorts.FirstOrDefault(s => sort.Contains(s.Value)) is { Value: not null } match
            ? match.Sort
            : WatchListSort.RecentlyAdded;
}
