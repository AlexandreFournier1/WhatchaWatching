using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Middleware;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

public class FallbackAnimeProviderTests
{
    private readonly StubJikanClient _jikan = new() { Anime = FakeAnimeProvider.Anime(1, "From Jikan") };
    private readonly StubAniListClient _aniList = new() { Anime = FakeAnimeProvider.Anime(1, "From AniList") };
    private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));
    private readonly FallbackAnimeProvider _provider;

    public FallbackAnimeProviderTests()
    {
        _provider = new FallbackAnimeProvider(
            _jikan, _aniList, new ProviderCooldown(_time), NullLogger<FallbackAnimeProvider>.Instance);
    }

    [Fact]
    public async Task Search_WhenJikanAnswers_DoesNotAskAniList()
    {
        var result = await _provider.SearchAnimeAsync("x", 1);

        Assert.Equal("From Jikan", Assert.Single(result.Items).Title);
        Assert.Equal(0, _aniList.Calls);
    }

    [Fact]
    public async Task Search_WhenJikanFails_UsesAniList()
    {
        _jikan.Failure = new ExternalApiException("down");

        var result = await _provider.SearchAnimeAsync("x", 1);

        Assert.Equal("From AniList", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task Get_WhenJikanFails_UsesAniList()
    {
        _jikan.Failure = new ExternalApiException("down");

        var anime = await _provider.GetAnimeAsync(1);

        Assert.Equal("From AniList", anime!.Title);
    }

    [Fact]
    public async Task Requests_AfterAJikanFailure_SkipJikanForAWhile()
    {
        _jikan.Failure = new ExternalApiException("down");
        await _provider.GetAnimeAsync(1);

        await _provider.GetAnimeAsync(1);
        await _provider.GetAnimeAsync(1);

        Assert.Equal(1, _jikan.Calls);
        Assert.Equal(3, _aniList.Calls);
    }

    [Fact]
    public async Task Requests_WhenTheCooldownEnds_AskJikanAgain()
    {
        _jikan.Failure = new ExternalApiException("down");
        await _provider.GetAnimeAsync(1);

        _jikan.Failure = null;
        _time.Advance(ProviderCooldown.Duration + TimeSpan.FromSeconds(1));
        var anime = await _provider.GetAnimeAsync(1);

        Assert.Equal("From Jikan", anime!.Title);
        Assert.Equal(2, _jikan.Calls);
    }

    [Fact]
    public async Task Requests_WhenJikanIsSkippedAndAniListFails_TryJikanAnyway()
    {
        _jikan.Failure = new ExternalApiException("down");
        await _provider.GetAnimeAsync(1);

        _jikan.Failure = null;
        _aniList.Failure = new ExternalApiException("down too");
        var anime = await _provider.GetAnimeAsync(1);

        Assert.Equal("From Jikan", anime!.Title);
    }

    [Fact]
    public async Task Requests_WhenNoProviderAnswers_ThrowWithBothFailures()
    {
        _jikan.Failure = new ExternalApiException("jikan down");
        _aniList.Failure = new ExternalApiException("anilist down");

        var exception = await Assert.ThrowsAsync<ExternalApiException>(() => _provider.SearchAnimeAsync("x", 1));

        var failures = Assert.IsType<AggregateException>(exception.InnerException);
        Assert.Equal(["jikan down", "anilist down"], failures.InnerExceptions.Select(e => e.Message));
    }

    [Fact]
    public async Task Get_WhenJikanSaysNotFound_DoesNotAskAniList()
    {
        _jikan.Anime = null;

        var anime = await _provider.GetAnimeAsync(404);

        Assert.Null(anime);
        Assert.Equal(0, _aniList.Calls);
    }

    [Fact]
    public async Task Requests_WhenTheCallerCancels_DoNotFallBack()
    {
        _jikan.Failure = new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(() => _provider.GetAnimeAsync(1));

        Assert.Equal(0, _aniList.Calls);
    }

    [Fact]
    public async Task Search_PassesTheAdultChoiceToTheProvider()
    {
        await _provider.SearchAnimeAsync("x", 1, includeAdult: true);
        Assert.True(_jikan.LastIncludeAdult);

        _jikan.Failure = new ExternalApiException("down");
        await _provider.SearchAnimeAsync("x", 1);
        Assert.False(_aniList.LastIncludeAdult);
    }
}
