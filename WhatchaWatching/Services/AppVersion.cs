using System.Reflection;

namespace WhatchaWatching.Services;

public static class AppVersion
{
    private const int SHORT_REVISION_LENGTH = 7;

    /// <summary>For example <c>v0.1.0 (318545d)</c>: the version of the project and the commit it was built from.</summary>
    public static string Display { get; } = Format(
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>
    /// Turns an informational version (<c>0.1.0+318545d1f0...</c>, where the SDK adds the commit after the plus sign)
    /// into the text shown to visitors. Returns an empty string when there is nothing to show.
    /// </summary>
    public static string Format(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return string.Empty;
        }

        var parts = informationalVersion.Split('+', 2);
        var revision = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        var shortRevision = revision.Length > SHORT_REVISION_LENGTH ? revision[..SHORT_REVISION_LENGTH] : revision;

        return shortRevision.Length == 0 ? $"v{parts[0]}" : $"v{parts[0]} ({shortRevision})";
    }
}
