using WhatchaWatching.Services.Localization;

namespace WhatchaWatching.Components.Shared;

/// <summary>One choice of a filter: what is kept when it is selected, and what is shown for it.</summary>
public sealed record FilterOption(string Value, string Label)
{
    /// <summary>Options whose text is also their value, like the names of genres or of platforms.</summary>
    public static IReadOnlyList<FilterOption> OfNames(IEnumerable<string> names) =>
        names.Select(name => new FilterOption(name, name)).ToList();

    /// <summary>Options whose value is a fixed English text and whose label is that text in the language of the visit.</summary>
    public static IReadOnlyList<FilterOption> Translated(IEnumerable<string> values, ILocalizer localizer) =>
        values.Select(value => new FilterOption(value, localizer[value])).ToList();
}
