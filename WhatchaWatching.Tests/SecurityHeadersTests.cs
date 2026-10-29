using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WhatchaWatching.Services;

namespace WhatchaWatching.Tests;

public class SecurityHeadersTests
{
    private static async Task<HttpResponseMessage> GetAsync(string path)
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services.AddAntiforgery())
                .Configure(app =>
                {
                    app.UseSecurityHeaders();
                    app.Run(async context =>
                    {
                        // A form page: generating the token makes the framework write its own headers.
                        if (context.Request.Path == "/form")
                        {
                            context.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);
                        }

                        await context.Response.WriteAsync("ok");
                    });
                }))
            .StartAsync();

        return await host.GetTestClient().GetAsync(path);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/form")]
    public async Task EveryResponse_HasTheSecurityHeaders(string path)
    {
        var response = await GetAsync(path);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("same-origin", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("same-origin", response.Headers.GetValues("Cross-Origin-Opener-Policy").Single());
        Assert.Contains("camera=()", response.Headers.GetValues("Permissions-Policy").Single());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/form")]
    public async Task ThePolicy_IsTheOneOfTheSite_EvenOnAFormPage(string path)
    {
        // The antiforgery token of a form page writes its own policy (frame-ancestors only): ours must replace it.
        var policy = (await GetAsync(path)).Headers.GetValues("Content-Security-Policy").Single();

        Assert.Equal(SecurityHeaders.POLICY, policy);
    }

    [Fact]
    public void ThePolicy_StopsFramingForeignFormsAndPlugins()
    {
        Assert.Contains("frame-ancestors 'none'", SecurityHeaders.POLICY);
        Assert.Contains("form-action 'self'", SecurityHeaders.POLICY);
        Assert.Contains("base-uri 'self'", SecurityHeaders.POLICY);
        Assert.Contains("object-src 'none'", SecurityHeaders.POLICY);
    }

    [Fact]
    public void ThePolicy_OnlyLoadsScriptsFromTheSite_AndAllowsThePostersAndTheCircuit()
    {
        Assert.Contains("default-src 'self'", SecurityHeaders.POLICY);
        Assert.Contains("script-src 'self' 'unsafe-inline'", SecurityHeaders.POLICY);
        Assert.Contains("img-src 'self' data: https:", SecurityHeaders.POLICY);
        Assert.Contains("connect-src 'self' wss: ws:", SecurityHeaders.POLICY);
        Assert.DoesNotContain("http:", SecurityHeaders.POLICY.Replace("https:", string.Empty));
    }

    [Fact]
    public async Task NoPolicyIsOnlyReported()
    {
        Assert.False((await GetAsync("/")).Headers.Contains("Content-Security-Policy-Report-Only"));
    }
}
