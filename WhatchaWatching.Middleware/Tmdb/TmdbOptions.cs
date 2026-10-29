namespace WhatchaWatching.Middleware.Tmdb;

/// <param name="AccessToken">The TMDB "API Read Access Token". Without it the client reports that TMDB is unavailable.</param>
/// <param name="WatchRegion">ISO 3166-1 country code whose streaming platforms are listed.</param>
public sealed record TmdbOptions(string? AccessToken, string WatchRegion)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(AccessToken);
}
