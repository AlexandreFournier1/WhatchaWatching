using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace WhatchaWatching.Services;

public static class ProxyHeaders
{
    /// <summary>
    /// AlwaysData ends the TLS connection and talks plain HTTP to the app. Without these headers the app believes every
    /// visit is http: the cookies are not marked Secure, HSTS is never sent and the links it builds start with http.
    /// The proxy is not on a loopback address, so no network can be listed: the headers are read from any sender, which
    /// is only safe because the app is reached through the proxy alone.
    /// </summary>
    public static void Configure(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
}
