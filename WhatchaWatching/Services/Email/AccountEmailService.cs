using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Services.Email;

public sealed class AccountEmailService(
    UserManager<UserEntity> userManager,
    IEmailSender<UserEntity> sender,
    NavigationManager navigation,
    ILogger<AccountEmailService> logger) : IAccountEmailService
{
    public async Task<bool> SendConfirmationAsync(UserEntity user)
    {
        if (string.IsNullOrEmpty(user.Email))
        {
            return false;
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = Link("confirm-email", new Dictionary<string, string?>
        {
            ["userId"] = user.Id.ToString(),
            ["code"] = TokenCodec.Encode(token)
        });

        return await TrySendAsync(() => sender.SendConfirmationLinkAsync(user, user.Email, link), "confirmation");
    }

    public async Task<bool> SendPasswordResetAsync(UserEntity user)
    {
        if (string.IsNullOrEmpty(user.Email))
        {
            return false;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = Link("reset-password", new Dictionary<string, string?>
        {
            ["email"] = user.Email,
            ["code"] = TokenCodec.Encode(token)
        });

        return await TrySendAsync(() => sender.SendPasswordResetLinkAsync(user, user.Email, link), "password reset");
    }

    // The address of the site as the visitor reached it (https behind the proxy, thanks to the forwarded headers).
    private string Link(string page, Dictionary<string, string?> query) =>
        navigation.ToAbsoluteUri(QueryHelpers.AddQueryString(page, query)).ToString();

    private async Task<bool> TrySendAsync(Func<Task> send, string kind)
    {
        try
        {
            await send();
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The link is a secret: it is never written to the logs.
            logger.LogWarning(ex, "The {Kind} email could not be sent.", kind);
            return false;
        }
    }
}
