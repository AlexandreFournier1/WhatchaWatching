using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WhatchaWatching.Components.Layout;
using WhatchaWatching.Components.Pages;

namespace WhatchaWatching.Tests;

public class PrivacyPageTests
{
    private static async Task<string> RenderAsync<T>() where T : IComponent
    {
        var services = new ServiceCollection();
        services.AddLogging();

        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<T>(ParameterView.Empty)).ToHtmlString());
    }

    [Fact]
    public async Task Privacy_SaysWhatIsStoredWhichCookiesAreUsedAndHowToDelete()
    {
        var html = await RenderAsync<Privacy>();

        Assert.Contains("What is stored", html);
        Assert.Contains("Cookies", html);
        Assert.Contains("no tracking", html);
        Assert.Contains("Delete my account", html);
        Assert.Contains("href=\"account\"", html);
    }

    [Fact]
    public async Task Privacy_NamesTheSitesThatSeeTheIpAddressOfTheVisitor()
    {
        var html = await RenderAsync<Privacy>();

        Assert.Contains("IP address", html);
        Assert.Contains("jikan.moe", html);
        Assert.Contains("anilist.co", html);
        Assert.Contains("themoviedb.org", html);
    }

    [Fact]
    public async Task TheFooter_LinksToThePrivacyPage()
    {
        Assert.Contains("href=\"privacy\"", await RenderAsync<SiteFooter>());
    }
}
