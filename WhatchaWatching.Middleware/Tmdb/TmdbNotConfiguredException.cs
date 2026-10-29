namespace WhatchaWatching.Middleware.Tmdb;

/// <summary>There is no access token: this is a setting to make, not an outage to wait out.</summary>
public class TmdbNotConfiguredException()
    : TmdbApiException("TMDB has no access token configured (Tmdb:AccessToken).");
