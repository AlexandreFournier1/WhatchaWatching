namespace WhatchaWatching.Middleware.Tmdb;

public static class TmdbPlatforms
{
    // The same service is listed several times: through a channel of another service, or in a cheaper plan with ads.
    // To someone who wants to know where to watch, "Netflix" and "Netflix Standard with Ads" are one choice.
    private static readonly string[] Suffixes =
    [
        " Amazon Channel", " Apple TV Channel", " Roku Premium Channel", " Standard with Ads", " Basic with Ads", " with Ads"
    ];

    public static string? Canonical(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var trimmed = name.Trim();
        foreach (var suffix in Suffixes)
        {
            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && trimmed.Length > suffix.Length)
            {
                return trimmed[..^suffix.Length].Trim();
            }
        }

        return trimmed;
    }
}
