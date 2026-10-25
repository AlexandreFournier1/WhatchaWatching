using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Middleware;

internal static class StreamingLinkFilter
{
    /// <summary>
    /// Keeps the links that can be shown and stored safely. The addresses end up in <c>href</c> attributes, so only
    /// http and https are accepted (never <c>javascript:</c>), and one that is too long for its column is dropped
    /// rather than cut, since a cut address would lead nowhere.
    /// </summary>
    public static IReadOnlyList<StreamingLinkDto> Clean(IEnumerable<(string? Site, string? Url)> links) =>
        links
            .Select(l => (Site: l.Site?.Trim(), Url: l.Url?.Trim()))
            .Where(l => !string.IsNullOrEmpty(l.Site) && l.Site.Length <= ConfigurationConstants.SMALL_SIZE)
            .Where(l => !string.IsNullOrEmpty(l.Url) && l.Url.Length <= ConfigurationConstants.LARGE_SIZE && IsWebAddress(l.Url))
            .Select(l => new StreamingLinkDto(l.Site!, l.Url!))
            .DistinctBy(l => l.Url, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool IsWebAddress(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}
