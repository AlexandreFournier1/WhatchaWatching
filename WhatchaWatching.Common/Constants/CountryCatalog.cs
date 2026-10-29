namespace WhatchaWatching.Common.Constants;

/// <summary>
/// The countries a person can choose for where to watch. Streaming offers differ from one country to the next, and
/// TMDB gives them for every country at once: only these are kept, so that a title does not store a hundred of them.
/// </summary>
public static class CountryCatalog
{
    /// <summary>The country whose platforms are shown to someone who chose none.</summary>
    public const string DEFAULT_CODE = "FR";

    /// <summary>ISO 3166-1 codes are two letters.</summary>
    public const int CODE_LENGTH = 2;

    public static IReadOnlyList<(string Code, string Name)> All { get; } =
    [
        ("FR", "France"),
        ("BE", "Belgium"),
        ("CH", "Switzerland"),
        ("LU", "Luxembourg"),
        ("CA", "Canada"),
        ("US", "United States"),
        ("GB", "United Kingdom"),
        ("DE", "Germany"),
        ("ES", "Spain"),
        ("IT", "Italy")
    ];

    public static IReadOnlyList<string> Codes { get; } = All.Select(c => c.Code).ToList();

    /// <summary>The code in capitals when it belongs to the catalog, otherwise null.</summary>
    public static string? Normalize(string? code) =>
        All.FirstOrDefault(c => string.Equals(c.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase)) is { Code: not null } found
            ? found.Code
            : null;

    public static string NameOf(string code) =>
        All.FirstOrDefault(c => c.Code == code) is { Name: not null } found ? found.Name : code;
}
