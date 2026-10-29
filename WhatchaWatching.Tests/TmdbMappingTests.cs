using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Tmdb;

namespace WhatchaWatching.Tests;

public class TmdbMappingTests
{
    private static TmdbClient CreateClient(string json) =>
        new(new HttpClient(StubHttpMessageHandler.Json(json)) { BaseAddress = new Uri(ExternalApiConstants.TMDB_BASE_URL) },
            new TmdbOptions("token"));

    [Fact]
    public async Task Genres_OfSeriesThatAreTwoGenres_BecomeTheTwoGenresOfMovies()
    {
        var client = CreateClient("""
            { "id": 1, "name": "X", "genres": [
                { "id": 10759, "name": "Action & Adventure" },
                { "id": 10765, "name": "Sci-Fi & Fantasy" },
                { "id": 10768, "name": "War & Politics" },
                { "id": 18, "name": "Drama" } ] }
            """);

        var show = await client.GetAsync(MediaType.TVShow, 1);

        Assert.Equal(["Action", "Adventure", "Sci-Fi", "Fantasy", "War", "Drama"], show!.Genres.Select(g => g.Name));
        Assert.Equal([28, 12, 878, 14, 10752, 18], show.Genres.Select(g => g.ExternalId));
    }

    [Fact]
    public async Task Platforms_ListedSeveralTimes_BecomeOneChoice()
    {
        var client = CreateClient("""
            { "id": 1, "title": "X", "watch/providers": { "results": { "FR": { "link": "https://t.test/fr", "flatrate": [
                { "provider_name": "Netflix" }, { "provider_name": "Netflix Standard with Ads" },
                { "provider_name": "HBO Max" }, { "provider_name": "HBO Max Amazon Channel" },
                { "provider_name": "Canal+ Apple TV Channel" }, { "provider_name": "Prime Video" } ] } } } }
            """);

        var movie = await client.GetAsync(MediaType.Movie, 1);

        Assert.Equal(["Netflix", "HBO Max", "Canal+", "Prime Video"], movie!.StreamingLinks.Select(l => l.Site));
    }

    [Theory]
    [InlineData("Netflix Standard with Ads", "Netflix")]
    [InlineData("netflix standard with ads", "netflix")]
    [InlineData("HBO Max Amazon Channel", "HBO Max")]
    [InlineData("Canal+ Apple TV Channel", "Canal+")]
    [InlineData("Paramount+ Roku Premium Channel", "Paramount+")]
    [InlineData("Netflix", "Netflix")]
    [InlineData("  Disney Plus ", "Disney Plus")]
    [InlineData(" with Ads", "with Ads")]
    [InlineData("", "")]
    public void PlatformNames_LoseTheMarkOfTheChannelOrThePlan(string name, string expected)
    {
        Assert.Equal(expected, TmdbPlatforms.Canonical(name));
    }

    [Fact]
    public async Task Genres_OfAMovie_UseTheNamesOfMyAnimeList()
    {
        var client = CreateClient("""{ "id": 1, "title": "X", "genres": [ { "id": 878, "name": "Science Fiction" } ] }""");

        var movie = await client.GetAsync(MediaType.Movie, 1);

        Assert.Equal(["Sci-Fi"], movie!.Genres.Select(g => g.Name));
    }

    [Fact]
    public async Task Genres_ThatTwoGenresShare_AreListedOnce()
    {
        var client = CreateClient("""
            { "id": 1, "name": "X", "genres": [
                { "id": 10759, "name": "Action & Adventure" }, { "id": 12, "name": "Adventure" } ] }
            """);

        var show = await client.GetAsync(MediaType.TVShow, 1);

        Assert.Equal(["Action", "Adventure"], show!.Genres.Select(g => g.Name));
    }

    [Fact]
    public async Task Search_OfSeries_AlsoGivesTheNamesOfTheSite()
    {
        var client = CreateClient("""{ "results": [ { "id": 1, "name": "X", "genre_ids": [10765] } ] }""");

        var show = Assert.Single((await client.SearchAsync(MediaType.TVShow, "x", 1)).Items);

        Assert.Equal(["Sci-Fi", "Fantasy"], show.Genres.Select(g => g.Name));
    }

    [Theory]
    [InlineData("Super Porn Movie", true)]
    [InlineData("XXX: State of the Union", true)]
    [InlineData("Hentai Kamen", true)]
    [InlineData("Erotica", true)]
    [InlineData("The Naked Gun", false)]
    [InlineData("Sexual Matrix", false)]
    [InlineData("Sex Education", false)]
    [InlineData("Pornography of the Mind", false)]
    [InlineData("Cornflakes", false)]
    public async Task ExplicitWordsInTheTitle_MakeAMovieAdult_WhateverTmdbFlags(string title, bool expected)
    {
        var client = CreateClient($$"""{ "id": 1, "title": "{{title}}", "adult": false }""");

        var movie = await client.GetAsync(MediaType.Movie, 1);

        Assert.Equal(expected, movie!.IsAdult);
    }

    [Fact]
    public async Task ExplicitWordsInTheOriginalTitle_AreFoundToo()
    {
        var client = CreateClient("""{ "id": 1, "title": "A film", "original_title": "Un film porno", "adult": false }""");

        Assert.True((await client.GetAsync(MediaType.Movie, 1))!.IsAdult);
    }

    [Fact]
    public async Task TheAdultFlagOfTmdb_StillCounts()
    {
        var client = CreateClient("""{ "id": 1, "title": "A film", "adult": true }""");

        Assert.True((await client.GetAsync(MediaType.Movie, 1))!.IsAdult);
    }
}
