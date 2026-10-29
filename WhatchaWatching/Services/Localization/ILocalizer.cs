namespace WhatchaWatching.Services.Localization;

/// <summary>
/// The text of the interface in the language of the visit. The English text is the key: it is what a page writes, what a
/// test reads, and what is shown when there is no translation.
/// </summary>
public interface ILocalizer
{
    /// <summary>"en" or "fr".</summary>
    string Language { get; }

    string this[string text] { get; }

    /// <summary>The text with {0}, {1}... replaced by the arguments.</summary>
    string F(string text, params object?[] args);
}
