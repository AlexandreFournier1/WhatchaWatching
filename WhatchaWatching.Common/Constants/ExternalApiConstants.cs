namespace WhatchaWatching.Common.Constants;

public static class ExternalApiConstants
{
    public const string JIKAN_BASE_URL = "https://api.jikan.moe/v4/";
    public const string ANILIST_URL = "https://graphql.anilist.co/";

    public const string TMDB_BASE_URL = "https://api.themoviedb.org/3/";
    public const string TMDB_IMAGE_BASE_URL = "https://image.tmdb.org/t/p/w500";

    // The site is in English, and so are the genre names TMDB gives with the search results.
    public const string TMDB_LANGUAGE = "en-US";

    /// <summary>Country whose streaming platforms are listed for a movie or TV series, when none is configured.</summary>
    public const string TMDB_DEFAULT_WATCH_REGION = "FR";

    /// <summary>Results per search page, for every anime provider.</summary>
    public const int SEARCH_PAGE_SIZE = 12;
}
