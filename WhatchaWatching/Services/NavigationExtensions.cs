using Microsoft.AspNetCore.Components;

namespace WhatchaWatching.Services;

public static class NavigationExtensions
{
    /// <summary>The login page, coming back to the current page afterwards.</summary>
    public static string GetLoginUrl(this NavigationManager navigation) =>
        $"login?returnUrl={Uri.EscapeDataString("/" + navigation.ToBaseRelativePath(navigation.Uri))}";
}
