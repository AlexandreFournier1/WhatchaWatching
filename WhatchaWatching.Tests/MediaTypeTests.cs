using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

/// <summary>Provider ids repeat from one type to another: these tests list a movie and an anime with the same id.</summary>
public class MediaTypeTests
{
    private const int User = 1;
    private const int SharedId = 7;

    private readonly FakeAnimeProvider _provider = new();
    private readonly TestDbContextFactory _db = new();
    private readonly MediaService _media;
    private readonly WatchListService _watchList;
    private readonly RandomPickerService _picker;

    public MediaTypeTests()
    {
        _media = new MediaService(_provider, _db, TimeProvider.System, NullLogger<MediaService>.Instance);
        _watchList = new WatchListService(_db, _media);
        _picker = new RandomPickerService(_db, new Random(42));
    }

    // Movies cannot be fetched yet, so the movie is stored by hand as it will be once TMDB is wired in.
    private async Task ListAnimeAndMovieWithTheSameIdAsync()
    {
        _provider.With(FakeAnimeProvider.Anime(SharedId, "The anime", 24, "Action"));
        await _watchList.AddAsync(User, MediaType.Anime, SharedId);

        await using var context = _db.CreateDbContext();
        var movie = new MediaEntity
        {
            Type = MediaType.Movie,
            ExternalId = SharedId,
            Title = "The movie",
            Status = MediaStatus.Finished,
            EpisodeDurationMinutes = 120
        };
        movie.WatchListItems.Add(new WatchListItemEntity { UserId = User });
        context.Media.Add(movie);
        await context.SaveChangesAsync();
    }

    [Theory]
    [InlineData(MediaType.Movie)]
    [InlineData(MediaType.TVShow)]
    public async Task Search_ForATypeWithoutProvider_Throws(MediaType type)
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => _media.SearchAsync(type, "x", 1));
    }

    [Theory]
    [InlineData(MediaType.Movie)]
    [InlineData(MediaType.TVShow)]
    public async Task Get_ForATypeWithoutProvider_Throws(MediaType type)
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => _media.GetAsync(type, 1));
    }

    [Fact]
    public async Task Get_ReturnsTheTypeOfTheAnime()
    {
        _provider.With(FakeAnimeProvider.Anime(1));

        var anime = await _media.GetAsync(MediaType.Anime, 1);

        Assert.Equal(MediaType.Anime, anime!.Type);
    }

    [Fact]
    public async Task GetListedIds_OnlyCountsTheGivenType()
    {
        await ListAnimeAndMovieWithTheSameIdAsync();

        Assert.Equal([SharedId], await _watchList.GetListedIdsAsync(User, MediaType.Anime, [SharedId]));
        Assert.Equal([SharedId], await _watchList.GetListedIdsAsync(User, MediaType.Movie, [SharedId]));
        Assert.Empty(await _watchList.GetListedIdsAsync(User, MediaType.TVShow, [SharedId]));
    }

    [Fact]
    public async Task Get_WithoutATypeFilter_ReturnsEveryType()
    {
        await ListAnimeAndMovieWithTheSameIdAsync();

        var items = await _watchList.GetAsync(User);

        Assert.Equal([MediaType.Anime, MediaType.Movie], items.Select(i => i.Type).Order());
    }

    [Theory]
    [InlineData(MediaType.Anime, "The anime")]
    [InlineData(MediaType.Movie, "The movie")]
    public async Task Get_WithATypeFilter_KeepsThatTypeOnly(MediaType type, string title)
    {
        await ListAnimeAndMovieWithTheSameIdAsync();

        var items = await _watchList.GetAsync(User, WatchListFilterDto.None with { Type = type });

        Assert.Equal(title, Assert.Single(items).Title);
    }

    [Fact]
    public async Task GetGenres_CanBeLimitedToOneType()
    {
        await ListAnimeAndMovieWithTheSameIdAsync();

        Assert.Equal(["Action"], await _watchList.GetGenresAsync(User, MediaType.Anime));
        Assert.Empty(await _watchList.GetGenresAsync(User, MediaType.Movie));
        Assert.Equal(["Action"], await _watchList.GetGenresAsync(User));
    }

    [Fact]
    public async Task Remove_OnlyRemovesTheTitleOfTheGivenType()
    {
        await ListAnimeAndMovieWithTheSameIdAsync();

        Assert.True(await _watchList.RemoveAsync(User, MediaType.Movie, SharedId));
        Assert.False(await _watchList.RemoveAsync(User, MediaType.TVShow, SharedId));

        var remaining = Assert.Single(await _watchList.GetAsync(User));
        Assert.Equal(MediaType.Anime, remaining.Type);
    }

    [Fact]
    public async Task Pick_ExcludesTheTitleOfTheGivenTypeOnly()
    {
        await ListAnimeAndMovieWithTheSameIdAsync();

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var pick = await _picker.PickAsync(User, exclude: new MediaKey(MediaType.Anime, SharedId));

            Assert.Equal(MediaType.Movie, pick!.Type);
        }
    }

    [Fact]
    public async Task Pick_WithATypeFilter_PicksThatTypeOnly()
    {
        await ListAnimeAndMovieWithTheSameIdAsync();

        var pick = await _picker.PickAsync(User, WatchListFilterDto.None with { Type = MediaType.Movie });

        Assert.Equal("The movie", pick!.Title);
    }
}
