using System.Net;

namespace WhatchaWatching.Middleware.Jikan;

/// <summary>Raised when Jikan cannot be reached or answers with an error, after retries.</summary>
public class JikanApiException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
    : ExternalApiException(message, statusCode, innerException);
