using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WhatchaWatching.Application;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Components;
using WhatchaWatching.Infrastructure.Data;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddApplication(builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(ProxyHeaders.Configure);

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
    }));

builder.Services.AddIdentityCore<UserEntity>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<AdultFilterState>();

var app = builder.Build();

// First, so that everything after it sees the scheme and address of the visitor, not the proxy's.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
