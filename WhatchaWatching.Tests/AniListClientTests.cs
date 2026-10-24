using System.Net;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware;
using WhatchaWatching.Middleware.AniList;

namespace WhatchaWatching.Tests;

public class AniListClientTests
{
    private const string SearchJson = """
        {
          "data": { "Page": {
            "pageInfo": { "total": 25, "currentPage": 2, "lastPage": 3 },
            "media": [
              {
                "idMal": 52991,
                "title": { "romaji": "Sousou no Frieren", "english": "Frieren: Beyond Journey's End", "native": "葬送のフリーレン" },
                "description": "The adventure is over.<br><br>\n(Source: <i>Crunchyroll</i>) Tom &amp; Jerry",
                "coverImage": { "large": "https://img.test/frieren.jpg" },
                "episodes": 28, "duration": 24, "status": "FINISHED", "averageScore": 91,
                "seasonYear": 2023, "season": "FALL",
                "startDate": { "year": 2023, "month": 9, "day": 29 },
                "genres": ["Adventure", "Drama", "Fantasy"]
              },
              {
                "idMal": null,
                "title": { "romaji": "No MAL id", "english": null, "native": null },
                "genres": []
              }
            ]
          } }
        }
        """;

    private static AniListClient CreateClient(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri(ExternalApiConstants.ANILIST_URL) });

    private static string MediaJson(string fields) => $$"""{ "data": { "Media": { "idMal": 1, "title": { "romaji": "Test" } {{fields}} } } }""";

    [Fact]
    public async Task Search_MapsTheAnimeAndThePagination()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(SearchJson));

        var result = await client.SearchAnimeAsync("frieren", 2);

        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(25, result.TotalItems);

        var anime = Assert.Single(result.Items);
        Assert.Equal(52991, anime.ExternalId);
        Assert.Equal("Frieren: Beyond Journey's End", anime.Title);
        Assert.Equal("葬送のフリーレン", anime.OriginalTitle);
        Assert.Equal("https://img.test/frieren.jpg", anime.ImageUrl);
        Assert.Equal(28, anime.EpisodeCount);
        Assert.Equal(24, anime.EpisodeDurationMinutes);
        Assert.Equal(MediaStatus.Finished, anime.Status);
        Assert.Equal(9.1m, anime.Score);
        Assert.Equal(2023, anime.Year);
        Assert.Equal("fall", anime.Season);
        Assert.Equal(new DateTime(2023, 9, 29, 0, 0, 0, DateTimeKind.Utc), anime.ReleaseDate);
        Assert.Equal(["Adventure", "Drama", "Fantasy"], anime.Genres.Select(g => g.Name));
        Assert.Equal([2, 8, 10], anime.Genres.Select(g => g.ExternalId));
    }

    [Fact]
    public async Task Search_SkipsAnimeWithoutAMyAnimeListId()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(SearchJson));

        var result = await client.SearchAnimeAsync("x", 1);

        Assert.DoesNotContain(result.Items, a => a.Title == "No MAL id");
    }

    [Fact]
    public async Task Search_PostsTheQueryAndItsVariablesAsJson()
    {
        string? body = null;
        string? contentType = null;
        HttpMethod? method = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            method = request.Method;
            contentType = request.Content?.Headers.ContentType?.MediaType;
            body = request.Content?.ReadAsStringAsync().Result;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{ "data": { "Page": { "media": [] } } }""") };
        });

        await CreateClient(handler).SearchAnimeAsync("fate/zero \"extra\"", 3);

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("application/json", contentType);
        Assert.Contains("\"search\":\"fate/zero \\u0022extra\\u0022\"", body);
        Assert.Contains("\"page\":3", body);
        Assert.Contains($"\"perPage\":{ExternalApiConstants.SEARCH_PAGE_SIZE}", body);
        Assert.Contains("isAdult: false", body);
    }

    [Fact]
    public async Task Search_WithBlankQuery_Throws()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.SearchAnimeAsync(" ", 1));
    }

    [Fact]
    public async Task GetAnime_AsksForTheAnimeByItsMyAnimeListId()
    {
        string? body = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            body = request.Content?.ReadAsStringAsync().Result;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(MediaJson("")) };
        });

        var anime = await CreateClient(handler).GetAnimeAsync(52991);

        Assert.Contains("\"id\":52991", body);
        Assert.Contains("idMal: $id", body);
        Assert.Equal("Test", anime!.Title);
    }

    [Fact]
    public async Task GetAnime_WhenAniListAnswers404_ReturnsNull()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(
            """{ "data": { "Media": null }, "errors": [ { "message": "Not Found.", "status": 404 } ] }""", HttpStatusCode.NotFound));

        Assert.Null(await client.GetAnimeAsync(99999999));
    }

    [Fact]
    public async Task GetAnime_WithoutAMyAnimeListId_ReturnsNull()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("""{ "data": { "Media": { "idMal": null } } }"""));

        Assert.Null(await client.GetAnimeAsync(1));
    }

    [Fact]
    public async Task GetAnime_CleansTheHtmlOfTheDescription()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(
            MediaJson(""", "description": "First<br><br><br>Second <i>italic</i> &amp; <b>bold</b>"  """)));

        var anime = await client.GetAnimeAsync(1);

        Assert.Equal("First\n\nSecond italic & bold", anime!.Synopsis);
    }

    [Theory]
    [InlineData("RELEASING", MediaStatus.Airing)]
    [InlineData("FINISHED", MediaStatus.Finished)]
    [InlineData("NOT_YET_RELEASED", MediaStatus.Upcoming)]
    [InlineData("CANCELLED", null)]
    public async Task GetAnime_MapsTheStatus(string status, MediaStatus? expected)
    {
        var client = CreateClient(StubHttpMessageHandler.Json(MediaJson($$""", "status": "{{status}}" """)));

        Assert.Equal(expected, (await client.GetAnimeAsync(1))!.Status);
    }

    [Theory]
    [InlineData(85, 8.5)]
    [InlineData(100, 10.0)]
    [InlineData(74, 7.4)]
    public async Task GetAnime_ConvertsTheScoreToATenPointScale(int averageScore, double expected)
    {
        var client = CreateClient(StubHttpMessageHandler.Json(MediaJson($""", "averageScore": {averageScore} """)));

        Assert.Equal((decimal)expected, (await client.GetAnimeAsync(1))!.Score);
    }

    [Fact]
    public async Task GetAnime_TranslatesGenresToMyAnimeListGenresAndSkipsUnknownOnes()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(
            MediaJson(""", "genres": ["Thriller", "Slice of Life", "Brand New Genre", "Action", "Action"] """)));

        var genres = (await client.GetAnimeAsync(1))!.Genres;

        Assert.Equal(["Suspense", "Slice of Life", "Action"], genres.Select(g => g.Name));
        Assert.Equal([41, 36, 1], genres.Select(g => g.ExternalId));
    }

    [Fact]
    public async Task GetAnime_WithAPartialStartDate_UsesTheYearAndLeavesTheRest()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(
            MediaJson(""", "startDate": { "year": 2027, "month": 10, "day": null } """)));

        var anime = await client.GetAnimeAsync(1);

        Assert.Equal(2027, anime!.Year);
        Assert.Equal(new DateTime(2027, 10, 1, 0, 0, 0, DateTimeKind.Utc), anime.ReleaseDate);
    }

    [Fact]
    public async Task Requests_WhenGraphQlReturnsAnErrorWithoutData_Throw()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("""{ "errors": [ { "message": "Too Many Requests." } ] }"""));

        var exception = await Assert.ThrowsAsync<ExternalApiException>(() => client.GetAnimeAsync(1));

        Assert.Contains("Too Many Requests.", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task Requests_WhenAniListFails_ThrowWithTheStatus(HttpStatusCode status)
    {
        var client = CreateClient(StubHttpMessageHandler.Status(status));

        var exception = await Assert.ThrowsAsync<ExternalApiException>(() => client.SearchAnimeAsync("x", 1));

        Assert.Equal(status, exception.StatusCode);
    }

    [Fact]
    public async Task Requests_WhenTheNetworkFails_Throw()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => throw new HttpRequestException("offline")));

        await Assert.ThrowsAsync<ExternalApiException>(() => client.GetAnimeAsync(1));
    }

    [Fact]
    public async Task Requests_WhenTheAnswerIsNotJson_Throw()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("<html>maintenance</html>"));

        await Assert.ThrowsAsync<ExternalApiException>(() => client.GetAnimeAsync(1));
    }
}

