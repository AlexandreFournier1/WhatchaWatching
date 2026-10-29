using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Exceptions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Tmdb;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

/// <summary>Movies and TV series go through the same services as anime, with TMDB as their provider.</summary>
public class TmdbMediaServiceTests
{
    private const int User = 1;

    private readonly FakeAnimeProvider _anime = new();
    private readonly FakeTmdbClient _tmdb = new();
    private readonly TestDbContextFactory _db = new();
    private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly MediaService _media;
    private readonly WatchListService _watchList;

    public TmdbMediaServiceTests()
    {
        _media = new MediaService(_anime, _tmdb, _db, _time, NullLogger<MediaService>.Instance);
        _watchList = new WatchListService(_db, _media);
    }

    [Fact]
    public async Task Search_Movies_AsksTmdbAndNotTheAnimeProviders()
    {
        _tmdb.With(FakeTmdbClient.Movie(1, "The Matrix"), FakeTmdbClient.Series(2, "Matrix Show"));

        var result = await _media.SearchAsync(MediaType.Movie, "matrix", 1);

        var movie = Assert.Single(result.Results.Items);
        Assert.Equal((MediaType.Movie, "The Matrix"), (movie.Type, movie.Title));
        Assert.Equal(0, _anime.GetAnimeCalls);
        Assert.False(result.IsLocalOnly);
    }

    [Fact]
    public async Task Search_TvShows_ReturnsTheSeasons()
    {
        _tmdb.With(FakeTmdbClient.Series(2, "Matrix Show"));

        var show = Assert.Single((await _media.SearchAsync(MediaType.TVShow, "matrix", 1)).Results.Items);

        Assert.Equal(2, show.Seasons);
        Assert.Equal(24, show.Episodes);
    }

    [Fact]
    public async Task Search_KeepsAdultTitlesOutUnlessAsked()
    {
        _tmdb.With(FakeTmdbClient.Movie(1, "Adult one") with { IsAdult = true });

        var safe = await _media.SearchAsync(MediaType.Movie, "adult", 1);
        var all = await _media.SearchAsync(MediaType.Movie, "adult", 1, includeAdult: true);

        Assert.Empty(safe.Results.Items);
        Assert.Single(all.Results.Items);
        Assert.True(_tmdb.LastIncludeAdult);
    }

    [Fact]
    public async Task Search_WhenTmdbFails_SearchesTheStoredMovies()
    {
        _tmdb.With(FakeTmdbClient.Movie(1, "Stored movie"));
        await _media.GetAsync(MediaType.Movie, 1);
        _tmdb.Failure = new TmdbApiException("down");

        var result = await _media.SearchAsync(MediaType.Movie, "Stored", 1);

        Assert.True(result.IsLocalOnly);
        Assert.Equal("Stored movie", Assert.Single(result.Results.Items).Title);
    }

    [Fact]
    public async Task Search_WhenTmdbFailsAndNothingIsStored_ThrowsUnavailable()
    {
        _tmdb.Failure = new TmdbApiException("down");

        await Assert.ThrowsAsync<MediaProviderUnavailableException>(() => _media.SearchAsync(MediaType.Movie, "x", 1));
    }

    [Fact]
    public async Task Search_StoredTitlesOfAnotherTypeAreNotReturned()
    {
        _anime.With(FakeAnimeProvider.Anime(1, "Matrix anime"));
        await _media.GetAsync(MediaType.Anime, 1);
        _tmdb.Failure = new TmdbApiException("down");

        await Assert.ThrowsAsync<MediaProviderUnavailableException>(() => _media.SearchAsync(MediaType.Movie, "matrix", 1));
    }

    [Fact]
    public async Task Get_Movie_StoresItWithItsGenresAndPlatforms()
    {
        _tmdb.With(FakeTmdbClient.Movie(603, "The Matrix", 136, (28, "Action"), (878, "Science Fiction")) with
        {
            StreamingLinks = [new StreamingLinkDto("Netflix", "https://tmdb.test/watch"), new StreamingLinkDto("Prime Video", "https://tmdb.test/watch")]
        });

        var movie = await _media.GetAsync(MediaType.Movie, 603);

        Assert.Equal(MediaType.Movie, movie!.Type);
        Assert.Equal(136, movie.EpisodeDurationMinutes);
        Assert.Equal(["Action", "Science Fiction"], movie.Genres);
        Assert.Equal(["Netflix", "Prime Video"], movie.StreamingLinks.Select(l => l.Site));

        await using var context = _db.CreateDbContext();
        var stored = await context.Media.Include(m => m.StreamingLinks).SingleAsync();
        Assert.Equal(MediaType.Movie, stored.Type);
        Assert.Equal(2, stored.StreamingLinks.Count);
    }

    [Fact]
    public async Task Get_TvShow_StoresTheSeasons()
    {
        _tmdb.With(FakeTmdbClient.Series(1396));

        var show = await _media.GetAsync(MediaType.TVShow, 1396);

        Assert.Equal(2, show!.Seasons);
        await using var context = _db.CreateDbContext();
        Assert.Equal(2, (await context.Media.SingleAsync()).SeasonCount);
    }

    [Fact]
    public async Task Get_UsesTheStoredCopyForAWeek()
    {
        _tmdb.With(FakeTmdbClient.Movie(1));
        await _media.GetAsync(MediaType.Movie, 1);

        _time.Advance(TimeSpan.FromDays(6));
        await _media.GetAsync(MediaType.Movie, 1);
        Assert.Equal(1, _tmdb.GetCalls);

        _time.Advance(TimeSpan.FromDays(2));
        await _media.GetAsync(MediaType.Movie, 1);
        Assert.Equal(2, _tmdb.GetCalls);
    }

    [Fact]
    public async Task Get_WhenTmdbFails_ServesTheStoredCopyOrThrows()
    {
        _tmdb.With(FakeTmdbClient.Movie(1, "Known"));
        await _media.GetAsync(MediaType.Movie, 1);
        _time.Advance(TimeSpan.FromDays(30));
        _tmdb.Failure = new TmdbApiException("down");

        Assert.Equal("Known", (await _media.GetAsync(MediaType.Movie, 1))!.Title);
        await Assert.ThrowsAsync<MediaProviderUnavailableException>(() => _media.GetAsync(MediaType.Movie, 2));
    }

    [Fact]
    public async Task Get_UnknownTitle_ReturnsNull()
    {
        Assert.Null(await _media.GetAsync(MediaType.Movie, 999));
    }

    [Fact]
    public async Task Get_AdultTitle_IsHiddenAndNotStoredWithoutTheSetting()
    {
        _tmdb.With(FakeTmdbClient.Movie(1) with { IsAdult = true });

        Assert.Null(await _media.GetAsync(MediaType.Movie, 1));
        await using (var context = _db.CreateDbContext())
        {
            Assert.Empty(context.Media);
        }

        Assert.NotNull(await _media.GetAsync(MediaType.Movie, 1, includeAdult: true));
    }

    [Fact]
    public async Task Get_ANonPositiveId_IsNotSentToTmdb()
    {
        Assert.Null(await _media.GetAsync(MediaType.Movie, 0));
        Assert.Null(await _media.GetAsync(MediaType.TVShow, -1));
        Assert.Equal(0, _tmdb.GetCalls);
    }

    [Fact]
    public async Task Get_TheSameIdAsAnAnimeAndAMovie_KeepsTwoTitles()
    {
        _anime.With(FakeAnimeProvider.Anime(7, "The anime"));
        _tmdb.With(FakeTmdbClient.Movie(7, "The movie"));

        Assert.Equal("The anime", (await _media.GetAsync(MediaType.Anime, 7))!.Title);
        Assert.Equal("The movie", (await _media.GetAsync(MediaType.Movie, 7))!.Title);
        Assert.Null(await _media.GetAsync(MediaType.TVShow, 7));
    }

    [Fact]
    public async Task Genres_OfAnimeAndTmdb_DoNotMixUpWhenTheirIdsCollide()
    {
        // Genre 14 is Horror for MyAnimeList and Fantasy for TMDB.
        _anime.With(FakeAnimeProvider.Anime(1, "Scary anime") with { Genres = [new ExternalGenreDto(14, "Horror")] });
        _tmdb.With(FakeTmdbClient.Movie(1, "Magic movie", 100, (14, "Fantasy")));

        var anime = await _media.GetAsync(MediaType.Anime, 1);
        var movie = await _media.GetAsync(MediaType.Movie, 1);

        Assert.Equal(["Horror"], anime!.Genres);
        Assert.Equal(["Fantasy"], movie!.Genres);
        await using var context = _db.CreateDbContext();
        Assert.Equal(
            [(GenreSource.MyAnimeList, "Horror"), (GenreSource.Tmdb, "Fantasy")],
            context.Genres.AsEnumerable().Select(g => (g.Source, g.Name)).OrderBy(g => g.Source));
    }

    [Fact]
    public async Task Genres_SharedByTwoTmdbTitles_AreStoredOnce()
    {
        _tmdb.With(
            FakeTmdbClient.Movie(1, "One", 100, (35, "Comedy")),
            FakeTmdbClient.Series(2, "Two", 30, (35, "Comedy")));

        await _media.GetAsync(MediaType.Movie, 1);
        await _media.GetAsync(MediaType.TVShow, 2);

        await using var context = _db.CreateDbContext();
        Assert.Single(context.Genres);
    }

    [Fact]
    public async Task StreamingLinks_OfAPlatformThatLeft_AreRemovedAndTheOthersKept()
    {
        _tmdb.With(FakeTmdbClient.Movie(1) with
        {
            StreamingLinks = [new StreamingLinkDto("Netflix", "https://tmdb.test/w"), new StreamingLinkDto("Canal+", "https://tmdb.test/w")]
        });
        await _media.GetAsync(MediaType.Movie, 1);

        _tmdb.With(FakeTmdbClient.Movie(1) with { StreamingLinks = [new StreamingLinkDto("Netflix", "https://tmdb.test/w")] });
        _time.Advance(TimeSpan.FromDays(8));
        var movie = await _media.GetAsync(MediaType.Movie, 1);

        Assert.Equal(["Netflix"], movie!.StreamingLinks.Select(l => l.Site));
    }

    [Fact]
    public async Task WatchList_HoldsMoviesAndSeriesNextToAnime()
    {
        _anime.With(FakeAnimeProvider.Anime(7, "The anime"));
        _tmdb.With(FakeTmdbClient.Movie(7, "The movie"), FakeTmdbClient.Series(7, "The series"));

        Assert.Equal(WatchListAddResult.Added, await _watchList.AddAsync(User, MediaType.Anime, 7));
        Assert.Equal(WatchListAddResult.Added, await _watchList.AddAsync(User, MediaType.Movie, 7));
        Assert.Equal(WatchListAddResult.Added, await _watchList.AddAsync(User, MediaType.TVShow, 7));
        Assert.Equal(WatchListAddResult.AlreadyInList, await _watchList.AddAsync(User, MediaType.Movie, 7));

        var titles = (await _watchList.GetAsync(User)).Select(i => i.Title).Order();
        Assert.Equal(["The anime", "The movie", "The series"], titles);
    }

    [Fact]
    public async Task WatchList_AddingAnUnknownMovie_ReportsNotFound()
    {
        Assert.Equal(WatchListAddResult.NotFound, await _watchList.AddAsync(User, MediaType.Movie, 999));
    }

    [Fact]
    public async Task WatchList_AddingAMovieWhileTmdbIsDown_Throws()
    {
        _tmdb.Failure = new TmdbApiException("down");

        await Assert.ThrowsAsync<MediaProviderUnavailableException>(() => _watchList.AddAsync(User, MediaType.Movie, 1));
    }

    [Fact]
    public async Task WatchList_FiltersByDurationForMovies()
    {
        _tmdb.With(FakeTmdbClient.Movie(1, "Long film", 150));
        await _watchList.AddAsync(User, MediaType.Movie, 1);

        var longOnes = await _watchList.GetAsync(User, new WatchListFilterDto([], DurationRange.Long) with { Type = MediaType.Movie });

        Assert.Single(longOnes);
    }
}
