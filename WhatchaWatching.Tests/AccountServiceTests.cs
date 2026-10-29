using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

public class AccountServiceTests
{
    private readonly FakeAnimeProvider _provider = new();
    private readonly TestDbContextFactory _db = new();
    private readonly AccountService _accounts;
    private readonly CustomAnimeService _custom;
    private readonly WatchListService _watchList;

    public AccountServiceTests()
    {
        _accounts = new AccountService(_db);
        _custom = new CustomAnimeService(_db);
        var media = new MediaService(_provider, new FakeTmdbClient(), _db, TimeProvider.System, NullLogger<MediaService>.Instance);
        _watchList = new WatchListService(_db, media);
    }

    private async Task<int> CreateUserAsync(string name)
    {
        await using var context = _db.CreateDbContext();
        var user = new UserEntity { UserName = name, Email = name + "@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }

    [Fact]
    public async Task Delete_RemovesTheAccountItsListAndItsOwnAnime()
    {
        var alex = await CreateUserAsync("alex");
        _provider.With(FakeAnimeProvider.Anime(1, "Shared"));
        await _watchList.AddAsync(alex, MediaType.Anime, 1);
        var mine = await _custom.CreateAsync(alex, new CustomAnimeDto { Title = "Mine", Genres = ["Action"] });

        Assert.True(await _accounts.DeleteAsync(alex));

        await using var context = _db.CreateDbContext();
        Assert.Empty(context.Users);
        Assert.Empty(context.WatchListItems);
        Assert.DoesNotContain(context.Media, m => m.ExternalId == mine.Id);
    }

    [Fact]
    public async Task Delete_KeepsTheSharedCatalogAndTheOtherAccounts()
    {
        var alex = await CreateUserAsync("alex");
        var sam = await CreateUserAsync("sam");
        _provider.With(FakeAnimeProvider.Anime(1, "Shared"));
        await _watchList.AddAsync(alex, MediaType.Anime, 1);
        await _watchList.AddAsync(sam, MediaType.Anime, 1);
        await _custom.CreateAsync(sam, new CustomAnimeDto { Title = "Sam's own" });

        await _accounts.DeleteAsync(alex);

        await using var context = _db.CreateDbContext();
        Assert.Equal(["sam"], context.Users.Select(u => u.UserName));
        Assert.Contains(context.Media, m => m.Title == "Shared");
        Assert.Equal(2, await context.WatchListItems.CountAsync());
        Assert.Contains(context.Media, m => m.Title == "Sam's own");
        Assert.All(context.WatchListItems, i => Assert.Equal(sam, i.UserId));
    }

    [Fact]
    public async Task Delete_OfAnUnknownAccount_ReturnsFalse()
    {
        Assert.False(await _accounts.DeleteAsync(999));
    }

    [Fact]
    public async Task Delete_TwiceDeletesOnlyOnce()
    {
        var alex = await CreateUserAsync("alex");

        Assert.True(await _accounts.DeleteAsync(alex));
        Assert.False(await _accounts.DeleteAsync(alex));
    }
}
