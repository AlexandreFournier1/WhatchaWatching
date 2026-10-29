using Microsoft.AspNetCore.Localization;

namespace WhatchaWatching.Services.Localization;

public static class LanguageEndpoint
{
    public const string PATH = "/language";

    public static readonly string[] SupportedLanguages = [Localizer.ENGLISH, Localizer.FRENCH];

    /// <summary>Remembers the language chosen in a cookie, then goes back to the page the visitor was on.</summary>
    public static IResult Choose(HttpContext context, string? culture, string? returnUrl)
    {
        if (culture is not null && SupportedLanguages.Contains(culture, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture.ToLowerInvariant())),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    // A preference the site needs to work, set by the visitor's own choice.
                    IsEssential = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = context.Request.IsHttps
                });
        }

        // The address comes from the query string: it must never lead to another site.
        return TypedResults.LocalRedirect(LocalUrl.Normalize(returnUrl));
    }
}
