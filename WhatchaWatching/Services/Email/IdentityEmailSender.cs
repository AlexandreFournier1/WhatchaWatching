using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Services.Localization;

namespace WhatchaWatching.Services.Email;

/// <summary>The messages of the account: Identity asks for them, the transport delivers them.</summary>
public sealed class IdentityEmailSender(IEmailTransport transport, ILocalizer localizer) : IEmailSender<UserEntity>
{
    public Task SendConfirmationLinkAsync(UserEntity user, string email, string confirmationLink) => transport.SendAsync(Compose(
        email,
        localizer["Confirm your email address"],
        user,
        localizer["Open this link to confirm your email address for WhatchaWatching:"],
        confirmationLink,
        localizer["If you did not create an account, you can ignore this message."]));

    public Task SendPasswordResetLinkAsync(UserEntity user, string email, string resetLink) => transport.SendAsync(Compose(
        email,
        localizer["Reset your password"],
        user,
        localizer["Open this link to choose a new password for WhatchaWatching. It works for two hours."],
        resetLink,
        localizer["If you did not ask for it, you can ignore this message: your password has not changed."]));

    public Task SendPasswordResetCodeAsync(UserEntity user, string email, string resetCode) => transport.SendAsync(new EmailMessage(
        email,
        localizer["Your reset code"],
        $"{localizer.F("Hello {0},", user.UserName ?? string.Empty)}\n\n{localizer["Your code to reset your password:"]} {resetCode}",
        $"<p>{HtmlEncoder.Default.Encode(localizer.F("Hello {0},", user.UserName ?? string.Empty))}</p><p>{HtmlEncoder.Default.Encode(localizer["Your code to reset your password:"])} <b>{HtmlEncoder.Default.Encode(resetCode)}</b></p>"));

    private EmailMessage Compose(string to, string subject, UserEntity user, string intro, string link, string outro)
    {
        var name = user.UserName ?? string.Empty;
        var greeting = localizer.F("Hello {0},", name);
        var text = $"{greeting}\n\n{intro}\n{link}\n\n{outro}\n\nWhatchaWatching";
        var html = $"<p>{HtmlEncoder.Default.Encode(greeting)}</p>" +
                   $"<p>{HtmlEncoder.Default.Encode(intro)}</p>" +
                   $"<p><a href=\"{HtmlEncoder.Default.Encode(link)}\">{HtmlEncoder.Default.Encode(link)}</a></p>" +
                   $"<p>{HtmlEncoder.Default.Encode(outro)}</p><p>WhatchaWatching</p>";

        return new EmailMessage(to, subject, text, html);
    }
}
