namespace WhatchaWatching.Middleware.Tmdb;

/// <param name="AccessToken">The TMDB "API Read Access Token". Without it the client reports that TMDB is not set up.</param>
public sealed record TmdbOptions(string? AccessToken)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(AccessToken);
}
