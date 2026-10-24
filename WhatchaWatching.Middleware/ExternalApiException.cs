using System.Net;

namespace WhatchaWatching.Middleware;

/// <summary>An external API cannot be reached or answers with an error, after retries.</summary>
public class ExternalApiException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
