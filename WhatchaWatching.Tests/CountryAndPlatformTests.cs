using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Middleware.Tmdb;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

public class CountryAndPlatformTests
{
    private readonly TestDbContextFactory _db = new();
    private readonly FakeTmdbClient _tmdb = new();
    private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly UserSettingsService _settings;
    private readonly MediaService _media;
    private readonly WatchListService _watchList;
    private readonly RandomPickerService _picker;

    public CountryAndPlatformTests()
    {
        _settings = new UserSettingsService(_db);
        _media = new MediaService(new FakeAnimeProvider(), _tmdb, _db, _time, NullLogger<MediaService>.Instance);
        _watchList = new WatchListService(_db, _media);
        _picker = new RandomPickerService(_db, new Random(1));
    }

    private async Task<int> CreateUserAsync(string? country = null)
    {
        await using var context = _db.CreateDbContext();
        var user = new UserEntity { UserName = "alex" + Guid.NewGuid().ToString("N")[..6], Country = country };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }

    private static StreamingLinkDto Link(string region, string site) =>
        new(site, $"https://tmdb.test/{region}") { Region = region };

    private async Task ListAsync(int userId, int id, string title, params StreamingLinkDto[] links)
    {
        _tmdb.With(FakeTmdbClient.Movie(id, title) with { StreamingLinks = links });
        Assert.Equal(WatchListAddResult.Added, await _watchList.AddAsync(userId, MediaType.Movie, id));
    }

    [Theory]
    [InlineData("fr", "FR")]
    [InlineData(" be ", "BE")]
    [InlineData("US", "US")]
    [InlineData("XX", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Normalize_KeepsOnlyTheCountriesOfTheCatalog(string? code, string? expected)
    {
        Assert.Equal(expected, CountryCatalog.Normalize(code));
    }

    [Fact]
    public void TheDefaultCountry_IsInTheCatalog_AndHasAName()
    {
        Assert.Contains(CountryCatalog.DEFAULT_CODE, CountryCatalog.Codes);
        Assert.Equal("France", CountryCatalog.NameOf("FR"));
        Assert.Equal("ZZ", CountryCatalog.NameOf("ZZ"));
    }

    [Fact]
    public async Task Country_IsTheDefaultOneUntilTheUserChoosesAnother()
    {
        var user = await CreateUserAsync();

        Assert.Equal("FR", await _settings.GetCountryAsync(user));
        Assert.Equal("FR", await _settings.GetCountryAsync(999));

        await _settings.SetCountryAsync(user, "ca");

        Assert.Equal("CA", await _settings.GetCountryAsync(user));
    }

    [Fact]
    public async Task Country_CanGoBackToTheDefault()
    {
        var user = await CreateUserAsync("US");

        await _settings.SetCountryAsync(user, null);

        Assert.Equal("FR", await _settings.GetCountryAsync(user));
        await using var context = _db.CreateDbContext();
        Assert.Null((await context.Users.SingleAsync(u => u.Id == user)).Country);
    }

    [Fact]
    public async Task Country_OutsideTheCatalog_IsRefused()
    {
        var user = await CreateUserAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => _settings.SetCountryAsync(user, "XX"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _settings.SetCountryAsync(999, "BE"));
    }

    [Fact]
    public async Task Links_AreStoredWithTheirCountry_AndTheOnesThatLeftAreRemoved()
    {
        _tmdb.With(FakeTmdbClient.Movie(1) with { StreamingLinks = [Link("FR", "Netflix"), Link("US", "Hulu")] });
        await _media.GetAsync(MediaType.Movie, 1);

        _tmdb.With(FakeTmdbClient.Movie(1) with { StreamingLinks = [Link("FR", "Netflix"), Link("FR", "Canal+")] });
        _time.Advance(TimeSpan.FromDays(8));
        var movie = await _media.GetAsync(MediaType.Movie, 1);

        Assert.Equal([("FR", "Canal+"), ("FR", "Netflix")], movie!.StreamingLinks.Select(l => (l.Region!, l.Site)));
    }

    [Fact]
    public async Task ThePlatformsOfAList_AreThoseOfTheCountryOfTheUser()
    {
        var french = await CreateUserAsync();
        var american = await CreateUserAsync("US");
        await ListAsync(french, 1, "Film", Link("FR", "Netflix"), Link("US", "Hulu"));
        await ListAsync(american, 1, "Film");

        Assert.Equal(["Netflix"], await _watchList.GetPlatformsAsync(french));
        Assert.Equal(["Hulu"], await _watchList.GetPlatformsAsync(american));
    }

    [Fact]
    public async Task ThePlatformsOfAList_CanBeLimitedToATypeAndCountTheAnimeOfEveryCountry()
    {
        var user = await CreateUserAsync();
        await ListAsync(user, 1, "Film", Link("FR", "Netflix"));

        await using (var context = _db.CreateDbContext())
        {
            var anime = new MediaEntity { Type = MediaType.Anime, ExternalId = 5, Title = "Anime", Status = MediaStatus.Finished };
            anime.StreamingLinks.Add(new StreamingLinkEntity { Site = "Crunchyroll", Url = "https://cr.test/5" });
            anime.WatchListItems.Add(new WatchListItemEntity { UserId = user });
            context.Media.Add(anime);
            await context.SaveChangesAsync();
        }

        Assert.Equal(["Crunchyroll"], await _watchList.GetPlatformsAsync(user, MediaType.Anime));
        Assert.Equal(["Netflix"], await _watchList.GetPlatformsAsync(user, MediaType.Movie));
        Assert.Equal(["Crunchyroll", "Netflix"], await _watchList.GetPlatformsAsync(user));
    }

    [Fact]
    public async Task PlatformFilter_KeepsTheTitlesOfferedInTheCountryOfTheUser()
    {
        var user = await CreateUserAsync("US");
        await ListAsync(user, 1, "OnHulu", Link("US", "Hulu"), Link("FR", "Netflix"));
        await ListAsync(user, 2, "OnNetflixInFranceOnly", Link("FR", "Netflix"));
        await ListAsync(user, 3, "Nowhere");

        var filter = WatchListFilterDto.None with { Platforms = ["Hulu", "Netflix"] };

        var found = (await _watchList.GetAsync(user, filter)).Select(m => m.Title);

        // Netflix counts for the French offer only: for someone in the United States it is Hulu alone.
        Assert.Equal(["OnHulu"], found);
    }

    [Fact]
    public async Task PlatformFilter_ForSeveralPlatforms_KeepsAnyOfThem()
    {
        var user = await CreateUserAsync();
        await ListAsync(user, 1, "A", Link("FR", "Netflix"));
        await ListAsync(user, 2, "B", Link("FR", "Disney+"));
        await ListAsync(user, 3, "C", Link("FR", "Canal+"));

        var filter = WatchListFilterDto.None with { Platforms = ["Netflix", "Canal+"] };

        Assert.Equal(["A", "C"], (await _watchList.GetAsync(user, filter, WatchListSort.Title)).Select(m => m.Title));
    }

    [Fact]
    public async Task ThePick_FollowsThePlatformFilter()
    {
        var user = await CreateUserAsync();
        await ListAsync(user, 1, "OnNetflix", Link("FR", "Netflix"));
        await ListAsync(user, 2, "Elsewhere", Link("FR", "Disney+"));

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var pick = await _picker.PickAsync(user, WatchListFilterDto.None with { Platforms = ["Netflix"] });

            Assert.Equal("OnNetflix", pick!.Title);
        }
    }

    [Fact]
    public async Task WithoutAPlatformCondition_TheCountryIsNotLookedUp()
    {
        var user = await CreateUserAsync("US");
        await ListAsync(user, 1, "Anything", Link("FR", "Netflix"));

        Assert.Single(await _watchList.GetAsync(user, WatchListFilterDto.None));
    }
}
