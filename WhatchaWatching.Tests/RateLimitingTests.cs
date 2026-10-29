using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WhatchaWatching.Services;

namespace WhatchaWatching.Tests;

public class RateLimitingTests
{
    private static async Task<IHost> StartAsync() => await new HostBuilder()
        .ConfigureWebHost(web => web
            .UseTestServer()
            .ConfigureServices(services => services.AddRateLimiter(RateLimiting.Configure))
            .Configure(app =>
            {
                app.UseRateLimiter();
                app.Run(context => context.Response.WriteAsync("ok"));
            }))
        .StartAsync();

    [Fact]
    public async Task Login_AfterTooManyAttempts_AnswersTooManyRequests()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();

        for (var attempt = 0; attempt < RateLimiting.AUTH_ATTEMPTS; attempt++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/login", null)).StatusCode);
        }

        var rejected = await client.PostAsync("/login", null);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.RetryAfter is not null);
        Assert.Contains("Too many requests", await rejected.Content.ReadAsStringAsync());
        Assert.StartsWith("text/plain", rejected.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/register")]
    [InlineData("/forgot-password")]
    [InlineData("/reset-password")]
    [InlineData("/account")]
    public async Task EveryPageWhereAPasswordIsTried_IsLimited(string path)
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();

        for (var attempt = 0; attempt < RateLimiting.AUTH_ATTEMPTS; attempt++)
        {
            await client.PostAsync(path, null);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync(path, null)).StatusCode);
    }

    [Fact]
    public async Task ShowingTheLoginPage_IsNotLimitedLikeSendingTheForm()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();

        for (var visit = 0; visit < RateLimiting.AUTH_ATTEMPTS + 5; visit++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/login")).StatusCode);
        }
    }

    [Fact]
    public async Task TheForms_AndTheRestOfTheSite_DoNotShareACounter()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();

        for (var attempt = 0; attempt <= RateLimiting.AUTH_ATTEMPTS; attempt++)
        {
            await client.PostAsync("/login", null);
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/search?q=matrix")).StatusCode);
    }

    [Fact]
    public async Task Files_AreNeverCounted()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();

        for (var request = 0; request < RateLimiting.GENERAL_REQUESTS + 20; request++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/app.css")).StatusCode);
        }
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("10.1.2.3", true)]
    [InlineData("172.20.0.5", true)]
    [InlineData("192.168.1.10", true)]
    [InlineData("::ffff:10.0.0.7", true)]
    [InlineData("172.32.0.1", false)]
    [InlineData("81.2.69.142", false)]
    [InlineData("2a01:cb00::1", false)]
    public void LooksLikeAProxy_RecognizesPrivateAndLoopbackAddresses(string address, bool expected)
    {
        Assert.Equal(expected, RateLimiting.LooksLikeAProxy(IPAddress.Parse(address)));
    }

    [Fact]
    public void LooksLikeAProxy_IsFalseWithoutAnAddress()
    {
        Assert.False(RateLimiting.LooksLikeAProxy(null));
    }
}
