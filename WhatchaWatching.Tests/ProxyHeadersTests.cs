using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using WhatchaWatching.Services;

namespace WhatchaWatching.Tests;

public class ProxyHeadersTests
{
    private static ForwardedHeadersOptions Configured()
    {
        var options = new ForwardedHeadersOptions();
        ProxyHeaders.Configure(options);
        return options;
    }

    [Fact]
    public void Configure_ReadsTheSchemeAndTheAddressOfTheVisitor()
    {
        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, Configured().ForwardedHeaders);
    }

    [Fact]
    public void Configure_TrustsTheProxyWhateverItsAddress()
    {
        // By default only loopback proxies are trusted, which AlwaysData's is not: the headers would be ignored.
        var options = Configured();

        Assert.Empty(options.KnownProxies);
        Assert.Empty(options.KnownIPNetworks);
    }
}
