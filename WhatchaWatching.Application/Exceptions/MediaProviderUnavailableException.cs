using WhatchaWatching.Middleware.Tmdb;

namespace WhatchaWatching.Application.Exceptions;

/// <summary>The provider of a title (Jikan for anime, TMDB for movies and series) cannot be reached and no stored copy can replace it.</summary>
public class MediaProviderUnavailableException(Exception innerException)
    : Exception("The provider is not available.", innerException)
{
    /// <summary>True when the provider is not set up on this site (no TMDB token), so trying again will not help.</summary>
    public bool NotConfigured => InnerException is TmdbNotConfiguredException;
}
