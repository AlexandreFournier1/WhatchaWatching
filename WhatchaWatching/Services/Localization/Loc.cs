using System.Globalization;

namespace WhatchaWatching.Services.Localization;

/// <summary>For the helpers that have no localizer of their own: without one, the English text.</summary>
public static class Loc
{
    public static string T(ILocalizer? localizer, string text, params object?[] args) =>
        localizer is not null
            ? (args.Length == 0 ? localizer[text] : localizer.F(text, args))
            : (args.Length == 0 ? text : string.Format(CultureInfo.InvariantCulture, text, args));
}
