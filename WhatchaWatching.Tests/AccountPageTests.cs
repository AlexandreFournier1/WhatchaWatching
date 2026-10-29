using WhatchaWatching.Services.Localization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WhatchaWatching.Infrastructure.Data;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Services.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Components.Pages;

namespace WhatchaWatching.Tests;

/// <summary>Renders the page like the server does: Blazor renders it as soon as the component waits for something.</summary>
public class AccountPageTests
{
    private sealed class SlowSettings(bool showsAdult) : IUserSettingsService
    {
        public async Task<bool> ShowsAdultContentAsync(int userId, CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            return showsAdult;
        }

        public Task SetShowsAdultContentAsync(int userId, bool value, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> GetCountryAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult("BE");

        public Task SetCountryAsync(int userId, string? country, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoAccounts : IAccountService
    {
        public Task<bool> DeleteAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class NoEmails : IAccountEmailService
    {
        public Task<bool> SendConfirmationAsync(UserEntity user) => Task.FromResult(true);

        public Task<bool> SendPasswordResetAsync(UserEntity user) => Task.FromResult(true);
    }

    private sealed class SlowCurrentUser : ICurrentUser
    {
        public async Task<int?> GetUserIdAsync()
        {
            await Task.Delay(50);
            return 7;
        }
    }

    private sealed class SignedInProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "alex"), new Claim(ClaimTypes.Email, "alex@example.test")], "test"))));
    }

    private sealed class TestHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Development";
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/account");
    }

    private static async Task<string> RenderAccountAsync(bool showsAdult)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(new TestHostEnvironment());
        services.AddRazorComponents();
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();
        services.AddSingleton<AuthenticationStateProvider, SignedInProvider>();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton<ILocalizer, Localizer>();
        services.AddSingleton<IUserSettingsService>(new SlowSettings(showsAdult));
        services.AddSingleton<ICurrentUser, SlowCurrentUser>();
        services.AddSingleton<IAccountService, NoAccounts>();
        services.AddSingleton<IAccountEmailService, NoEmails>();
        services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddHttpContextAccessor();
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.AddIdentityCore<UserEntity>().AddEntityFrameworkStores<ApplicationDbContext>().AddSignInManager().AddDefaultTokenProviders();

        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<Account>();
            return output.ToHtmlString();
        });
    }

    [Fact]
    public async Task Account_WhileItReadsTheSetting_StillRendersItsForm()
    {
        var html = await RenderAccountAsync(showsAdult: true);

        Assert.Contains("Show adult content", html);
        Assert.Contains("alex", html);
    }

    [Fact]
    public async Task Account_ShowsTheStoredSettingInTheCheckbox()
    {
        var on = await RenderAccountAsync(showsAdult: true);
        var off = await RenderAccountAsync(showsAdult: false);

        Assert.Contains("checked", on);
        Assert.DoesNotContain("checked", off);
    }

    [Fact]
    public async Task Account_OffersToChangeThePasswordAndToDeleteTheAccount()
    {
        var html = await RenderAccountAsync(showsAdult: false);

        Assert.Contains("Change my password", html);
        Assert.Contains("Current password", html);
        Assert.Contains("Delete my account", html);
        Assert.Contains("href=\"privacy\"", html);
    }

    [Fact]
    public async Task Account_AsksForThePasswordAndAConfirmationBeforeDeleting()
    {
        var html = await RenderAccountAsync(showsAdult: false);

        Assert.Contains("id=\"delete-password\"", html);
        Assert.Contains("I understand that this cannot be undone", html);
        Assert.Contains("btn btn-danger", html);
    }
}
