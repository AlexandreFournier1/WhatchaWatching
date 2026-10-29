using System.Net;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Tmdb;

namespace WhatchaWatching.Tests;

public class TmdbClientTests
{
    private const string MovieSearchJson = """
        {
          "page": 2, "total_pages": 4, "total_results": 70,
          "results": [
            {
              "id": 603, "title": "The Matrix", "original_title": "The Matrix",
              "overview": "A hacker learns the truth.", "poster_path": "/matrix.jpg",
              "release_date": "1999-03-30", "vote_average": 8.2456, "adult": false,
              "genre_ids": [28, 878, 99999]
            }
          ]
        }
        """;

    private const string TvDetailsJson = """
        {
          "id": 1396, "name": "Breaking Bad", "original_name": "Breaking Bad",
          "overview": "A teacher turns to crime.", "poster_path": "/bb.jpg",
          "first_air_date": "2008-01-20", "vote_average": 8.9, "status": "Ended",
          "number_of_episodes": 62, "number_of_seasons": 5, "episode_run_time": [47, 45],
          "genres": [ { "id": 18, "name": "Drama" }, { "id": 80, "name": "Crime" } ],
          "watch/providers": { "results": {
            "FR": { "link": "https://www.themoviedb.org/tv/1396/watch?locale=FR",
                    "flatrate": [ { "provider_name": "Netflix" }, { "provider_name": "Prime Video" } ] },
            "US": { "link": "https://www.themoviedb.org/tv/1396/watch?locale=US",
                    "flatrate": [ { "provider_name": "Hulu" } ] }
          } }
        }
        """;

    private static TmdbClient CreateClient(StubHttpMessageHandler handler, string? token = "token") =>
        new(new HttpClient(handler) { BaseAddress = new Uri(ExternalApiConstants.TMDB_BASE_URL) },
            new TmdbOptions(token));

    private static string Path(HttpRequestMessage request) =>
        request.RequestUri!.AbsolutePath.Split("/3/")[1];

    [Fact]
    public async Task Search_Movies_MapsTitlesAndPagination()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(MovieSearchJson));

        var result = await client.SearchAsync(MediaType.Movie, "matrix", 2);

        Assert.Equal(2, result.Page);
        Assert.Equal(4, result.TotalPages);
        Assert.Equal(70, result.TotalItems);

        var movie = Assert.Single(result.Items);
        Assert.Equal(MediaType.Movie, movie.Type);
        Assert.Equal(603, movie.ExternalId);
        Assert.Equal("The Matrix", movie.Title);
        Assert.Equal(ExternalApiConstants.TMDB_IMAGE_BASE_URL + "/matrix.jpg", movie.ImageUrl);
        Assert.Equal(new DateTime(1999, 3, 30), movie.ReleaseDate);
        Assert.Equal(1999, movie.Year);
        Assert.Equal(8.2m, movie.Score);
        Assert.Null(movie.EpisodeCount);
        Assert.False(movie.IsAdult);
        Assert.Equal(["Action", "Sci-Fi"], movie.Genres.Select(g => g.Name));
    }

    [Fact]
    public async Task Search_TvShows_ReadsTheTvFieldNames()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("""
            { "page": 1, "total_pages": 1, "total_results": 1, "results": [
              { "id": 1396, "name": "Breaking Bad", "original_name": "Breaking Bad VO",
                "first_air_date": "2008-01-20", "genre_ids": [18, 10765] } ] }
            """));

        var show = Assert.Single((await client.SearchAsync(MediaType.TVShow, "breaking", 1)).Items);

        Assert.Equal(MediaType.TVShow, show.Type);
        Assert.Equal("Breaking Bad", show.Title);
        Assert.Equal("Breaking Bad VO", show.OriginalTitle);
        Assert.Equal(2008, show.Year);
        Assert.Equal(["Drama", "Sci-Fi", "Fantasy"], show.Genres.Select(g => g.Name));
    }

    [Fact]
    public async Task Search_AsksTheRightEndpointAndEscapesTheQuery()
    {
        var movies = StubHttpMessageHandler.Json("""{ "results": [] }""");
        var shows = StubHttpMessageHandler.Json("""{ "results": [] }""");

        await CreateClient(movies).SearchAsync(MediaType.Movie, "fast & furious", 1);
        await CreateClient(shows).SearchAsync(MediaType.TVShow, "x", 1);

        Assert.Equal("search/movie", Path(movies.Requests[0]));
        Assert.Contains("query=fast%20%26%20furious", movies.Requests[0].RequestUri!.Query);
        Assert.Equal("search/tv", Path(shows.Requests[0]));
    }

    [Fact]
    public async Task Search_IsSafeForWorkByDefaultAndLiftsTheFilterOnRequest()
    {
        var safe = StubHttpMessageHandler.Json("""{ "results": [] }""");
        var all = StubHttpMessageHandler.Json("""{ "results": [] }""");

        await CreateClient(safe).SearchAsync(MediaType.Movie, "x", 1);
        await CreateClient(all).SearchAsync(MediaType.Movie, "x", 1, includeAdult: true);

        Assert.Contains("include_adult=false", safe.Requests[0].RequestUri!.Query);
        Assert.Contains("include_adult=true", all.Requests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task Search_WithoutResults_ReturnsAnEmptyPage()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("""{ "page": 1, "total_pages": 0, "total_results": 0, "results": [] }"""));

        var result = await client.SearchAsync(MediaType.Movie, "nothing", 1);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task Search_WithBlankQuery_Throws()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.SearchAsync(MediaType.Movie, "  ", 1));
    }

    [Theory]
    [InlineData(MediaType.Anime)]
    [InlineData(MediaType.Manga)]
    public async Task Requests_ForATypeTmdbDoesNotProvide_Throw(MediaType type)
    {
        var client = CreateClient(StubHttpMessageHandler.Json("{}"));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.SearchAsync(type, "x", 1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetAsync(type, 1));
    }

    [Fact]
    public async Task Search_WhenFieldsAreMissing_UsesNulls()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("""{ "results": [ { "id": 7, "release_date": "", "vote_average": 0 } ] }"""));

        var movie = Assert.Single((await client.SearchAsync(MediaType.Movie, "x", 1)).Items);

        Assert.Equal(7, movie.ExternalId);
        Assert.Equal(string.Empty, movie.Title);
        Assert.Null(movie.ImageUrl);
        Assert.Null(movie.ReleaseDate);
        Assert.Null(movie.Year);
        Assert.Null(movie.Score);
        Assert.Empty(movie.Genres);
    }

    [Fact]
    public async Task Get_Movie_MapsTheDetails()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("""
            { "id": 603, "title": "The Matrix", "runtime": 136, "status": "Released",
              "genres": [ { "id": 28, "name": "Action" } ] }
            """));

        var movie = await client.GetAsync(MediaType.Movie, 603);

        Assert.Equal(136, movie!.EpisodeDurationMinutes);
        Assert.Equal(MediaStatus.Finished, movie.Status);
        Assert.Equal(["Action"], movie.Genres.Select(g => g.Name));
    }

    [Fact]
    public async Task Get_TvShow_MapsTheDetailsAndAsksForThePlatforms()
    {
        var handler = StubHttpMessageHandler.Json(TvDetailsJson);

        var show = await CreateClient(handler).GetAsync(MediaType.TVShow, 1396);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("tv/1396", Path(request));
        Assert.Contains("append_to_response=watch/providers", request.RequestUri!.Query);

        Assert.Equal(62, show!.EpisodeCount);
        Assert.Equal(5, show.SeasonCount);
        Assert.Equal(47, show.EpisodeDurationMinutes);
        Assert.Equal(MediaStatus.Finished, show.Status);
    }

    [Fact]
    public async Task Get_KeepsThePlatformsOfEveryCountryOfTheCatalog_EachWithItsCountry()
    {
        var show = await CreateClient(StubHttpMessageHandler.Json(TvDetailsJson)).GetAsync(MediaType.TVShow, 1396);

        Assert.Equal(
            [("FR", "Netflix"), ("FR", "Prime Video"), ("US", "Hulu")],
            show!.StreamingLinks.Select(l => (l.Region!, l.Site)));
        Assert.All(show.StreamingLinks.Where(l => l.Region == "FR"), l => Assert.Equal("https://www.themoviedb.org/tv/1396/watch?locale=FR", l.Url));
    }

    [Fact]
    public async Task Get_LeavesOutTheCountriesOfTheCatalogDoesNotKnow()
    {
        var json = """{ "id": 1, "name": "X", "watch/providers": { "results": { "JP": { "link": "https://t.test/jp", "flatrate": [ { "provider_name": "Netflix" } ] } } } }""";

        var show = await CreateClient(StubHttpMessageHandler.Json(json)).GetAsync(MediaType.TVShow, 1);

        Assert.Empty(show!.StreamingLinks);
    }

    [Fact]
    public async Task Get_TvShowWithoutRunTime_UsesTheLastEpisode()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(
            """{ "id": 1, "name": "X", "episode_run_time": [], "last_episode_to_air": { "runtime": 52 } }"""));

        var show = await client.GetAsync(MediaType.TVShow, 1);

        Assert.Equal(52, show!.EpisodeDurationMinutes);
    }

    [Theory]
    [InlineData(MediaType.Movie, "Released", MediaStatus.Finished)]
    [InlineData(MediaType.Movie, "Post Production", MediaStatus.Upcoming)]
    [InlineData(MediaType.Movie, "Canceled", null)]
    [InlineData(MediaType.TVShow, "Returning Series", MediaStatus.Airing)]
    [InlineData(MediaType.TVShow, "Ended", MediaStatus.Finished)]
    [InlineData(MediaType.TVShow, "Planned", MediaStatus.Upcoming)]
    [InlineData(MediaType.TVShow, "Something new", null)]
    public async Task Get_MapsTheStatus(MediaType type, string status, MediaStatus? expected)
    {
        var client = CreateClient(StubHttpMessageHandler.Json($$"""{ "id": 1, "title": "X", "name": "X", "status": "{{status}}" }"""));

        var title = await client.GetAsync(type, 1);

        Assert.Equal(expected, title!.Status);
    }

    [Fact]
    public async Task Get_FlagsAdultTitles()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("""{ "id": 1, "title": "X", "adult": true }"""));

        Assert.True((await client.GetAsync(MediaType.Movie, 1))!.IsAdult);
    }

    [Fact]
    public async Task Get_WhenNotFound_ReturnsNull()
    {
        var client = CreateClient(StubHttpMessageHandler.Status(HttpStatusCode.NotFound));

        Assert.Null(await client.GetAsync(MediaType.Movie, 999999));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task Requests_WhenTmdbFails_ThrowTmdbApiException(HttpStatusCode status)
    {
        var client = CreateClient(StubHttpMessageHandler.Status(status));

        var exception = await Assert.ThrowsAsync<TmdbApiException>(() => client.SearchAsync(MediaType.Movie, "x", 1));

        Assert.Equal(status, exception.StatusCode);
    }

    [Fact]
    public async Task Requests_WhenTheNetworkFails_ThrowTmdbApiException()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => throw new HttpRequestException("offline")));

        await Assert.ThrowsAsync<TmdbApiException>(() => client.GetAsync(MediaType.Movie, 1));
    }

    [Fact]
    public async Task Requests_WhenTheAnswerIsNotJson_ThrowTmdbApiException()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("<html>maintenance</html>"));

        await Assert.ThrowsAsync<TmdbApiException>(() => client.GetAsync(MediaType.Movie, 1));
    }

    [Fact]
    public async Task Requests_WithoutAnAccessToken_ThrowWithoutCallingTmdb()
    {
        var handler = StubHttpMessageHandler.Json("{}");
        var client = CreateClient(handler, token: null);

        await Assert.ThrowsAsync<TmdbNotConfiguredException>(() => client.SearchAsync(MediaType.Movie, "x", 1));
        await Assert.ThrowsAsync<TmdbNotConfiguredException>(() => client.GetAsync(MediaType.Movie, 1));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Requests_WhenTheCallerCancels_PropagateTheCancellation()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("{}"));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetAsync(MediaType.Movie, 1, cancellation.Token));
    }
}
