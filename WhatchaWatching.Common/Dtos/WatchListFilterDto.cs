using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Common.Dtos;

/// <summary>Narrows a watchlist: an anime matches when it has any of the genres and the duration fits.</summary>
public record WatchListFilterDto(IReadOnlyCollection<string> Genres, DurationRange? Duration)
{
    public static readonly WatchListFilterDto None = new([], null);
}
