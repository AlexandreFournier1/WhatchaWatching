using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using WhatchaWatching.Services;

namespace WhatchaWatching.Tests;

public class CurrentUserTests
{
    private sealed class StubAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }

    private static CurrentUser For(ClaimsPrincipal user) => new(new StubAuthenticationStateProvider(user));

    [Fact]
    public async Task GetUserId_ForASignedInUser_ReturnsTheIdentityId()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "42")], authenticationType: "test");

        Assert.Equal(42, await For(new ClaimsPrincipal(identity)).GetUserIdAsync());
    }

    [Fact]
    public async Task GetUserId_ForAnAnonymousVisitor_ReturnsNull()
    {
        Assert.Null(await For(new ClaimsPrincipal(new ClaimsIdentity())).GetUserIdAsync());
    }

    [Fact]
    public async Task GetUserId_WhenTheClaimIsNotANumber_ReturnsNull()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "abc")], authenticationType: "test");

        Assert.Null(await For(new ClaimsPrincipal(identity)).GetUserIdAsync());
    }
}
