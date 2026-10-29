using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace WhatchaWatching.Services;

public static class SecurityHeaders
{
    // What a page may load: its own files, posters from any https address (they come from the providers, and from
    // wherever the address of an anime added by hand points), and its circuit, a WebSocket. Blazor writes its importmap
    // as an inline script and the components have inline styles, so both need 'unsafe-inline'. Nothing may frame the
    // site, change its base address, send a form elsewhere or load a plugin.
    public const string POLICY =
        "default-src 'self'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline'; connect-src 'self' wss: ws:; font-src 'self' data:; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'";

    public static void Apply(IHeaderDictionary headers)
    {
        headers.XContentTypeOptions = "nosniff";
        // Posters are loaded from other sites: they get no page address, only the visit of an image.
        headers["Referrer-Policy"] = "same-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers.XFrameOptions = "DENY";
        headers.ContentSecurityPolicy = POLICY;
    }

    /// <summary>
    /// Adds the headers when the response starts, so that they are the last word: the antiforgery token of a form page
    /// sets its own X-Frame-Options and Content-Security-Policy, which would replace earlier ones.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) => app.Use((context, next) =>
    {
        context.Response.OnStarting(() =>
        {
            Apply(context.Response.Headers);
            return Task.CompletedTask;
        });

        return next(context);
    });
}
