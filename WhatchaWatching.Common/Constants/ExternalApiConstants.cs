namespace WhatchaWatching.Common.Constants;

public static class ExternalApiConstants
{
    public const string JIKAN_BASE_URL = "https://api.jikan.moe/v4/";
    public const string ANILIST_URL = "https://graphql.anilist.co/";

    public const string TMDB_BASE_URL = "https://api.themoviedb.org/3/";
    public const string TMDB_IMAGE_BASE_URL = "https://image.tmdb.org/t/p/w500";

    /// <summary>Small images, for a photo of an actor or a title that is only suggested.</summary>
    public const string TMDB_THUMBNAIL_BASE_URL = "https://image.tmdb.org/t/p/w185";

    public const string YOUTUBE_WATCH_URL = "https://www.youtube.com/watch?v=";

    // The site is in English, and so are the genre names TMDB gives with the search results.
    public const string TMDB_LANGUAGE = "en-US";


    /// <summary>Results per search page, for every provider: the size TMDB gives, which it does not let anyone change.</summary>
    public const int SEARCH_PAGE_SIZE = 20;
}
