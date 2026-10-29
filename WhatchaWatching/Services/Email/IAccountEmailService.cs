using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Services.Email;

public interface IAccountEmailService
{
    /// <summary>Sends the link that confirms the email address of the account.</summary>
    /// <returns>False when the message could not be sent (no mailbox configured, or the mail server refused it).</returns>
    Task<bool> SendConfirmationAsync(UserEntity user);

    /// <summary>Sends the link that lets the owner of the address choose a new password.</summary>
    /// <returns>False when the message could not be sent.</returns>
    Task<bool> SendPasswordResetAsync(UserEntity user);
}
