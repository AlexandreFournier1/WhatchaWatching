using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace WhatchaWatching.Services;

public static class RateLimiting
{
    /// <summary>Posts to the pages where a password or an email address is tried: log in, sign up, reset, account.</summary>
    public const int AUTH_ATTEMPTS = 10;
    public static readonly TimeSpan AuthWindow = TimeSpan.FromMinutes(5);

    /// <summary>Everything else. A page load is a dozen requests, so this is a few dozen visits a minute from one address.</summary>
    public const int GENERAL_REQUESTS = 600;
    public static readonly TimeSpan GeneralWindow = TimeSpan.FromMinutes(1);

    private static readonly string[] AuthPaths = ["/login", "/register", "/forgot-password", "/reset-password", "/account"];

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(Partition);
        options.OnRejected = async (context, cancellationToken) =>
        {
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status429TooManyRequests;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
            }

            // With a body, the status code pages of the site leave the answer alone instead of showing "not found".
            response.ContentType = "text/plain; charset=utf-8";
            await response.WriteAsync("Too many requests. Try again in a few minutes.", cancellationToken);
        };
    }

    // Each visitor has a counter of their own, by address. Behind the proxy the address is the one of the visitor only
    // because the forwarded headers are read first.
    internal static RateLimitPartition<string> Partition(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (IsAuthAttempt(context.Request))
        {
            return RateLimitPartition.GetFixedWindowLimiter("auth:" + address, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = AUTH_ATTEMPTS,
                Window = AuthWindow,
                QueueLimit = 0
            });
        }

        // Scripts, styles and images of a page are not what is being protected, and each page loads many of them.
        if (HttpMethods.IsGet(context.Request.Method) && Path.HasExtension(context.Request.Path.Value))
        {
            return RateLimitPartition.GetNoLimiter("static");
        }

        return RateLimitPartition.GetSlidingWindowLimiter("general:" + address, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = GENERAL_REQUESTS,
            Window = GeneralWindow,
            SegmentsPerWindow = 6,
            QueueLimit = 0
        });
    }

    internal static bool IsAuthAttempt(HttpRequest request) =>
        HttpMethods.IsPost(request.Method) &&
        AuthPaths.Contains(request.Path.Value?.TrimEnd('/') ?? string.Empty, StringComparer.OrdinalIgnoreCase);

    /// <summary>True for the address of a proxy rather than of a visitor: the forwarded headers did not arrive.</summary>
    public static bool LooksLikeAProxy(IPAddress? address) =>
        address is not null && (IPAddress.IsLoopback(address) || IsPrivate(address));

    private static bool IsPrivate(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();

        return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
               (bytes[0] == 10 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168));
    }
}
