using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Services;

public static class SecurityStampCheck
{
    /// <summary>
    /// Whether the account still is the one the sign-in was made for. The stamp changes when the password does, so a
    /// session started before that, or for an account that has been deleted, is no longer valid.
    /// </summary>
    public static async Task<bool> IsCurrentAsync(
        UserManager<UserEntity> userManager, ClaimsPrincipal principal, string stampClaimType)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return false;
        }

        if (!userManager.SupportsUserSecurityStamp)
        {
            return true;
        }

        var signedWith = principal.FindFirstValue(stampClaimType);

        return signedWith is not null && signedWith == await userManager.GetSecurityStampAsync(user);
    }
}
