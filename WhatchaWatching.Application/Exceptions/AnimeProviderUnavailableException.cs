namespace WhatchaWatching.Application.Exceptions;

/// <summary>The anime provider (Jikan) cannot be reached and no stored copy can replace it.</summary>
public class AnimeProviderUnavailableException(Exception innerException)
    : Exception("The anime provider is not available.", innerException);
