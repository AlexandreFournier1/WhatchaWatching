namespace WhatchaWatching.Services.Email;

public sealed record EmailMessage(string To, string Subject, string Text, string Html);

/// <summary>Sends a message. Throws when it cannot: the caller decides what the visitor is told.</summary>
public interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
