using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Components.Layout;
using WhatchaWatching.Components.Shared;
using WhatchaWatching.Services;

namespace WhatchaWatching.Tests;

public class MediaTypeUiTests
{
    private sealed class AtAddress : NavigationManager
    {
        public AtAddress(string uri) => Initialize(new Uri(uri).GetLeftPart(UriPartial.Authority) + "/", uri);
    }

    private sealed class Anonymous : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private static async Task<string> RenderAsync<T>(string address, Dictionary<string, object?>? parameters = null)
        where T : IComponent
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();
        services.AddSingleton<AuthenticationStateProvider, Anonymous>();
        services.AddSingleton<NavigationManager>(new AtAddress(address));

        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<T>(
                ParameterView.FromDictionary(parameters ?? new Dictionary<string, object?>()))).ToHtmlString());
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

    [Theory]
    [InlineData("", MediaType.Anime)]
    [InlineData("my-list", MediaType.Anime)]
    [InlineData("search?q=matrix&type=movie", MediaType.Movie)]
    [InlineData("search?type=tv", MediaType.TVShow)]
    [InlineData("search?type=movie#top", MediaType.Movie)]
    [InlineData("?type=tv", MediaType.TVShow)]
    [InlineData("search?type=bogus", MediaType.Anime)]
    [InlineData("movie/603", MediaType.Movie)]
    [InlineData("tv/1396?x=1", MediaType.TVShow)]
    [InlineData("anime/5114", MediaType.Anime)]
    [InlineData("anime/-3", MediaType.Anime)]
    [InlineData("movie/new", MediaType.Anime)]
    public void FromAddress_ReadsTheTypeFromThePathOrTheQuery(string address, MediaType expected)
    {
        Assert.Equal(expected, MediaRoutes.FromAddress(address));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("search", true)]
    [InlineData("my-list", true)]
    [InlineData("movie/603", true)]
    [InlineData("tv/1396", true)]
    [InlineData("anime/-3", true)]
    [InlineData("account", false)]
    [InlineData("login", false)]
    [InlineData("register", false)]
    [InlineData("anime/new", false)]
    [InlineData("movie", false)]
    [InlineData("error", false)]
    public void FollowsType_IsTrueForThePagesTheTypeSwitchChanges(string path, bool expected)
    {
        Assert.Equal(expected, MediaRoutes.FollowsType(path));
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
        var html = await RenderAsync<MediaTypeTabs>("http://localhost/search?q=matrix&page=3&type=anime");

        Assert.Contains("href=\"search?q=matrix&amp;type=anime\"", html);
        Assert.Contains("href=\"search?q=matrix&amp;type=movie\"", html);
        Assert.Contains("href=\"search?q=matrix&amp;type=tv\"", html);
        Assert.DoesNotContain("page=3", html);
    }

    [Fact]
    public async Task Tabs_MarkTheTypeOfTheAddressAsCurrent()
    {
        var html = await RenderAsync<MediaTypeTabs>("http://localhost/search?type=movie");

        Assert.Single(html.Split("aria-current=\"page\"").Skip(1));
        Assert.Contains("tab active\" href=\"search?type=movie\" aria-current=\"page\">Movies<", html);
    }

    [Fact]
    public async Task Tabs_WithoutATypeInTheAddress_ShowAnime()
    {
        var html = await RenderAsync<MediaTypeTabs>("http://localhost/my-list");

        Assert.Contains("aria-current=\"page\">Anime<", html);
        Assert.DoesNotContain(">All<", html);
    }

    [Fact]
    public async Task Tabs_OnTheHomePage_LinkToTheRootAndNotToTheCurrentQuery()
    {
        var html = await RenderAsync<MediaTypeTabs>("http://localhost/?type=movie");

        Assert.Contains("href=\"?type=anime\">Anime<", html);
        Assert.Contains("href=\"?type=tv\">TV series<", html);
    }

    [Fact]
    public async Task Tabs_OnThePageOfATitle_LeadToTheHomePageOfThatType()
    {
        var html = await RenderAsync<MediaTypeTabs>("http://localhost/movie/603");

        Assert.Contains("tab active\" href=\"./?type=movie\" aria-current=\"page\">Movies<", html);
        Assert.Contains("href=\"./?type=anime\">Anime<", html);
        Assert.Contains("href=\"./?type=tv\">TV series<", html);
    }

    [Theory]
    [InlineData("https://whatcha-watching.alwaysdata.net/search?q=x&type=movie")]
    [InlineData("http://whatcha-watching.alwaysdata.net/search?q=x&type=movie")]
    [InlineData("https://whatcha-watching.alwaysdata.net/movie/603")]
    public async Task Tabs_NeverLinkToAnAbsoluteAddress(string address)
    {
        // Behind a proxy the server may see http for a visit made over https: its scheme must not end up in a link.
        var html = await RenderAsync<MediaTypeTabs>(address);

        Assert.DoesNotContain("href=\"http", html);
    }

    [Fact]
    public async Task Header_OnASearchOfMovies_KeepsTheTypeInItsLinksAndSearch()
    {
        var html = await RenderAsync<NavMenu>("http://localhost/search?q=matrix&type=movie");

        Assert.Contains("type-tabs", html);
        Assert.Contains("href=\"./?type=movie\"", html);
        Assert.Contains("href=\"my-list?type=movie\"", html);
        Assert.Contains("name=\"type\" value=\"movie\"", html);
        Assert.Contains("Search movies...", html);
    }

    [Fact]
    public async Task Header_ForAnime_NeedsNoMarkInItsLinks()
    {
        var html = await RenderAsync<NavMenu>("http://localhost/my-list");

        Assert.Contains("href=\"my-list\"", html);
        Assert.Contains("name=\"type\" value=\"anime\"", html);
        Assert.Contains("nav-link active\" href=\"my-list\"", html);
    }

    [Fact]
    public async Task Header_OnThePageOfASeries_ShowsSeries()
    {
        var html = await RenderAsync<NavMenu>("http://localhost/tv/1396");

        Assert.Contains("tab active\" href=\"./?type=tv\"", html);
        Assert.Contains("href=\"my-list?type=tv\"", html);
    }

    [Theory]
    [InlineData("http://localhost/login")]
    [InlineData("http://localhost/account")]
    [InlineData("http://localhost/anime/new")]
    public async Task Header_OnPagesThatIgnoreTheType_HasNoTypeSwitch(string address)
    {
        Assert.DoesNotContain("type-tabs", await RenderAsync<NavMenu>(address));
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
}
