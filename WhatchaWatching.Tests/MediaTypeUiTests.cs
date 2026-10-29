using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Components.Shared;
using WhatchaWatching.Services;

namespace WhatchaWatching.Tests;

public class MediaTypeUiTests
{
    private sealed class AtAddress : NavigationManager
    {
        public AtAddress(string uri) => Initialize("http://localhost/", uri);
    }

    private static async Task<string> RenderAsync<T>(string address, Dictionary<string, object?> parameters)
        where T : IComponent
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<NavigationManager>(new AtAddress(address));

        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    }

    private static MediaSummaryDto Summary(MediaType type, int id = 603, int? minutes = 136) => new(
        type, id, "A title", null, null, ["Action"], minutes, 24, 2020, 7.5m, "Finished");

    [Theory]
    [InlineData(MediaType.Anime, "anime")]
    [InlineData(MediaType.Movie, "movie")]
    [InlineData(MediaType.TVShow, "tv")]
    public void Slug_RoundTrips(MediaType type, string slug)
    {
        Assert.Equal(slug, MediaRoutes.Slug(type));
        Assert.Equal(type, MediaRoutes.FromSlug(slug));
        Assert.Equal(type, MediaRoutes.FromSlug(slug.ToUpperInvariant()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("manga")]
    [InlineData("movies")]
    public void FromSlug_WithAnUnknownWord_ReturnsNull(string? slug)
    {
        Assert.Null(MediaRoutes.FromSlug(slug));
    }

    [Fact]
    public void Slug_ForATypeWithoutPages_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MediaRoutes.Slug(MediaType.Manga));
    }

    [Fact]
    public void Urls_UseTheSlugOfTheType()
    {
        Assert.Equal("movie/603", MediaRoutes.DetailUrl(MediaType.Movie, 603));
        Assert.Equal("tv/1396", MediaRoutes.DetailUrl(MediaType.TVShow, 1396));
        Assert.Equal("anime/5114", MediaRoutes.DetailUrl(MediaType.Anime, 5114));
        Assert.Equal("search?type=tv", MediaRoutes.SearchUrl(MediaType.TVShow));
    }

    [Fact]
    public void Duration_OfAMovieIsNotPerEpisode()
    {
        Assert.Equal("136 min", MediaDisplay.Duration(Summary(MediaType.Movie)));
        Assert.Equal("45 min / episode", MediaDisplay.Duration(Summary(MediaType.TVShow, minutes: 45)));
        Assert.Equal("Duration unknown", MediaDisplay.Duration(Summary(MediaType.Movie, minutes: null)));
    }

    [Fact]
    public void Episodes_AreLeftOutForAMovieAndCountSeasonsForASeries()
    {
        Assert.Null(MediaDisplay.Episodes(Summary(MediaType.Movie)));
        Assert.Equal("24 episodes", MediaDisplay.Episodes(Summary(MediaType.Anime)));
        Assert.Equal("24 episodes · 2 seasons", MediaDisplay.Episodes(Summary(MediaType.TVShow) with { Seasons = 2 }));
        Assert.Equal("24 episodes · 1 season", MediaDisplay.Episodes(Summary(MediaType.TVShow) with { Seasons = 1 }));
    }

    [Fact]
    public void Status_OfAReleasedMovieIsNotFinishedAiring()
    {
        Assert.Equal("Released", MediaDisplay.Status(Summary(MediaType.Movie)));
        Assert.Equal("Finished", MediaDisplay.Status(Summary(MediaType.Anime)));
    }

    [Fact]
    public async Task Tabs_LinkToEachTypeAndKeepTheSearch()
    {
        var html = await RenderAsync<MediaTypeTabs>(
            "http://localhost/search?q=matrix&page=3&type=anime", new() { ["Selected"] = MediaType.Anime });

        Assert.Contains("href=\"http://localhost/search?q=matrix&amp;type=movie\"", html);
        Assert.Contains("href=\"http://localhost/search?q=matrix&amp;type=tv\"", html);
        Assert.DoesNotContain("page=3", html);
        Assert.DoesNotContain(">All<", html);
    }

    [Fact]
    public async Task Tabs_MarkTheSelectedTypeAsCurrent()
    {
        var html = await RenderAsync<MediaTypeTabs>(
            "http://localhost/search?type=movie", new() { ["Selected"] = MediaType.Movie });

        Assert.Single(html.Split("aria-current=\"page\"").Skip(1));
        Assert.Contains("tab active\" href=\"http://localhost/search?type=movie\" aria-current=\"page\">Movies<", html);
    }

    [Fact]
    public async Task Tabs_WithAll_OfferAnAllTabThatDropsTheType()
    {
        var html = await RenderAsync<MediaTypeTabs>(
            "http://localhost/my-list?type=tv", new() { ["Selected"] = MediaType.TVShow, ["AllowAll"] = true });

        Assert.Contains("href=\"http://localhost/my-list\">All<", html);
        Assert.Contains("TV series", html);
    }

    [Fact]
    public async Task Tabs_WithAllAndNoType_MarkAllAsCurrent()
    {
        var html = await RenderAsync<MediaTypeTabs>(
            "http://localhost/my-list", new() { ["Selected"] = null, ["AllowAll"] = true });

        Assert.Contains("aria-current=\"page\">All<", html);
    }

    [Theory]
    [InlineData(MediaType.Anime, "anime/603")]
    [InlineData(MediaType.Movie, "movie/603")]
    [InlineData(MediaType.TVShow, "tv/603")]
    public async Task Card_LinksToThePageOfItsType(MediaType type, string href)
    {
        var html = await RenderAsync<MediaCard>("http://localhost/", new() { ["Media"] = Summary(type) });

        Assert.Contains($"href=\"{href}\"", html);
    }

    [Fact]
    public async Task Card_ShowsTheTypeBadgeOnlyWhenAsked()
    {
        var without = await RenderAsync<MediaCard>("http://localhost/", new() { ["Media"] = Summary(MediaType.Movie) });
        var with = await RenderAsync<MediaCard>(
            "http://localhost/", new() { ["Media"] = Summary(MediaType.Movie), ["ShowType"] = true });

        Assert.DoesNotContain("chip-type", without);
        Assert.Matches("chip-type\"[^>]*>Movie</span>", with);
    }
}
