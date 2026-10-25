using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Common.Dtos;

/// <summary>Narrows a watchlist: an anime matches when it has any of the genres and the duration fits.</summary>
public record WatchListFilterDto(IReadOnlyCollection<string> Genres, DurationRange? Duration)
{
    public static readonly WatchListFilterDto None = new([], null);

    /// <summary>
    /// False leaves adult anime out even for a user who turned adult content on (the switch next to the search and the
    /// random pick). True never shows more than the profile allows: a user who did not turn it on never sees them.
    /// </summary>
    public bool IncludeAdult { get; init; } = true;
}
