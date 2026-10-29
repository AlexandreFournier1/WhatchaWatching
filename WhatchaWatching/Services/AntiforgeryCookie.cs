using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace WhatchaWatching.Services;

public static class AntiforgeryCookie
{
    /// <summary>
    /// The antiforgery cookie is not marked Secure by default, even on an https visit. This makes it follow the visit, like
    /// the cookie of the account: Secure over https, and still usable over plain http while the site answers on both.
    /// </summary>
    public static void Configure(AntiforgeryOptions options) =>
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
}
