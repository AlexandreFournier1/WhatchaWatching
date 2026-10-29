using System.Globalization;

namespace WhatchaWatching.Services.Localization;

/// <summary>Reads the language from the culture of the visit, which the request localization sets from the cookie or the browser.</summary>
public sealed class Localizer : ILocalizer
{
    public const string FRENCH = "fr";
    public const string ENGLISH = "en";

    public string Language =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == FRENCH ? FRENCH : ENGLISH;

    public string this[string text] =>
        Language == FRENCH && FrenchTranslations.Texts.TryGetValue(text, out var translated) ? translated : text;

    public string F(string text, params object?[] args) => string.Format(CultureInfo.CurrentCulture, this[text], args);
}
