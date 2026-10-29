using System.Net;

namespace WhatchaWatching.Middleware.Tmdb;

/// <summary>Raised when TMDB is not configured, cannot be reached or answers with an error, after retries.</summary>
public class TmdbApiException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
    : ExternalApiException(message, statusCode, innerException);
