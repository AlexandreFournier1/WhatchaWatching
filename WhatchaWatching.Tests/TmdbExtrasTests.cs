using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Tmdb;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

public class TmdbExtrasTests
{
    private const string ExtrasJson = """
        {
          "id": 1396, "name": "Breaking Bad",
          "videos": { "results": [
            { "key": "teaser1", "site": "YouTube", "type": "Teaser", "official": true, "name": "Teaser" },
            { "key": "vimeo1", "site": "Vimeo", "type": "Trailer", "official": true, "name": "On Vimeo" },
            { "key": "fan1", "site": "YouTube", "type": "Trailer", "official": false, "name": "Fan trailer" },
            { "key": "off1", "site": "YouTube", "type": "Trailer", "official": true, "name": "Official trailer" },
            { "key": "clip1", "site": "YouTube", "type": "Clip", "official": true, "name": "A clip" } ] },
          "credits": { "cast": [
            { "name": "Third", "character": "C", "profile_path": "/c.jpg", "order": 2 },
            { "name": "First", "character": "A", "profile_path": "/a.jpg", "order": 0 },
            { "name": "Second", "character": null, "profile_path": null, "order": 1 },
            { "name": "", "character": "Nobody", "order": 3 } ] },
          "seasons": [
            { "season_number": 0, "name": "Specials", "episode_count": 5, "air_date": "2009-02-17" },
            { "season_number": 2, "name": "Season 2", "episode_count": 13, "air_date": "2009-03-08" },
            { "season_number": 1, "name": "", "episode_count": 7, "air_date": "2008-01-20" },
            { "season_number": 3, "name": "Season 3", "episode_count": 0, "air_date": "" } ],
          "recommendations": { "results": [
            { "id": 60059, "name": "Better Call Saul", "poster_path": "/saul.jpg", "first_air_date": "2015-02-08", "adult": false },
            { "id": 1, "name": "No poster", "poster_path": null },
            { "id": 2, "name": "Adult one", "poster_path": "/adult.jpg", "adult": true } ] }
        }
        """;

    private static TmdbClient CreateClient(string json) =>
        new(new HttpClient(StubHttpMessageHandler.Json(json)) { BaseAddress = new Uri(ExternalApiConstants.TMDB_BASE_URL) },
            new TmdbOptions("token"));

    [Fact]
    public async Task Extras_AskTmdbForVideosCreditsAndRecommendationsInOneCall()
    {
        var handler = StubHttpMessageHandler.Json(ExtrasJson);
        var client = new TmdbClient(
            new HttpClient(handler) { BaseAddress = new Uri(ExternalApiConstants.TMDB_BASE_URL) }, new TmdbOptions("token"));

        await client.GetExtrasAsync(MediaType.TVShow, 1396);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("tv/1396", request.RequestUri!.AbsolutePath.Split("/3/")[1]);
        Assert.Contains("append_to_response=videos,credits,recommendations", request.RequestUri.Query);
    }

    [Fact]
    public async Task Trailer_IsTheOfficialYouTubeTrailer_BeforeTeasersAndFanVideos()
    {
        var extras = await CreateClient(ExtrasJson).GetExtrasAsync(MediaType.TVShow, 1396);

        Assert.Equal("https://www.youtube.com/watch?v=off1", extras!.Trailer!.Url);
        Assert.Equal("Official trailer", extras.Trailer.Name);
    }

    [Fact]
    public async Task Trailer_FallsBackOnATeaser_AndIsNullWithoutAnyYouTubeVideo()
    {
        var teaser = await CreateClient("""{ "id": 1, "name": "X", "videos": { "results": [ { "key": "t", "site": "YouTube", "type": "Teaser" } ] } }""")
            .GetExtrasAsync(MediaType.TVShow, 1);
        var none = await CreateClient("""{ "id": 1, "name": "X", "videos": { "results": [ { "key": "v", "site": "Vimeo", "type": "Trailer" } ] } }""")
            .GetExtrasAsync(MediaType.TVShow, 1);

        Assert.EndsWith("v=t", teaser!.Trailer!.Url);
        Assert.Null(none!.Trailer);
    }

    [Fact]
    public async Task Trailer_KeyIsEscapedInTheAddress()
    {
        var extras = await CreateClient("""{ "id": 1, "name": "X", "videos": { "results": [ { "key": "a&b=c", "site": "YouTube", "type": "Trailer" } ] } }""")
            .GetExtrasAsync(MediaType.TVShow, 1);

        Assert.Equal("https://www.youtube.com/watch?v=a%26b%3Dc", extras!.Trailer!.Url);
    }

    [Fact]
    public async Task Cast_IsInBillingOrder_WithoutNamelessEntries()
    {
        var extras = await CreateClient(ExtrasJson).GetExtrasAsync(MediaType.TVShow, 1396);

        Assert.Equal(["First", "Second", "Third"], extras!.Cast.Select(c => c.Name));
        Assert.Equal("A", extras.Cast[0].Character);
        Assert.Equal(ExternalApiConstants.TMDB_THUMBNAIL_BASE_URL + "/a.jpg", extras.Cast[0].PhotoUrl);
        Assert.Null(extras.Cast[1].Character);
        Assert.Null(extras.Cast[1].PhotoUrl);
    }

    [Fact]
    public async Task Cast_IsLimitedToTheFirstEight()
    {
        var people = string.Join(",", Enumerable.Range(0, 20).Select(i => $$"""{ "name": "Actor {{i}}", "order": {{i}} }"""));

        var extras = await CreateClient($$"""{ "id": 1, "name": "X", "credits": { "cast": [ {{people}} ] } }""")
            .GetExtrasAsync(MediaType.TVShow, 1);

        Assert.Equal(8, extras!.Cast.Count);
        Assert.Equal("Actor 7", extras.Cast[^1].Name);
    }

    [Fact]
    public async Task Seasons_SkipTheSpecials_AreInOrderAndAreNamedWhenTheyHaveNoName()
    {
        var extras = await CreateClient(ExtrasJson).GetExtrasAsync(MediaType.TVShow, 1396);

        Assert.Equal([1, 2, 3], extras!.Seasons.Select(s => s.Number));
        Assert.Equal(["Season 1", "Season 2", "Season 3"], extras.Seasons.Select(s => s.Name));
        Assert.Equal([7, 13, null], extras.Seasons.Select(s => s.EpisodeCount));
        Assert.Equal([2008, 2009, null], extras.Seasons.Select(s => s.Year));
    }

    [Fact]
    public async Task AMovie_HasNoSeasons()
    {
        var extras = await CreateClient(ExtrasJson).GetExtrasAsync(MediaType.Movie, 1396);

        Assert.Empty(extras!.Seasons);
    }

    [Fact]
    public async Task Similar_AreSmallPostersOfTheSameType_WithoutTheOnesThatHaveNone()
    {
        var extras = await CreateClient(ExtrasJson).GetExtrasAsync(MediaType.TVShow, 1396);

        Assert.Equal(["Better Call Saul", "Adult one"], extras!.Similar.Select(s => s.Title));
        var saul = extras.Similar[0];
        Assert.Equal(MediaType.TVShow, saul.Type);
        Assert.Equal(60059, saul.ExternalId);
        Assert.Equal(2015, saul.Year);
        Assert.Equal(ExternalApiConstants.TMDB_THUMBNAIL_BASE_URL + "/saul.jpg", saul.ImageUrl);
        Assert.True(extras.Similar[1].IsAdult);
    }

    [Fact]
    public async Task Extras_OfAnUnknownTitle_AreNull()
    {
        var client = new TmdbClient(
            new HttpClient(StubHttpMessageHandler.Status(System.Net.HttpStatusCode.NotFound)) { BaseAddress = new Uri(ExternalApiConstants.TMDB_BASE_URL) },
            new TmdbOptions("token"));

        Assert.Null(await client.GetExtrasAsync(MediaType.Movie, 999999));
    }

    [Fact]
    public async Task Extras_WithoutAnyOfThem_AreEmpty()
    {
        var extras = await CreateClient("""{ "id": 1, "title": "X" }""").GetExtrasAsync(MediaType.Movie, 1);

        Assert.True(extras!.IsEmpty);
    }

    // The service, with a fake client.

    private readonly FakeTmdbClient _tmdb = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private MediaExtrasService Service() => new(_tmdb, _cache, NullLogger<MediaExtrasService>.Instance);

    private static MediaExtrasDto Extras(params ExternalMediaDto[] similar) =>
        new(new TrailerDto("https://www.youtube.com/watch?v=x", null), [new CastMemberDto("Actor", null, null)], [], similar);

    [Fact]
    public async Task Service_AsksTmdbOnlyOncePerTitle()
    {
        _tmdb.With(FakeTmdbClient.Movie(1));
        _tmdb.Extras = Extras();
        var service = Service();

        await service.GetAsync(MediaType.Movie, 1);
        var second = await service.GetAsync(MediaType.Movie, 1);

        Assert.Equal(1, _tmdb.ExtrasCalls);
        Assert.Single(second.Cast);
    }

    [Fact]
    public async Task Service_KeepsAMovieAndASeriesWithTheSameIdApart()
    {
        _tmdb.With(FakeTmdbClient.Movie(7), FakeTmdbClient.Series(7));
        _tmdb.Extras = Extras();
        var service = Service();

        await service.GetAsync(MediaType.Movie, 7);
        await service.GetAsync(MediaType.TVShow, 7);

        Assert.Equal(2, _tmdb.ExtrasCalls);
    }

    [Theory]
    [InlineData(MediaType.Anime, 1)]
    [InlineData(MediaType.Manga, 1)]
    [InlineData(MediaType.Movie, 0)]
    [InlineData(MediaType.Movie, -5)]
    public async Task Service_IsEmptyWithoutCallingTmdb_ForWhatTmdbDoesNotKnow(MediaType type, int id)
    {
        _tmdb.Extras = Extras();

        Assert.True((await Service().GetAsync(type, id)).IsEmpty);
        Assert.Equal(0, _tmdb.ExtrasCalls);
    }

    [Fact]
    public async Task Service_NeverThrows_AndDoesNotKeepAFailure()
    {
        _tmdb.With(FakeTmdbClient.Movie(1));
        _tmdb.Extras = Extras();
        _tmdb.Failure = new TmdbApiException("down");
        var service = Service();

        Assert.True((await service.GetAsync(MediaType.Movie, 1)).IsEmpty);

        _tmdb.Failure = null;
        Assert.False((await service.GetAsync(MediaType.Movie, 1)).IsEmpty);
        Assert.Equal(2, _tmdb.ExtrasCalls);
    }

    [Fact]
    public async Task Service_WhenTmdbIsNotSetUp_IsEmpty()
    {
        _tmdb.Failure = new TmdbNotConfiguredException();

        Assert.True((await Service().GetAsync(MediaType.Movie, 1)).IsEmpty);
    }

    [Fact]
    public async Task Service_LeavesAdultSuggestionsToThoseWhoTurnedThemOn()
    {
        _tmdb.With(FakeTmdbClient.Movie(1));
        _tmdb.Extras = Extras(FakeTmdbClient.Movie(2, "Safe"), FakeTmdbClient.Movie(3, "Adult") with { IsAdult = true });
        var service = Service();

        var safe = await service.GetAsync(MediaType.Movie, 1);
        var all = await service.GetAsync(MediaType.Movie, 1, includeAdult: true);
        var safeAgain = await service.GetAsync(MediaType.Movie, 1);

        Assert.Equal(["Safe"], safe.Similar.Select(s => s.Title));
        Assert.Equal(["Safe", "Adult"], all.Similar.Select(s => s.Title));
        Assert.Equal(["Safe"], safeAgain.Similar.Select(s => s.Title));
        Assert.Equal(1, _tmdb.ExtrasCalls);
    }

    [Fact]
    public async Task Service_OfAnUnknownTitle_IsEmpty()
    {
        Assert.True((await Service().GetAsync(MediaType.Movie, 12345)).IsEmpty);
    }
}
