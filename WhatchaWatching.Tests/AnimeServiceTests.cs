using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Exceptions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

public class AnimeServiceTests
{
    private readonly FakeAnimeProvider _provider = new();
    private readonly TestDbContextFactory _db = new();
    private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));

    private AnimeService CreateService() => new(_provider, _db, _time, NullLogger<AnimeService>.Instance);

    [Fact]
    public async Task Search_ReturnsSummariesAndPaging()
    {
        _provider.With(FakeAnimeProvider.Anime(1, "Frieren", 24, "Fantasy", "Adventure"), FakeAnimeProvider.Anime(2, "Other"));

        var result = await CreateService().SearchAsync("frieren", 1);

        var anime = Assert.Single(result.Results.Items);
        Assert.Equal(1, anime.Id);
        Assert.Equal("Frieren", anime.Title);
        Assert.Equal(["Adventure", "Fantasy"], anime.Genres);
        Assert.Equal("Finished", anime.Status);
        Assert.Equal(2, result.Results.TotalPages);
        Assert.False(result.IsLocalOnly);
    }

    [Fact]
    public async Task Search_WithBlankQuery_ReturnsNothingWithoutCallingAProvider()
    {
        _provider.Failure = new ExternalApiException("down");

        var result = await CreateService().SearchAsync("   ", 1);

        Assert.Empty(result.Results.Items);
        Assert.False(result.IsLocalOnly);
    }

    [Fact]
    public async Task Search_WhenNoProviderAnswersAndNothingIsStored_ThrowsProviderUnavailable()
    {
        _provider.Failure = new ExternalApiException("down");

        await Assert.ThrowsAsync<AnimeProviderUnavailableException>(() => CreateService().SearchAsync("x", 1));
    }

    [Fact]
    public async Task Search_WhenNoProviderAnswers_SearchesTheStoredAnime()
    {
        _provider.With(
            FakeAnimeProvider.Anime(1, "Frieren", 24, "Fantasy"),
            FakeAnimeProvider.Anime(2, "Other show", 24, "Drama"),
            FakeAnimeProvider.Anime(3, "Frieren Special", 12, "Fantasy"));
        var service = CreateService();
        await service.GetAsync(1);
        await service.GetAsync(2);
        await service.GetAsync(3);

        _provider.Failure = new ExternalApiException("down");
        var result = await service.SearchAsync("Frieren", 1);

        Assert.True(result.IsLocalOnly);
        Assert.Equal(["Frieren", "Frieren Special"], result.Results.Items.Select(a => a.Title));
        Assert.Equal(2, result.Results.TotalItems);
        Assert.Equal(1, result.Results.TotalPages);
        Assert.Equal(["Fantasy"], result.Results.Items[0].Genres);
    }

    [Fact]
    public async Task Search_WhenNoProviderAnswers_PagesTheStoredAnime()
    {
        var animes = Enumerable.Range(1, 15).Select(i => FakeAnimeProvider.Anime(i, $"Show {i:00}")).ToArray();
        _provider.With(animes);
        var service = CreateService();
        foreach (var anime in animes)
        {
            await service.GetAsync(anime.ExternalId);
        }

        _provider.Failure = new ExternalApiException("down");
        var second = await service.SearchAsync("Show", 2);

        Assert.Equal(3, second.Results.Items.Count);
        Assert.Equal(2, second.Results.TotalPages);
        Assert.Equal(2, second.Results.Page);
    }

    [Fact]
    public async Task Get_FirstTime_FetchesAndStoresTheAnimeWithItsGenres()
    {
        _provider.With(FakeAnimeProvider.Anime(7, "Stored", 24, "Action", "Comedy"));

        var anime = await CreateService().GetAsync(7);

        Assert.Equal("Stored", anime!.Title);
        await using var context = _db.CreateDbContext();
        var media = await context.Media.Include(m => m.MediaGenres).ThenInclude(mg => mg.Genre).SingleAsync();
        Assert.Equal(MediaType.Anime, media.Type);
        Assert.Equal(7, media.ExternalId);
        Assert.Equal(MediaStatus.Finished, media.Status);
        Assert.NotNull(media.LastSyncedAt);
        Assert.Equal(["Action", "Comedy"], media.MediaGenres.Select(mg => mg.Genre.Name).Order());
    }

    [Fact]
    public async Task Get_RecentlySynced_UsesTheStoredCopy()
    {
        _provider.With(FakeAnimeProvider.Anime(1));
        var service = CreateService();
        await service.GetAsync(1);

        _time.Advance(TimeSpan.FromDays(6));
        await service.GetAsync(1);

        Assert.Equal(1, _provider.GetAnimeCalls);
    }

    [Fact]
    public async Task Get_OutdatedCopy_RefreshesItAndReplacesTheGenres()
    {
        _provider.With(FakeAnimeProvider.Anime(1, "Old title", 24, "Action", "Comedy"));
        var service = CreateService();
        await service.GetAsync(1);

        _provider.With(FakeAnimeProvider.Anime(1, "New title", 24, "Action", "Drama"));
        _time.Advance(TimeSpan.FromDays(8));
        var anime = await service.GetAsync(1);

        Assert.Equal("New title", anime!.Title);
        Assert.Equal(["Action", "Drama"], anime.Genres);
        await using var context = _db.CreateDbContext();
        Assert.Equal(1, await context.Media.CountAsync());
        Assert.Equal(2, await context.MediaGenres.CountAsync());
    }

    [Fact]
    public async Task Get_AnimeSharingAGenre_ReusesTheGenre()
    {
        _provider.With(FakeAnimeProvider.Anime(1, "One", 24, "Action"), FakeAnimeProvider.Anime(2, "Two", 24, "Action"));
        var service = CreateService();

        await service.GetAsync(1);
        await service.GetAsync(2);

        await using var context = _db.CreateDbContext();
        Assert.Equal(1, await context.Genres.CountAsync());
        Assert.Equal(2, await context.MediaGenres.CountAsync());
    }

    [Fact]
    public async Task Get_WhenNoProviderAnswersButACopyExists_ReturnsTheOldCopy()
    {
        _provider.With(FakeAnimeProvider.Anime(1, "Kept"));
        var service = CreateService();
        await service.GetAsync(1);

        _time.Advance(TimeSpan.FromDays(30));
        _provider.Failure = new ExternalApiException("down");
        var anime = await service.GetAsync(1);

        Assert.Equal("Kept", anime!.Title);
    }

    [Fact]
    public async Task Get_WhenNoProviderAnswersAndNothingIsStored_ThrowsProviderUnavailable()
    {
        _provider.Failure = new ExternalApiException("down");

        await Assert.ThrowsAsync<AnimeProviderUnavailableException>(() => CreateService().GetAsync(1));
    }

    [Fact]
    public async Task Get_UnknownAnime_ReturnsNull()
    {
        Assert.Null(await CreateService().GetAsync(404));
    }

    [Fact]
    public async Task Get_WithValuesLongerThanTheColumns_StoresTruncatedValues()
    {
        var anime = FakeAnimeProvider.Anime(1) with { Title = new string('t', 1000), Rating = new string('r', 200) };
        _provider.With(anime);

        await CreateService().GetAsync(1);

        await using var context = _db.CreateDbContext();
        var media = await context.Media.SingleAsync();
        Assert.Equal(512, media.Title.Length);
        Assert.Equal(64, media.Rating!.Length);
    }
}
