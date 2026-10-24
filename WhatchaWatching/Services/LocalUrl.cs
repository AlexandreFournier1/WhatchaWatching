namespace WhatchaWatching.Services;

public static class LocalUrl
{
    public const string HOME = "/";

    /// <summary>
    /// Returns the url when it stays on this site, otherwise the home page. A return url comes from the query string,
    /// so it must never send a freshly signed-in user to another site.
    /// </summary>
    public static string Normalize(string? url) =>
        !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
            ? url
            : HOME;
}
