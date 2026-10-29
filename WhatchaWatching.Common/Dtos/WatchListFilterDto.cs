using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Common.Dtos;

/// <summary>Narrows a watchlist: a title matches when it has any of the genres and when every other condition holds.</summary>
public record WatchListFilterDto(IReadOnlyCollection<string> Genres, DurationRange? Duration)
{
    public static readonly WatchListFilterDto None = new([], null);

    /// <summary>Keeps one type of title (anime, movie or series). Null keeps them all.</summary>
    public MediaType? Type { get; init; }

    /// <summary>Keeps the titles scored at least this (out of 10). A title without a score never matches.</summary>
    public decimal? MinScore { get; init; }

    /// <summary>Keeps the titles offered by any of these platforms (names, in the country of <see cref="Region"/>).</summary>
    public IReadOnlyCollection<string> Platforms { get; init; } = [];

    /// <summary>
    /// The country whose offers count for <see cref="Platforms"/> (an ISO code). The services fill it with the country of
    /// the user, so a caller does not need to.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>Keeps the titles that came out in this period. A title without a year never matches.</summary>
    public ReleasePeriod? Period { get; init; }

    /// <summary>
    /// False leaves adult titles out even for a user who turned adult content on (the switch next to the search and the
    /// random pick). True never shows more than the profile allows: a user who did not turn it on never sees them.
    /// </summary>
    public bool IncludeAdult { get; init; } = true;
}
