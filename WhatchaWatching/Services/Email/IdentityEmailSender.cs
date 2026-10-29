using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Services.Email;

/// <summary>The messages of the account: Identity asks for them, the transport delivers them.</summary>
public sealed class IdentityEmailSender(IEmailTransport transport) : IEmailSender<UserEntity>
{
    public Task SendConfirmationLinkAsync(UserEntity user, string email, string confirmationLink) => transport.SendAsync(Compose(
        email,
        "Confirm your email address",
        user,
        "Open this link to confirm your email address for WhatchaWatching:",
        confirmationLink,
        "If you did not create an account, you can ignore this message."));

    public Task SendPasswordResetLinkAsync(UserEntity user, string email, string resetLink) => transport.SendAsync(Compose(
        email,
        "Reset your password",
        user,
        "Open this link to choose a new password for WhatchaWatching. It works for two hours.",
        resetLink,
        "If you did not ask for it, you can ignore this message: your password has not changed."));

    public Task SendPasswordResetCodeAsync(UserEntity user, string email, string resetCode) => transport.SendAsync(new EmailMessage(
        email,
        "Your reset code",
        $"Hello {user.UserName},\n\nYour code to reset your password: {resetCode}",
        $"<p>Hello {HtmlEncoder.Default.Encode(user.UserName ?? string.Empty)},</p><p>Your code to reset your password: <b>{HtmlEncoder.Default.Encode(resetCode)}</b></p>"));

    private static EmailMessage Compose(string to, string subject, UserEntity user, string intro, string link, string outro)
    {
        var name = user.UserName ?? string.Empty;
        var text = $"Hello {name},\n\n{intro}\n{link}\n\n{outro}\n\nWhatchaWatching";
        var html = $"<p>Hello {HtmlEncoder.Default.Encode(name)},</p>" +
                   $"<p>{HtmlEncoder.Default.Encode(intro)}</p>" +
                   $"<p><a href=\"{HtmlEncoder.Default.Encode(link)}\">{HtmlEncoder.Default.Encode(link)}</a></p>" +
                   $"<p>{HtmlEncoder.Default.Encode(outro)}</p><p>WhatchaWatching</p>";

        return new EmailMessage(to, subject, text, html);
    }
}
