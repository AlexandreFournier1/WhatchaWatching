namespace WhatchaWatching.Services.Email;

/// <summary>The "Email" section of the configuration: the mailbox the site writes from.</summary>
public sealed class EmailOptions
{
    public const string SECTION = "Email";

    public string? Host { get; set; }

    /// <summary>587 with STARTTLS, the usual port for sending.</summary>
    public int Port { get; set; } = 587;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? From { get; set; }

    public string FromName { get; set; } = "WhatchaWatching";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
}
