using System.Net;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Jikan;

namespace WhatchaWatching.Tests;

public class JikanClientTests
{
    private const string SearchJson = """
        {
          "pagination": { "last_visible_page": 3, "current_page": 2, "items": { "total": 30 } },
          "data": [
            {
              "mal_id": 5114,
              "title": "Sousou no Frieren",
              "title_english": "Frieren: Beyond Journey's End",
              "title_japanese": "葬送のフリーレン",
              "synopsis": "After the party defeats the Demon King...",
              "episodes": 28,
              "status": "Finished Airing",
              "duration": "24 min per ep",
              "rating": "PG-13 - Teens 13 or older",
              "score": 9.3,
              "season": "fall",
              "year": 2023,
              "images": { "jpg": { "image_url": "small.jpg", "large_image_url": "large.jpg" } },
              "aired": { "from": "2023-09-29T00:00:00+00:00" },
              "genres": [ { "mal_id": 2, "name": "Adventure" }, { "mal_id": 10, "name": "Fantasy" } ]
            }
          ]
        }
        """;

    private static JikanClient CreateClient(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri(ExternalApiConstants.JIKAN_BASE_URL) });

    private static string AnimeJson(string? duration, string? status = "Finished Airing") =>
        $$"""{ "data": { "mal_id": 1, "title": "Test", "duration": {{(duration is null ? "null" : $"\"{duration}\"")}}, "status": {{(status is null ? "null" : $"\"{status}\"")}} } }""";

    [Fact]
    public async Task SearchAnime_MapsAnimeAndPagination()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(SearchJson));

        var result = await client.SearchAnimeAsync("frieren", 2);

        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(30, result.TotalItems);
        Assert.True(result.HasNextPage);

        var anime = Assert.Single(result.Items);
        Assert.Equal(5114, anime.ExternalId);
        Assert.Equal("Frieren: Beyond Journey's End", anime.Title);
        Assert.Equal("葬送のフリーレン", anime.OriginalTitle);
        Assert.Equal("large.jpg", anime.ImageUrl);
        Assert.Equal(28, anime.EpisodeCount);
        Assert.Equal(24, anime.EpisodeDurationMinutes);
        Assert.Equal(MediaStatus.Finished, anime.Status);
        Assert.Equal(9.3m, anime.Score);
        Assert.Equal(2023, anime.Year);
        Assert.Equal(["Adventure", "Fantasy"], anime.Genres.Select(g => g.Name));
    }

    [Fact]
    public async Task SearchAnime_EscapesTheQueryAndAsksForSafeResults()
    {
        var handler = StubHttpMessageHandler.Json("""{ "data": [] }""");
        var client = CreateClient(handler);

        await client.SearchAnimeAsync("fate/zero & more", 1);

        var uri = Assert.Single(handler.Requests).RequestUri!;
        Assert.Contains("q=fate%2Fzero%20%26%20more", uri.AbsoluteUri);
        Assert.Contains("sfw=true", uri.Query);
    }

    [Fact]
    public async Task SearchAnime_WithoutResults_ReturnsAnEmptyPage()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("""{ "data": [] }"""));

        var result = await client.SearchAnimeAsync("nothing", 1);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task SearchAnime_WithBlankQuery_Throws()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.SearchAnimeAsync("  ", 1));
    }

    [Fact]
    public async Task SearchAnime_WhenFieldsAreMissing_UsesNulls()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("""{ "data": [ { "mal_id": 7 } ] }"""));

        var anime = Assert.Single((await client.SearchAnimeAsync("x", 1)).Items);

        Assert.Equal(7, anime.ExternalId);
        Assert.Equal(string.Empty, anime.Title);
        Assert.Null(anime.ImageUrl);
        Assert.Null(anime.EpisodeDurationMinutes);
        Assert.Null(anime.Status);
        Assert.Empty(anime.Genres);
    }

    [Fact]
    public async Task GetAnime_WhenNotFound_ReturnsNull()
    {
        var client = CreateClient(StubHttpMessageHandler.Status(HttpStatusCode.NotFound));

        Assert.Null(await client.GetAnimeAsync(999999));
    }

    [Fact]
    public async Task GetAnime_RequestsTheAnimeById()
    {
        var handler = StubHttpMessageHandler.Json(AnimeJson("24 min per ep"));
        var client = CreateClient(handler);

        var anime = await client.GetAnimeAsync(1);

        Assert.Equal("anime/1", Assert.Single(handler.Requests).RequestUri!.AbsolutePath.Split("/v4/")[1]);
        Assert.Equal("Test", anime!.Title);
    }

    [Theory]
    [InlineData("24 min per ep", 24)]
    [InlineData("1 hr 30 min", 90)]
    [InlineData("2 hr", 120)]
    [InlineData("Unknown", null)]
    [InlineData(null, null)]
    public async Task GetAnime_ParsesTheDuration(string? duration, int? expected)
    {
        var client = CreateClient(StubHttpMessageHandler.Json(AnimeJson(duration)));

        var anime = await client.GetAnimeAsync(1);

        Assert.Equal(expected, anime!.EpisodeDurationMinutes);
    }

    [Theory]
    [InlineData("Currently Airing", MediaStatus.Airing)]
    [InlineData("Finished Airing", MediaStatus.Finished)]
    [InlineData("Not yet aired", MediaStatus.Upcoming)]
    [InlineData("Something new", null)]
    public async Task GetAnime_MapsTheStatus(string status, MediaStatus? expected)
    {
        var client = CreateClient(StubHttpMessageHandler.Json(AnimeJson("24 min", status)));

        var anime = await client.GetAnimeAsync(1);

        Assert.Equal(expected, anime!.Status);
    }

    [Fact]
    public async Task GetGenres_MapsAndSkipsNamelessGenres()
    {
        var client = CreateClient(StubHttpMessageHandler.Json(
            """{ "data": [ { "mal_id": 1, "name": "Action" }, { "mal_id": 2, "name": null }, { "mal_id": 4, "name": "Comedy" } ] }"""));

        var genres = await client.GetGenresAsync();

        Assert.Equal(["Action", "Comedy"], genres.Select(g => g.Name));
        Assert.Equal([1, 4], genres.Select(g => g.ExternalId));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task Requests_WhenJikanFails_ThrowJikanApiException(HttpStatusCode status)
    {
        var client = CreateClient(StubHttpMessageHandler.Status(status));

        var exception = await Assert.ThrowsAsync<JikanApiException>(() => client.SearchAnimeAsync("x", 1));

        Assert.Equal(status, exception.StatusCode);
    }

    [Fact]
    public async Task Requests_WhenTheNetworkFails_ThrowJikanApiException()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => throw new HttpRequestException("offline")));

        await Assert.ThrowsAsync<JikanApiException>(() => client.GetGenresAsync());
    }

    [Fact]
    public async Task Requests_WhenTheAnswerIsNotJson_ThrowJikanApiException()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("<html>maintenance</html>"));

        await Assert.ThrowsAsync<JikanApiException>(() => client.GetGenresAsync());
    }

    [Fact]
    public async Task Requests_WhenTheCallerCancels_PropagateTheCancellation()
    {
        var client = CreateClient(StubHttpMessageHandler.Json("{}"));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetGenresAsync(cancellation.Token));
    }
}
