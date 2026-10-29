namespace WhatchaWatching.Services.Email;

/// <summary>Stands in when no mailbox is configured: the site keeps working, and nothing is sent.</summary>
public sealed class UnconfiguredEmailTransport : IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("No mailbox is configured: set the Email section (Host, From, UserName, Password).");
}
