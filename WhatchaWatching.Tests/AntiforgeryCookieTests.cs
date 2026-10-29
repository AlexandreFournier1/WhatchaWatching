using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WhatchaWatching.Services;

namespace WhatchaWatching.Tests;

public class AntiforgeryCookieTests
{
    private static string SetCookieOf(bool https, bool configured)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (configured)
        {
            services.AddAntiforgery(AntiforgeryCookie.Configure);
        }
        else
        {
            services.AddAntiforgery();
        }

        using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Scheme = https ? "https" : "http";
        context.Request.Host = new HostString("whatcha-watching.alwaysdata.net");

        provider.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);

        return context.Response.Headers.SetCookie.ToString();
    }

    [Fact]
    public void Cookie_OverHttps_IsSecure()
    {
        Assert.Contains("secure", SetCookieOf(https: true, configured: true), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cookie_OverHttp_IsNotSecure_SoThatItStillWorksOnPlainHttp()
    {
        Assert.DoesNotContain("secure", SetCookieOf(https: false, configured: true), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithoutTheSetting_TheCookieIsNotSecureEvenOverHttps()
    {
        // What the live site showed: the reason this setting exists.
        Assert.DoesNotContain("secure", SetCookieOf(https: true, configured: false), StringComparison.OrdinalIgnoreCase);
    }
}
