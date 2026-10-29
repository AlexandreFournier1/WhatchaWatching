using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace WhatchaWatching.Services.Email;

public sealed class SmtpEmailTransport(IOptions<EmailOptions> options) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        // STARTTLS is required: the password of the mailbox must not travel in clear text.
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15_000
        };

        if (!string.IsNullOrEmpty(settings.UserName))
        {
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(settings.From!, settings.FromName),
            Subject = message.Subject,
            Body = message.Text
        };
        mail.To.Add(new MailAddress(message.To));
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.Html, null, "text/html"));

        await client.SendMailAsync(mail, cancellationToken);
    }
}
