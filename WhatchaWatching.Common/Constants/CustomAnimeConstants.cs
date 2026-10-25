namespace WhatchaWatching.Common.Constants;

public static class CustomAnimeConstants
{
    /// <summary>
    /// How many anime one user can add by hand. Registration is open: without a limit one account could fill the database.
    /// </summary>
    public const int MAX_PER_USER = 200;

    public const int MAX_EPISODES = 10_000;
    public const int MAX_EPISODE_DURATION_MINUTES = 600;
    public const int MIN_YEAR = 1900;
    public const int MAX_YEAR = 2100;
}
