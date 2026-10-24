using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Exceptions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Jikan;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

public class AnimeServiceTests
{
    private readonly FakeJikanClient _jikan = new();
    private readonly TestDbContextFactory _db = new();
    private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));

    private AnimeService CreateService() => new(_jikan, _db, _time, NullLogger<AnimeService>.Instance);

    [Fact]
    public async Task Search_ReturnsSummariesAndPaging()
    {
        _jikan.With(FakeJikanClient.Anime(1, "Frieren", 24, "Fantasy", "Adventure"), FakeJikanClient.Anime(2, "Other"));

        var result = await CreateService().SearchAsync("frieren", 1);

        var anime = Assert.Single(result.Items);
        Assert.Equal(1, anime.Id);
        Assert.Equal("Frieren", anime.Title);
        Assert.Equal(["Adventure", "Fantasy"], anime.Genres);
        Assert.Equal("Finished", anime.Status);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task Search_WithBlankQuery_ReturnsNothingWithoutCallingJikan()
    {
        _jikan.Failure = new JikanApiException("down");

        var result = await CreateService().SearchAsync("   ", 1);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Search_WhenJikanFails_ThrowsProviderUnavailable()
    {
        _jikan.Failure = new JikanApiException("down");

        await Assert.ThrowsAsync<AnimeProviderUnavailableException>(() => CreateService().SearchAsync("x", 1));
    }

    [Fact]
    public async Task Get_FirstTime_FetchesAndStoresTheAnimeWithItsGenres()
    {
        _jikan.With(FakeJikanClient.Anime(7, "Stored", 24, "Action", "Comedy"));

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
        _jikan.With(FakeJikanClient.Anime(1));
        var service = CreateService();
        await service.GetAsync(1);

        _time.Advance(TimeSpan.FromDays(6));
        await service.GetAsync(1);

        Assert.Equal(1, _jikan.GetAnimeCalls);
    }

    [Fact]
    public async Task Get_OutdatedCopy_RefreshesItAndReplacesTheGenres()
    {
        _jikan.With(FakeJikanClient.Anime(1, "Old title", 24, "Action", "Comedy"));
        var service = CreateService();
        await service.GetAsync(1);

        _jikan.With(FakeJikanClient.Anime(1, "New title", 24, "Action", "Drama"));
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
        _jikan.With(FakeJikanClient.Anime(1, "One", 24, "Action"), FakeJikanClient.Anime(2, "Two", 24, "Action"));
        var service = CreateService();

        await service.GetAsync(1);
        await service.GetAsync(2);

        await using var context = _db.CreateDbContext();
        Assert.Equal(1, await context.Genres.CountAsync());
        Assert.Equal(2, await context.MediaGenres.CountAsync());
    }

    [Fact]
    public async Task Get_WhenJikanFailsButACopyExists_ReturnsTheOldCopy()
    {
        _jikan.With(FakeJikanClient.Anime(1, "Kept"));
        var service = CreateService();
        await service.GetAsync(1);

        _time.Advance(TimeSpan.FromDays(30));
        _jikan.Failure = new JikanApiException("down");
        var anime = await service.GetAsync(1);

        Assert.Equal("Kept", anime!.Title);
    }

    [Fact]
    public async Task Get_WhenJikanFailsAndNothingIsStored_ThrowsProviderUnavailable()
    {
        _jikan.Failure = new JikanApiException("down");

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
        var anime = FakeJikanClient.Anime(1) with { Title = new string('t', 1000), Rating = new string('r', 200) };
        _jikan.With(anime);

        await CreateService().GetAsync(1);

        await using var context = _db.CreateDbContext();
        var media = await context.Media.SingleAsync();
        Assert.Equal(512, media.Title.Length);
        Assert.Equal(64, media.Rating!.Length);
    }
}
