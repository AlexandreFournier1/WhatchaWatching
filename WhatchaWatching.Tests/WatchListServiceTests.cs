using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Exceptions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Jikan;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

public class WatchListServiceTests
{
    private const int User = 1;
    private const int OtherUser = 2;

    private readonly FakeJikanClient _jikan = new();
    private readonly TestDbContextFactory _db = new();
    private readonly WatchListService _service;

    public WatchListServiceTests()
    {
        var animeService = new AnimeService(_jikan, _db, TimeProvider.System, NullLogger<AnimeService>.Instance);
        _service = new WatchListService(_db, animeService);
    }

    private async Task AddAsync(int userId, params ExternalAnimeDto[] animes)
    {
        _jikan.With(animes);
        foreach (var anime in animes)
        {
            Assert.Equal(WatchListAddResult.Added, await _service.AddAsync(userId, anime.ExternalId));
        }
    }

    private async Task<int[]> ListedIdsAsync(
        WatchListFilterDto? filter = null, WatchListSort sort = WatchListSort.RecentlyAdded) =>
        (await _service.GetAsync(User, filter, sort)).Select(a => a.Id).ToArray();

    [Fact]
    public async Task Add_NewAnime_StoresItAndPutsItInTheList()
    {
        _jikan.With(FakeJikanClient.Anime(1, "First"));

        var result = await _service.AddAsync(User, 1);

        Assert.Equal(WatchListAddResult.Added, result);
        var anime = Assert.Single(await _service.GetAsync(User));
        Assert.Equal("First", anime.Title);
    }

    [Fact]
    public async Task Add_TheSameAnimeTwice_KeepsOneItem()
    {
        _jikan.With(FakeJikanClient.Anime(1));
        await _service.AddAsync(User, 1);

        var result = await _service.AddAsync(User, 1);

        Assert.Equal(WatchListAddResult.AlreadyInList, result);
        Assert.Single(await _service.GetAsync(User));
    }

    [Fact]
    public async Task Add_UnknownAnime_ReportsNotFound()
    {
        Assert.Equal(WatchListAddResult.AnimeNotFound, await _service.AddAsync(User, 404));
    }

    [Fact]
    public async Task Add_WhenJikanIsDownAndTheAnimeIsNotStored_ThrowsProviderUnavailable()
    {
        _jikan.Failure = new JikanApiException("down");

        await Assert.ThrowsAsync<AnimeProviderUnavailableException>(() => _service.AddAsync(User, 1));
    }

    [Fact]
    public async Task Lists_BelongToTheirOwner()
    {
        await AddAsync(User, FakeJikanClient.Anime(1));
        await AddAsync(OtherUser, FakeJikanClient.Anime(2));

        Assert.Equal(new[] { 1 }, await ListedIdsAsync());
        Assert.Equal([2], (await _service.GetAsync(OtherUser)).Select(a => a.Id));
    }

    [Fact]
    public async Task Two_UsersCanListTheSameAnime()
    {
        _jikan.With(FakeJikanClient.Anime(1));

        Assert.Equal(WatchListAddResult.Added, await _service.AddAsync(User, 1));
        Assert.Equal(WatchListAddResult.Added, await _service.AddAsync(OtherUser, 1));
    }

    [Fact]
    public async Task Remove_ListedAnime_RemovesOnlyThatItem()
    {
        await AddAsync(User, FakeJikanClient.Anime(1), FakeJikanClient.Anime(2));

        Assert.True(await _service.RemoveAsync(User, 1));

        Assert.Equal(new[] { 2 }, await ListedIdsAsync());
        await using var context = _db.CreateDbContext();
        Assert.Equal(2, await context.Media.CountAsync());
    }

    [Fact]
    public async Task Remove_AnimeNotInTheList_ReturnsFalse()
    {
        await AddAsync(OtherUser, FakeJikanClient.Anime(1));

        Assert.False(await _service.RemoveAsync(User, 1));
        Assert.Single(await _service.GetAsync(OtherUser));
    }

    [Fact]
    public async Task Get_DefaultOrder_PutsTheLatestAddedFirst()
    {
        await AddAsync(User, FakeJikanClient.Anime(1), FakeJikanClient.Anime(2), FakeJikanClient.Anime(3));

        Assert.Equal(new[] { 3, 2, 1 }, await ListedIdsAsync());
    }

    [Fact]
    public async Task Get_SortedByTitle_OrdersAlphabetically()
    {
        await AddAsync(User,
            FakeJikanClient.Anime(1, "Charlie"), FakeJikanClient.Anime(2, "Alpha"), FakeJikanClient.Anime(3, "Bravo"));

        Assert.Equal(new[] { 2, 3, 1 }, await ListedIdsAsync(sort: WatchListSort.Title));
    }

    [Fact]
    public async Task Get_SortedByShortest_PutsUnknownDurationsLast()
    {
        await AddAsync(User,
            FakeJikanClient.Anime(1, minutes: 24), FakeJikanClient.Anime(2, minutes: null), FakeJikanClient.Anime(3, minutes: 10));

        Assert.Equal(new[] { 3, 1, 2 }, await ListedIdsAsync(sort: WatchListSort.ShortestFirst));
    }

    [Fact]
    public async Task Get_FilteredByGenre_KeepsAnimeWithAnyOfTheGenres()
    {
        await AddAsync(User,
            FakeJikanClient.Anime(1, genres: "Action"),
            FakeJikanClient.Anime(2, genres: "Comedy"),
            FakeJikanClient.Anime(3, genres: ["Action", "Comedy"]),
            FakeJikanClient.Anime(4, genres: "Drama"));

        var comedy = await ListedIdsAsync(new WatchListFilterDto(["Comedy"], null), WatchListSort.Title);
        var either = await ListedIdsAsync(new WatchListFilterDto(["Comedy", "Drama"], null));

        Assert.Equal([2, 3], comedy.Order());
        Assert.Equal([2, 3, 4], either.Order());
    }

    [Theory]
    [InlineData(DurationRange.Short, new[] { 1 })]
    [InlineData(DurationRange.Medium, new[] { 2, 3 })]
    [InlineData(DurationRange.Long, new[] { 4 })]
    public async Task Get_FilteredByDuration_UsesTheDurationRanges(DurationRange range, int[] expected)
    {
        await AddAsync(User,
            FakeJikanClient.Anime(1, minutes: 14),
            FakeJikanClient.Anime(2, minutes: 15),
            FakeJikanClient.Anime(3, minutes: 25),
            FakeJikanClient.Anime(4, minutes: 26),
            FakeJikanClient.Anime(5, minutes: null));

        var ids = await ListedIdsAsync(new WatchListFilterDto([], range));

        Assert.Equal(expected, ids.Order());
    }

    [Fact]
    public async Task Get_WithGenreAndDurationFilters_AppliesBoth()
    {
        await AddAsync(User,
            FakeJikanClient.Anime(1, minutes: 10, genres: "Comedy"),
            FakeJikanClient.Anime(2, minutes: 10, genres: "Drama"),
            FakeJikanClient.Anime(3, minutes: 30, genres: "Comedy"));

        Assert.Equal(new[] { 1 }, await ListedIdsAsync(new WatchListFilterDto(["Comedy"], DurationRange.Short)));
    }

    [Fact]
    public async Task GetGenres_ReturnsTheDistinctGenresOfTheListSorted()
    {
        await AddAsync(User,
            FakeJikanClient.Anime(1, genres: ["Comedy", "Action"]), FakeJikanClient.Anime(2, genres: "Comedy"));
        await AddAsync(OtherUser, FakeJikanClient.Anime(3, genres: "Drama"));

        Assert.Equal(["Action", "Comedy"], await _service.GetGenresAsync(User));
    }

    [Fact]
    public async Task GetListedIds_ReturnsOnlyTheListedOnesOfThisUser()
    {
        await AddAsync(User, FakeJikanClient.Anime(1));
        await AddAsync(OtherUser, FakeJikanClient.Anime(2));

        var listed = await _service.GetListedIdsAsync(User, [1, 2, 3]);

        Assert.Equal([1], listed);
        Assert.Empty(await _service.GetListedIdsAsync(User, []));
    }
}
