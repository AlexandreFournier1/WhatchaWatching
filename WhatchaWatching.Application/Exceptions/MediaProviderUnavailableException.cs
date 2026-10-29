namespace WhatchaWatching.Application.Exceptions;

/// <summary>The provider of a title (Jikan for anime, TMDB for movies and series) cannot be reached and no stored copy can replace it.</summary>
public class MediaProviderUnavailableException(Exception innerException)
    : Exception("The provider is not available.", innerException);
