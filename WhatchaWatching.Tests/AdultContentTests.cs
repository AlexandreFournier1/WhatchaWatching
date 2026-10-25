using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Middleware;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

public class AdultContentTests
{
    private const int Adult = 1;
    private const int Safe = 2;

    private readonly FakeAnimeProvider _provider = new();
    private readonly TestDbContextFactory _db = new();
    private readonly AnimeService _anime;
    private readonly WatchListService _watchList;
    private readonly UserSettingsService _settings;

    public AdultContentTests()
    {
        _anime = new AnimeService(_provider, _db, TimeProvider.System, NullLogger<AnimeService>.Instance);
        _watchList = new WatchListService(_db, _anime);
        _settings = new UserSettingsService(_db);

        using var context = _db.CreateDbContext();
        context.Users.Add(new UserEntity { Id = Adult, UserName = "adult", ShowAdultContent = true });
        context.Users.Add(new UserEntity { Id = Safe, UserName = "safe" });
        context.SaveChanges();
    }

    private static ExternalAnimeDto AdultAnime(int id, string title = "Adult show") =>
        FakeAnimeProvider.Anime(id, title) with { IsAdult = true };

    [Fact]
    public async Task Settings_AreOffByDefaultAndForAnUnknownUser()
    {
        Assert.False(await _settings.ShowsAdultContentAsync(Safe));
        Assert.False(await _settings.ShowsAdultContentAsync(999));
    }

    [Fact]
    public async Task Settings_CanBeTurnedOnAndOff()
    {
        await _settings.SetShowsAdultContentAsync(Safe, true);
        Assert.True(await _settings.ShowsAdultContentAsync(Safe));

        await _settings.SetShowsAdultContentAsync(Safe, false);
        Assert.False(await _settings.ShowsAdultContentAsync(Safe));
        Assert.True(await _settings.ShowsAdultContentAsync(Adult));
    }

    [Fact]
    public async Task Settings_ForAnUnknownUser_Throw()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _settings.SetShowsAdultContentAsync(999, true));
    }

    [Fact]
    public async Task Search_AsksTheProviderForSafeAnimeUnlessToldOtherwise()
    {
        _provider.With(FakeAnimeProvider.Anime(1, "Plain show"), AdultAnime(2, "Plain adult"));

        var safe = await _anime.SearchAsync("Plain", 1);
        Assert.False(_provider.LastIncludeAdult);
        Assert.Equal(["Plain show"], safe.Results.Items.Select(a => a.Title));

        var all = await _anime.SearchAsync("Plain", 1, includeAdult: true);
        Assert.True(_provider.LastIncludeAdult);
        Assert.Equal(2, all.Results.Items.Count);
        Assert.Contains(all.Results.Items, a => a.IsAdult);
    }

    [Fact]
    public async Task Search_KeepsAnAdultAnimeOutEvenIfTheProviderReturnsOne()
    {
        var careless = new StubAniListClient { Anime = AdultAnime(1) };
        var service = new AnimeService(careless, _db, TimeProvider.System, NullLogger<AnimeService>.Instance);

        var result = await service.SearchAsync("x", 1);

        Assert.Empty(result.Results.Items);
    }

    [Fact]
    public async Task Get_AnAdultAnime_IsNotFoundAndNotStoredForSomeoneWhoDidNotAllowIt()
    {
        _provider.With(AdultAnime(1));

        Assert.Null(await _anime.GetAsync(1));

        await using var context = _db.CreateDbContext();
        Assert.Equal(0, await context.Media.CountAsync());
    }

    [Fact]
    public async Task Get_AnAdultAnime_IsStoredAndFlaggedForSomeoneWhoAllowedIt()
    {
        _provider.With(AdultAnime(1));

        var anime = await _anime.GetAsync(1, includeAdult: true);

        Assert.True(anime!.IsAdult);
        await using var context = _db.CreateDbContext();
        Assert.True((await context.Media.SingleAsync()).IsAdult);
    }

    [Fact]
    public async Task Get_AStoredAdultAnime_IsHiddenAgainWhenTheSettingIsOff()
    {
        _provider.With(AdultAnime(1));
        await _anime.GetAsync(1, includeAdult: true);

        Assert.Null(await _anime.GetAsync(1));
        Assert.NotNull(await _anime.GetAsync(1, includeAdult: true));
    }

    [Fact]
    public async Task SearchingTheStoredAnime_HidesAdultOnesUnlessAllowed()
    {
        _provider.With(AdultAnime(1, "Stored adult"), FakeAnimeProvider.Anime(2, "Stored plain"));
        await _anime.GetAsync(1, includeAdult: true);
        await _anime.GetAsync(2);
        _provider.Failure = new ExternalApiException("down");

        var safe = await _anime.SearchAsync("Stored", 1);
        var all = await _anime.SearchAsync("Stored", 1, includeAdult: true);

        Assert.Equal(["Stored plain"], safe.Results.Items.Select(a => a.Title));
        Assert.Equal(2, all.Results.Items.Count);
    }

    [Fact]
    public async Task Add_AnAdultAnime_OnlyWorksForAUserWhoAllowedIt()
    {
        _provider.With(AdultAnime(1));

        Assert.Equal(WatchListAddResult.AnimeNotFound, await _watchList.AddAsync(Safe, 1));
        Assert.Equal(WatchListAddResult.Added, await _watchList.AddAsync(Adult, 1));
    }

    [Fact]
    public async Task TheList_HidesAdultAnimeFromAUserWhoTurnedTheSettingOff_AndShowsThemAgainAfterwards()
    {
        _provider.With(AdultAnime(1, "Adult one"), FakeAnimeProvider.Anime(2, "Plain one", 24, "Comedy"));
        await _watchList.AddAsync(Adult, 1);
        await _watchList.AddAsync(Adult, 2);

        Assert.Equal(2, (await _watchList.GetAsync(Adult)).Count);

        await _settings.SetShowsAdultContentAsync(Adult, false);
        Assert.Equal(["Plain one"], (await _watchList.GetAsync(Adult)).Select(a => a.Title));

        await _settings.SetShowsAdultContentAsync(Adult, true);
        Assert.Equal(2, (await _watchList.GetAsync(Adult)).Count);
    }

    [Fact]
    public async Task TheGenresOfTheList_LeaveOutTheOnesOfHiddenAnime()
    {
        _provider.With(
            AdultAnime(1) with { Genres = [new ExternalGenreDto(12, "Hentai")] },
            FakeAnimeProvider.Anime(2, "Plain one", 24, "Comedy"));
        await _watchList.AddAsync(Adult, 1);
        await _watchList.AddAsync(Adult, 2);

        Assert.Equal(["Comedy", "Hentai"], await _watchList.GetGenresAsync(Adult));

        await _settings.SetShowsAdultContentAsync(Adult, false);
        Assert.Equal(["Comedy"], await _watchList.GetGenresAsync(Adult));
    }

    [Fact]
    public async Task ThePicker_NeverPicksAHiddenAdultAnime()
    {
        _provider.With(AdultAnime(1, "Adult one"), FakeAnimeProvider.Anime(2, "Plain one"));
        await _watchList.AddAsync(Adult, 1);
        await _watchList.AddAsync(Adult, 2);
        await _settings.SetShowsAdultContentAsync(Adult, false);
        var picker = new RandomPickerService(_db, new Random(3));

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal("Plain one", (await picker.PickAsync(Adult))!.Title);
        }
    }

    [Fact]
    public async Task ThePicker_CanPickAdultAnimeWhenTheSettingIsOn()
    {
        _provider.With(AdultAnime(1, "Adult one"));
        await _watchList.AddAsync(Adult, 1);
        var picker = new RandomPickerService(_db, new Random(3));

        Assert.Equal("Adult one", (await picker.PickAsync(Adult))!.Title);
    }

    [Fact]
    public async Task TheSwitchOff_HidesAdultAnimeFromTheListAndThePickEvenWhenTheProfileAllowsThem()
    {
        _provider.With(AdultAnime(1, "Adult one"), FakeAnimeProvider.Anime(2, "Plain one"));
        await _watchList.AddAsync(Adult, 1);
        await _watchList.AddAsync(Adult, 2);
        var switchedOff = WatchListFilterDto.None with { IncludeAdult = false };
        var picker = new RandomPickerService(_db, new Random(5));

        Assert.Equal(["Plain one"], (await _watchList.GetAsync(Adult, switchedOff)).Select(a => a.Title));
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal("Plain one", (await picker.PickAsync(Adult, switchedOff))!.Title);
        }

        // The switch on (the default) shows what the profile allows.
        Assert.Equal(2, (await _watchList.GetAsync(Adult, WatchListFilterDto.None)).Count);
    }

    [Fact]
    public async Task TheSwitchOn_NeverShowsMoreThanTheProfileAllows()
    {
        _provider.With(AdultAnime(1, "Adult one"), FakeAnimeProvider.Anime(2, "Plain one"));
        await _watchList.AddAsync(Adult, 1);
        await _watchList.AddAsync(Adult, 2);
        await _settings.SetShowsAdultContentAsync(Adult, false);
        var switchedOn = WatchListFilterDto.None with { IncludeAdult = true };
        var picker = new RandomPickerService(_db, new Random(5));

        Assert.Equal(["Plain one"], (await _watchList.GetAsync(Adult, switchedOn)).Select(a => a.Title));
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal("Plain one", (await picker.PickAsync(Adult, switchedOn))!.Title);
        }
    }

    [Fact]
    public void TheFilter_IncludesAdultAnimeByDefault()
    {
        Assert.True(WatchListFilterDto.None.IncludeAdult);
        Assert.True(new WatchListFilterDto(["Comedy"], null).IncludeAdult);
    }
}
