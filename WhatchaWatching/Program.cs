using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using WhatchaWatching.Application;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Components;
using WhatchaWatching.Infrastructure.Data;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Services;
using WhatchaWatching.Services.Email;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddApplication(builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(ProxyHeaders.Configure);
builder.Services.AddAntiforgery(AntiforgeryCookie.Configure);
builder.Services.AddRateLimiter(RateLimiting.Configure);
builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies(cookies => cookies.ApplicationCookie?.Configure(options =>
    {
        // Where an anonymous visitor is sent by [Authorize] on a page.
        options.LoginPath = "/login";
        options.ReturnUrlParameter = "returnUrl";

        // Secure over https, and still usable over plain http while the site answers on both.
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
    }));

builder.Services.AddIdentityCore<UserEntity>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// A link to confirm an email or to reset a password is only worth a short while.
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(2));

builder.Services.AddAccountEmail(builder.Configuration);

builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<AdultFilterState>();

var app = builder.Build();

// First, so that everything after it sees the scheme and address of the visitor, not the proxy's.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    // Without X-Forwarded-For every visitor would share the address of the proxy, and so one rate limit.
    var proxyWarned = 0;
    app.Use((context, next) =>
    {
        if (RateLimiting.LooksLikeAProxy(context.Connection.RemoteIpAddress) && Interlocked.Exchange(ref proxyWarned, 1) == 0)
        {
            app.Logger.LogWarning(
                "Requests come from the private address {Address}: X-Forwarded-For is missing, so the rate limits count all visitors together.",
                context.Connection.RemoteIpAddress);
        }

        return next(context);
    });
}

app.UseSecurityHeaders();
app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Signing out changes the auth cookie, which a Blazor circuit cannot do: it is a plain form post.
// The form field makes the endpoint validate the antiforgery token.
app.MapPost("/logout", async (SignInManager<UserEntity> signInManager, [FromForm] string? returnUrl) =>
{
    await signInManager.SignOutAsync();
    return TypedResults.LocalRedirect(LocalUrl.Normalize(returnUrl));
});

app.Run();
