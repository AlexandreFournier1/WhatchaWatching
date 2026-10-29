using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Services;

/// <summary>
/// A Blazor circuit stays open for hours and reads the sign-in only once, when it starts. This checks it again every
/// few minutes, so that a changed password or a deleted account ends the sessions that are already open.
/// </summary>
public sealed class IdentityRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityOptions> options) : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(10);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        // The circuit has its own scope, which stays open: a short one gives a context that is not shared with the pages.
        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();

        return await SecurityStampCheck.IsCurrentAsync(
            userManager, authenticationState.User, options.Value.ClaimsIdentity.SecurityStampClaimType);
    }
}
