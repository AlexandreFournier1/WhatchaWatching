using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Common.Constants;

public static class WatchListConstants
{
    /// <summary>Anime episodes shorter than this (in minutes) are <c>DurationRange.Short</c>.</summary>
    public const int SHORT_DURATION_MAX_MINUTES = 15;

    /// <summary>Anime episodes longer than this (in minutes) are <c>DurationRange.Long</c>.</summary>
    public const int LONG_DURATION_MIN_MINUTES = 25;

    /// <summary>The same limits for the episodes of a TV series, which are longer than those of an anime.</summary>
    public const int SERIES_SHORT_DURATION_MAX_MINUTES = 30;
    public const int SERIES_LONG_DURATION_MIN_MINUTES = 50;

    /// <summary>The same limits for the length of a movie.</summary>
    public const int MOVIE_SHORT_DURATION_MAX_MINUTES = 90;
    public const int MOVIE_LONG_DURATION_MIN_MINUTES = 120;

    /// <summary>Limits between the periods of <c>ReleasePeriod</c>.</summary>
    public const int RECENT_FROM_YEAR = 2020;
    public const int TWENTY_TEN_FROM_YEAR = 2010;
    public const int TWO_THOUSAND_FROM_YEAR = 2000;

    /// <summary>The limits of a duration range for this type of title: what is short for a movie is not for an episode.</summary>
    public static (int ShortMax, int LongMin) DurationLimits(MediaType type) => type switch
    {
        MediaType.Movie => (MOVIE_SHORT_DURATION_MAX_MINUTES, MOVIE_LONG_DURATION_MIN_MINUTES),
        MediaType.TVShow => (SERIES_SHORT_DURATION_MAX_MINUTES, SERIES_LONG_DURATION_MIN_MINUTES),
        _ => (SHORT_DURATION_MAX_MINUTES, LONG_DURATION_MIN_MINUTES)
    };
}
