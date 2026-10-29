using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Infrastructure.Data;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Services.Email;
using WhatchaWatching.Services.Localization;

namespace WhatchaWatching.Tests;

public class EmailTests
{
    private sealed class Site : NavigationManager
    {
        public Site() => Initialize("https://whatcha-watching.alwaysdata.net/", "https://whatcha-watching.alwaysdata.net/account");
    }

    private sealed class Outbox : IEmailTransport
    {
        public List<EmailMessage> Sent { get; } = [];

        public bool Fails { get; set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (Fails)
            {
                throw new InvalidOperationException("The mail server refused it.");
            }

            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class Fixture : IDisposable
    {
        public Outbox Outbox { get; } = new();

        public ServiceProvider Provider { get; }

        public Fixture()
        {
            Provider = new ServiceCollection()
                .AddLogging()
                .AddDataProtection().Services
                .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()))
                .AddIdentityCore<UserEntity>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .Services
                .AddSingleton<IEmailTransport>(Outbox)
                .AddSingleton<NavigationManager, Site>()
                .AddSingleton<ILocalizer, Localizer>()
                .AddScoped<IEmailSender<UserEntity>, IdentityEmailSender>()
                .AddScoped<IAccountEmailService, AccountEmailService>()
                .BuildServiceProvider();
        }

        public UserManager<UserEntity> Users => Provider.GetRequiredService<UserManager<UserEntity>>();

        public IAccountEmailService Emails => Provider.GetRequiredService<IAccountEmailService>();

        public async Task<UserEntity> CreateUserAsync()
        {
            var user = new UserEntity { UserName = "alex", Email = "alex@example.test" };
            Assert.True((await Users.CreateAsync(user, "Password1!")).Succeeded);
            return user;
        }

        public void Dispose() => Provider.Dispose();
    }

    private static string LinkIn(EmailMessage message) =>
        message.Text.Split('\n').Single(line => line.StartsWith("https://"));

    private static string QueryValue(string link, string name) =>
        Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(link).Query)[name].ToString();

    [Theory]
    [InlineData("CfDJ8N+/abc=")]
    [InlineData("a token with spaces & symbols?")]
    public void TokenCodec_RoundTripsATokenThatDoesNotBelongInAnAddress(string token)
    {
        var code = TokenCodec.Encode(token);

        Assert.DoesNotContain('+', code);
        Assert.DoesNotContain('/', code);
        Assert.DoesNotContain('=', code);
        Assert.True(TokenCodec.TryDecode(code, out var decoded));
        Assert.Equal(token, decoded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not base 64 !")]
    [InlineData("a")]
    public void TokenCodec_RefusesWhatIsNotACode(string? code)
    {
        Assert.False(TokenCodec.TryDecode(code, out _));
    }

    [Fact]
    public async Task Confirmation_SendsAnHttpsLinkToTheEmailOfTheAccount()
    {
        using var fixture = new Fixture();
        var user = await fixture.CreateUserAsync();

        Assert.True(await fixture.Emails.SendConfirmationAsync(user));

        var message = Assert.Single(fixture.Outbox.Sent);
        Assert.Equal("alex@example.test", message.To);
        var link = LinkIn(message);
        Assert.StartsWith("https://whatcha-watching.alwaysdata.net/confirm-email?", link);
        Assert.Equal(user.Id.ToString(), QueryValue(link, "userId"));
        Assert.Contains(link.Replace("&", "&amp;"), message.Html);
    }

    [Fact]
    public async Task TheConfirmationLink_ConfirmsTheAddress()
    {
        using var fixture = new Fixture();
        var user = await fixture.CreateUserAsync();
        await fixture.Emails.SendConfirmationAsync(user);

        var code = QueryValue(LinkIn(fixture.Outbox.Sent.Single()), "code");
        Assert.True(TokenCodec.TryDecode(code, out var token));
        Assert.True((await fixture.Users.ConfirmEmailAsync(user, token)).Succeeded);

        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task TheResetLink_ChangesThePassword_OnlyOnce()
    {
        using var fixture = new Fixture();
        var user = await fixture.CreateUserAsync();
        await fixture.Emails.SendPasswordResetAsync(user);

        var link = LinkIn(fixture.Outbox.Sent.Single());
        Assert.StartsWith("https://whatcha-watching.alwaysdata.net/reset-password?", link);
        Assert.Equal("alex@example.test", QueryValue(link, "email"));
        Assert.True(TokenCodec.TryDecode(QueryValue(link, "code"), out var token));

        Assert.True((await fixture.Users.ResetPasswordAsync(user, token, "NewPassword2!")).Succeeded);
        Assert.True(await fixture.Users.CheckPasswordAsync(user, "NewPassword2!"));

        // The password changed, so the stamp did, so the same link is dead.
        var again = await fixture.Users.ResetPasswordAsync(user, token, "Another3!");
        Assert.False(again.Succeeded);
        Assert.Contains(again.Errors, e => e.Code == nameof(IdentityErrorDescriber.InvalidToken));
    }

    [Fact]
    public async Task TheResetLink_OfAnotherAccount_DoesNotWork()
    {
        using var fixture = new Fixture();
        var alex = await fixture.CreateUserAsync();
        var sam = new UserEntity { UserName = "sam", Email = "sam@example.test" };
        Assert.True((await fixture.Users.CreateAsync(sam, "Password1!")).Succeeded);
        await fixture.Emails.SendPasswordResetAsync(alex);
        Assert.True(TokenCodec.TryDecode(QueryValue(LinkIn(fixture.Outbox.Sent.Single()), "code"), out var token));

        Assert.False((await fixture.Users.ResetPasswordAsync(sam, token, "Stolen4!")).Succeeded);
    }

    [Fact]
    public async Task WhenTheMailServerFails_TheServiceSaysSo_AndDoesNotThrow()
    {
        using var fixture = new Fixture();
        var user = await fixture.CreateUserAsync();
        fixture.Outbox.Fails = true;

        Assert.False(await fixture.Emails.SendConfirmationAsync(user));
        Assert.False(await fixture.Emails.SendPasswordResetAsync(user));
    }

    [Fact]
    public async Task TheLinkIsNeverWrittenToTheLogs()
    {
        var logs = new List<string>();
        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(new CapturingLoggerProvider(logs)))
            .AddDataProtection().Services
            .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()))
            .AddIdentityCore<UserEntity>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders().Services
            .AddSingleton<IEmailTransport>(new Outbox { Fails = true })
            .AddSingleton<NavigationManager, Site>()
            .AddSingleton<ILocalizer, Localizer>()
            .AddScoped<IEmailSender<UserEntity>, IdentityEmailSender>()
            .AddScoped<IAccountEmailService, AccountEmailService>();

        await using var provider = services.BuildServiceProvider();
        var users = provider.GetRequiredService<UserManager<UserEntity>>();
        var user = new UserEntity { UserName = "alex", Email = "alex@example.test" };
        await users.CreateAsync(user, "Password1!");

        await provider.GetRequiredService<IAccountEmailService>().SendPasswordResetAsync(user);

        Assert.NotEmpty(logs);
        Assert.DoesNotContain(logs, line => line.Contains("code=") || line.Contains("reset-password"));
    }

    [Fact]
    public async Task AnAccountWithoutAnEmail_GetsNothing()
    {
        using var fixture = new Fixture();

        Assert.False(await fixture.Emails.SendConfirmationAsync(new UserEntity { UserName = "x" }));
        Assert.Empty(fixture.Outbox.Sent);
    }

    [Fact]
    public async Task TheHtmlOfAMessage_EncodesTheLink()
    {
        var outbox = new Outbox();
        var sender = new IdentityEmailSender(outbox, new Localizer());

        await sender.SendConfirmationLinkAsync(
            new UserEntity { UserName = "alex" }, "alex@example.test", "https://site.test/confirm?a=1&b=\"2\"<x>");

        var html = outbox.Sent.Single().Html;
        Assert.DoesNotContain("<x>", html);
        Assert.Contains("&amp;b=", html);
        Assert.Contains("https://site.test/confirm?a=1&b=\"2\"<x>", outbox.Sent.Single().Text);
    }

    [Fact]
    public async Task Unconfigured_Throws_WhichTheServiceTurnsIntoAFailedSend()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UnconfiguredEmailTransport().SendAsync(new EmailMessage("a@b.test", "s", "t", "h")));
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("smtp.example.test", null, false)]
    [InlineData(null, "site@example.test", false)]
    [InlineData("smtp.example.test", "site@example.test", true)]
    public void TheMailboxIsConfigured_WhenHostAndSenderAreGiven(string? host, string? from, bool expected)
    {
        Assert.Equal(expected, new EmailOptions { Host = host, From = from }.IsConfigured);
    }

    [Theory]
    [InlineData(false, typeof(UnconfiguredEmailTransport))]
    [InlineData(true, typeof(SmtpEmailTransport))]
    public void TheTransport_FollowsTheConfiguration(bool configured, Type expected)
    {
        var values = configured
            ? new Dictionary<string, string?> { ["Email:Host"] = "smtp.example.test", ["Email:From"] = "site@example.test" }
            : new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        using var provider = new ServiceCollection()
            .AddLogging()
            .AddAccountEmail(configuration)
            .BuildServiceProvider();

        Assert.IsType(expected, provider.GetRequiredService<IEmailTransport>());
    }

    private sealed class CapturingLoggerProvider(List<string> lines) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(lines);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(List<string> lines) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                lines.Add(formatter(state, exception) + " " + exception);
        }
    }
}
