using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WhatchaWatching.Infrastructure.Data;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Services;

namespace WhatchaWatching.Tests;

public class SecurityStampCheckTests
{
    private static readonly string StampClaim = new IdentityOptions().ClaimsIdentity.SecurityStampClaimType;

    private static ServiceProvider Services() => new ServiceCollection()
        .AddLogging()
        .AddDataProtection().Services
        .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()))
        .AddIdentityCore<UserEntity>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders()
        .Services.BuildServiceProvider();

    private static async Task<(UserManager<UserEntity> Manager, UserEntity User, ClaimsPrincipal Principal)> SignedInAsync(
        ServiceProvider provider)
    {
        var manager = provider.GetRequiredService<UserManager<UserEntity>>();
        var user = new UserEntity { UserName = "alex", Email = "alex@example.test" };
        Assert.True((await manager.CreateAsync(user, "Password1!")).Succeeded);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(StampClaim, await manager.GetSecurityStampAsync(user))
            ],
            "test"));

        return (manager, user, principal);
    }

    [Fact]
    public async Task ASessionOfTheCurrentAccount_IsValid()
    {
        using var provider = Services();
        var (manager, _, principal) = await SignedInAsync(provider);

        Assert.True(await SecurityStampCheck.IsCurrentAsync(manager, principal, StampClaim));
    }

    [Fact]
    public async Task ASessionStartedBeforeAPasswordChange_IsNoLongerValid()
    {
        using var provider = Services();
        var (manager, user, principal) = await SignedInAsync(provider);

        Assert.True((await manager.ChangePasswordAsync(user, "Password1!", "Password2!")).Succeeded);

        Assert.False(await SecurityStampCheck.IsCurrentAsync(manager, principal, StampClaim));
    }

    [Fact]
    public async Task ASessionOfADeletedAccount_IsNoLongerValid()
    {
        using var provider = Services();
        var (manager, user, principal) = await SignedInAsync(provider);

        Assert.True((await manager.DeleteAsync(user)).Succeeded);

        Assert.False(await SecurityStampCheck.IsCurrentAsync(manager, principal, StampClaim));
    }

    [Fact]
    public async Task ASessionWithoutAStamp_IsNotValid()
    {
        using var provider = Services();
        var (manager, user, _) = await SignedInAsync(provider);
        var withoutStamp = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "test"));

        Assert.False(await SecurityStampCheck.IsCurrentAsync(manager, withoutStamp, StampClaim));
    }

    [Fact]
    public async Task AnAnonymousVisitor_IsNotValid()
    {
        using var provider = Services();
        var (manager, _, _) = await SignedInAsync(provider);

        Assert.False(await SecurityStampCheck.IsCurrentAsync(manager, new ClaimsPrincipal(new ClaimsIdentity()), StampClaim));
    }
}
