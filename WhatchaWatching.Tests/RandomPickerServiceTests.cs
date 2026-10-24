using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

public class RandomPickerServiceTests
{
    private const int User = 1;

    private readonly FakeAnimeProvider _provider = new();
    private readonly TestDbContextFactory _db = new();
    private readonly WatchListService _watchList;
    private readonly RandomPickerService _picker;

    public RandomPickerServiceTests()
    {
        var animeService = new AnimeService(_provider, _db, TimeProvider.System, NullLogger<AnimeService>.Instance);
        _watchList = new WatchListService(_db, animeService);
        _picker = new RandomPickerService(_db, new Random(42));
    }

    private async Task AddAsync(int userId, params ExternalAnimeDto[] animes)
    {
        _provider.With(animes);
        foreach (var anime in animes)
        {
            await _watchList.AddAsync(userId, anime.ExternalId);
        }
    }

    [Fact]
    public async Task Pick_FromAnEmptyList_ReturnsNull()
    {
        Assert.Null(await _picker.PickAsync(User));
    }

    [Fact]
    public async Task Pick_ReturnsTheSummaryWithItsGenres()
    {
        await AddAsync(User, FakeAnimeProvider.Anime(1, "Only one", 24, "Fantasy", "Action"));

        var anime = await _picker.PickAsync(User);

        Assert.Equal("Only one", anime!.Title);
        Assert.Equal(["Action", "Fantasy"], anime.Genres);
    }

    [Fact]
    public async Task Pick_EventuallyReturnsEveryAnimeOfTheList()
    {
        await AddAsync(User, FakeAnimeProvider.Anime(1), FakeAnimeProvider.Anime(2), FakeAnimeProvider.Anime(3));

        var picked = new HashSet<int>();
        for (var i = 0; i < 60; i++)
        {
            picked.Add((await _picker.PickAsync(User))!.Id);
        }

        Assert.Equal([1, 2, 3], picked.Order());
    }

    [Fact]
    public async Task Pick_OnlyConsidersTheListOfTheUser()
    {
        await AddAsync(User, FakeAnimeProvider.Anime(1));
        await AddAsync(2, FakeAnimeProvider.Anime(2));

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(1, (await _picker.PickAsync(User))!.Id);
        }
    }

    [Fact]
    public async Task Pick_RespectsTheFilters()
    {
        await AddAsync(User,
            FakeAnimeProvider.Anime(1, minutes: 10, genres: "Comedy"),
            FakeAnimeProvider.Anime(2, minutes: 30, genres: "Comedy"),
            FakeAnimeProvider.Anime(3, minutes: 10, genres: "Drama"));

        for (var i = 0; i < 10; i++)
        {
            var anime = await _picker.PickAsync(User, new WatchListFilterDto(["Comedy"], DurationRange.Short));
            Assert.Equal(1, anime!.Id);
        }
    }

    [Fact]
    public async Task Pick_WhenNothingMatches_ReturnsNull()
    {
        await AddAsync(User, FakeAnimeProvider.Anime(1, genres: "Comedy"));

        Assert.Null(await _picker.PickAsync(User, new WatchListFilterDto(["Drama"], null)));
    }

    [Fact]
    public async Task Pick_WithAnExcludedAnime_NeverRepeatsItWhenThereIsAChoice()
    {
        await AddAsync(User, FakeAnimeProvider.Anime(1), FakeAnimeProvider.Anime(2));

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(2, (await _picker.PickAsync(User, excludeExternalId: 1))!.Id);
        }
    }

    [Fact]
    public async Task Pick_WithTheOnlyAnimeExcluded_ReturnsItAnyway()
    {
        await AddAsync(User, FakeAnimeProvider.Anime(1));

        Assert.Equal(1, (await _picker.PickAsync(User, excludeExternalId: 1))!.Id);
    }
}
