using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Exceptions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Middleware;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

public class CustomAnimeServiceTests
{
    private const int User = 1;
    private const int OtherUser = 2;

    private readonly FakeAnimeProvider _provider = new();
    private readonly TestDbContextFactory _db = new();
    private readonly CustomAnimeService _custom;
    private readonly AnimeService _anime;
    private readonly WatchListService _watchList;

    public CustomAnimeServiceTests()
    {
        _custom = new CustomAnimeService(_db);
        _anime = new AnimeService(_provider, _db, TimeProvider.System, NullLogger<AnimeService>.Instance);
        _watchList = new WatchListService(_db, _anime);
    }

    private static CustomAnimeDto Input(string title = "My anime") => new() { Title = title };

    [Fact]
    public async Task Create_PutsTheAnimeInTheListOfItsOwnerWithANegativeId()
    {
        var created = await _custom.CreateAsync(User, Input("Lost show"));

        Assert.True(created.Id < 0);
        Assert.True(created.IsCustom);
        var listed = Assert.Single(await _watchList.GetAsync(User));
        Assert.Equal(created.Id, listed.Id);
        Assert.Equal("Lost show", listed.Title);
        Assert.True(listed.IsCustom);
    }

    [Fact]
    public async Task Create_GivesEachAnimeItsOwnIdCountingDown()
    {
        var first = await _custom.CreateAsync(User, Input("One"));
        var second = await _custom.CreateAsync(User, Input("Two"));
        var other = await _custom.CreateAsync(OtherUser, Input("Three"));

        Assert.Equal([-1, -2, -3], new[] { first.Id, second.Id, other.Id });
    }

    [Fact]
    public async Task Create_KeepsWhatWasTypedAndTurnsBlankIntoNothing()
    {
        var input = new CustomAnimeDto
        {
            Title = "  Padded title  ",
            OriginalTitle = "   ",
            Synopsis = " A summary. ",
            ImageUrl = " https://img.test/a.jpg ",
            EpisodeCount = 12,
            EpisodeDurationMinutes = 24,
            Year = 2019,
            Status = MediaStatus.Airing
        };

        var created = await _custom.CreateAsync(User, input);

        Assert.Equal("Padded title", created.Title);
        Assert.Equal("A summary.", created.Synopsis);
        Assert.Equal("https://img.test/a.jpg", created.ImageUrl);
        Assert.Equal(12, created.Episodes);
        Assert.Equal(24, created.EpisodeDurationMinutes);
        Assert.Equal(2019, created.Year);
        Assert.Equal("Airing", created.Status);
        await using var context = _db.CreateDbContext();
        Assert.Null((await context.Media.SingleAsync()).OriginalTitle);
    }

    [Fact]
    public async Task Create_LinksTheGenresAndReusesTheOnesAlreadyStored()
    {
        _provider.With(FakeAnimeProvider.Anime(7, "From a provider", 24, "Comedy"));
        await _anime.GetAsync(7);

        var created = await _custom.CreateAsync(User, new CustomAnimeDto { Title = "Mine", Genres = ["comedy", "Drama", "Drama"] });

        Assert.Equal(["Comedy", "Drama"], created.Genres);
        await using var context = _db.CreateDbContext();
        Assert.Equal(2, await context.Genres.CountAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithoutATitle_Throws(string title)
    {
        await Assert.ThrowsAsync<ValidationException>(() => _custom.CreateAsync(User, Input(title)));
    }

    [Fact]
    public async Task Create_WithValuesOutOfRange_Throws()
    {
        var badInputs = new[]
        {
            new CustomAnimeDto { Title = new string('t', ConfigurationConstants.LARGE_SIZE + 1) },
            new CustomAnimeDto { Title = "x", EpisodeCount = 0 },
            new CustomAnimeDto { Title = "x", EpisodeDurationMinutes = 601 },
            new CustomAnimeDto { Title = "x", Year = 1800 },
            new CustomAnimeDto { Title = "x", Synopsis = new string('s', ConfigurationConstants.XXXL_SIZE + 1) }
        };

        foreach (var bad in badInputs)
        {
            await Assert.ThrowsAsync<ValidationException>(() => _custom.CreateAsync(User, bad));
        }

        await using var context = _db.CreateDbContext();
        Assert.Equal(0, await context.Media.CountAsync());
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://files.test/a.jpg")]
    [InlineData("/relative/path.jpg")]
    [InlineData("not an address")]
    public async Task Create_WithAnImageThatIsNotAWebAddress_Throws(string imageUrl)
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _custom.CreateAsync(User, new CustomAnimeDto { Title = "x", ImageUrl = imageUrl }));
    }

    [Fact]
    public async Task Create_WithAnUnknownGenre_Throws()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _custom.CreateAsync(User, new CustomAnimeDto { Title = "x", Genres = ["Made up"] }));

        Assert.Contains("Made up", exception.Message);
    }

    [Fact]
    public async Task Create_WhenTheUserReachedTheLimit_ThrowsAndOtherUsersAreNotAffected()
    {
        await using (var context = _db.CreateDbContext())
        {
            for (var i = 1; i <= CustomAnimeConstants.MAX_PER_USER; i++)
            {
                context.Media.Add(new MediaEntity
                {
                    Type = MediaType.Anime, ExternalId = -i, Title = $"Own {i}", CreatedByUserId = User, Status = MediaStatus.Finished
                });
            }

            await context.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<ValidationException>(() => _custom.CreateAsync(User, Input()));
        var other = await _custom.CreateAsync(OtherUser, Input("Allowed"));
        Assert.Equal(-(CustomAnimeConstants.MAX_PER_USER + 1), other.Id);
    }

    [Fact]
    public async Task Get_OnlyAnswersToTheOwner()
    {
        var created = await _custom.CreateAsync(User, Input());

        Assert.Equal("My anime", (await _custom.GetAsync(User, created.Id))!.Title);
        Assert.Null(await _custom.GetAsync(OtherUser, created.Id));
        Assert.Null(await _custom.GetAsync(User, -999));
    }

    [Fact]
    public async Task TheSharedAnimeService_NeverReturnsAnAnimeAddedByHand()
    {
        var created = await _custom.CreateAsync(User, Input());

        Assert.Null(await _anime.GetAsync(created.Id));
        Assert.Equal(0, _provider.GetAnimeCalls);
    }

    [Fact]
    public async Task TheSharedAnimeService_DoesNotAskAProviderAboutNonProviderIds()
    {
        _provider.Failure = new ExternalApiException("down");

        Assert.Null(await _anime.GetAsync(-5));
        Assert.Null(await _anime.GetAsync(0));
        Assert.Equal(0, _provider.GetAnimeCalls);
    }

    [Fact]
    public async Task SearchingTheStoredAnime_NeverShowsAnimeAddedByHand()
    {
        await _custom.CreateAsync(OtherUser, Input("Secret fan edit"));
        _provider.Failure = new ExternalApiException("down");

        await Assert.ThrowsAsync<AnimeProviderUnavailableException>(() => _anime.SearchAsync("Secret", 1));
    }

    [Fact]
    public async Task AddingAnAnimeAddedByHand_ThroughTheSharedList_ReportsNotFound()
    {
        var created = await _custom.CreateAsync(OtherUser, Input());

        Assert.Equal(WatchListAddResult.AnimeNotFound, await _watchList.AddAsync(User, created.Id));
    }

    [Fact]
    public async Task TheListOfAnotherUser_DoesNotContainTheAnime()
    {
        await _custom.CreateAsync(User, Input());

        Assert.Empty(await _watchList.GetAsync(OtherUser));
    }

    [Fact]
    public async Task Remove_DeletesTheAnimeAddedByHandAndItsGenres()
    {
        var created = await _custom.CreateAsync(User, new CustomAnimeDto { Title = "Short lived", Genres = ["Action"] });

        Assert.True(await _watchList.RemoveAsync(User, created.Id));

        await using var context = _db.CreateDbContext();
        Assert.Equal(0, await context.Media.CountAsync());
        Assert.Equal(0, await context.MediaGenres.CountAsync());
        Assert.Equal(0, await context.WatchListItems.CountAsync());
        Assert.Null(await _custom.GetAsync(User, created.Id));
    }

    [Fact]
    public async Task Remove_KeepsASharedAnimeForTheOtherUsers()
    {
        _provider.With(FakeAnimeProvider.Anime(1, "Shared"));
        await _watchList.AddAsync(User, 1);
        await _watchList.AddAsync(OtherUser, 1);

        await _watchList.RemoveAsync(User, 1);

        await using var context = _db.CreateDbContext();
        Assert.Equal(1, await context.Media.CountAsync());
        Assert.Single(await _watchList.GetAsync(OtherUser));
    }

    [Fact]
    public async Task ThePicker_CanPickAnAnimeAddedByHand()
    {
        await _custom.CreateAsync(User, new CustomAnimeDto { Title = "Only mine", EpisodeDurationMinutes = 10 });
        var picker = new RandomPickerService(_db, new Random(1));

        var picked = await picker.PickAsync(User, new WatchListFilterDto([], DurationRange.Short));

        Assert.Equal("Only mine", picked!.Title);
        Assert.True(picked.IsCustom);
    }
}

public class GenreCatalogTests
{
    [Fact]
    public void Catalog_HasUniqueNamesAndIds()
    {
        Assert.Equal(GenreCatalog.All.Count, GenreCatalog.All.Select(g => g.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(GenreCatalog.All.Count, GenreCatalog.All.Select(g => g.ExternalId).Distinct().Count());
    }

    [Theory]
    [InlineData("action", "Action")]
    [InlineData("SLICE OF LIFE", "Slice of Life")]
    public void Find_IgnoresTheCase(string name, string expected)
    {
        Assert.Equal(expected, GenreCatalog.Find(name)!.Name);
    }

    [Fact]
    public void Find_ForAnUnknownGenre_ReturnsNull()
    {
        Assert.Null(GenreCatalog.Find("Made up"));
    }
}
