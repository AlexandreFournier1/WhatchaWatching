using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WhatchaWatching.Components.Layout;

namespace WhatchaWatching.Tests;

public class SiteFooterTests
{
    private static async Task<string> RenderFooterAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<SiteFooter>(ParameterView.Empty)).ToHtmlString());
    }

    [Fact]
    public async Task Footer_CreditsTmdbWithItsLogoAndTheRequiredStatement()
    {
        var html = await RenderFooterAsync();

        Assert.Contains("tmdb-logo.png", html);
        Assert.Contains("https://www.themoviedb.org/", html);
        Assert.Contains("This product uses the TMDB API but is not endorsed or certified by TMDB.", html);
    }

    [Fact]
    public async Task Footer_CreditsJustWatchForTheStreamingData()
    {
        Assert.Contains("https://www.justwatch.com/", await RenderFooterAsync());
    }

    [Fact]
    public void TheLogoFileIsShippedWithTheSite()
    {
        var project = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "WhatchaWatching", "wwwroot", "tmdb-logo.png"));

        Assert.True(File.Exists(project));
    }
}
