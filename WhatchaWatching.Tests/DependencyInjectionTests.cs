using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhatchaWatching.Application;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Infrastructure.Data;
using WhatchaWatching.Middleware;
using WhatchaWatching.Middleware.AniList;
using WhatchaWatching.Middleware.Jikan;
using WhatchaWatching.Middleware.Tmdb;

namespace WhatchaWatching.Tests;

public class DependencyInjectionTests
{
    private static IConfiguration BuildConfiguration(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(connectionString is null
                ? []
                : [new($"ConnectionStrings:{DatabaseConstants.CONNECTION_NAME}", connectionString)])
            .Build();

    [Fact]
    public void AddApplication_BuildsServiceProvider()
    {
        var services = new ServiceCollection()
            .AddApplication(BuildConfiguration("Server=localhost;Database=whatchawatching"));

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider);
    }

    [Fact]
    public void AddApplication_RegistersDbContextFactory()
    {
        var services = new ServiceCollection()
            .AddApplication(BuildConfiguration("Server=localhost;Database=whatchawatching"));

        Assert.Contains(services, d => d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>));
    }

    [Fact]
    public void AddApplication_RegistersTheJikanClient()
    {
        var services = new ServiceCollection()
            .AddApplication(BuildConfiguration("Server=localhost;Database=whatchawatching"));

        using var provider = services.BuildServiceProvider();

        Assert.IsType<JikanClient>(provider.GetRequiredService<IJikanClient>());
        Assert.IsType<AniListClient>(provider.GetRequiredService<IAniListClient>());
    }

    [Fact]
    public void AddApplication_RegistersTheTmdbClient_EvenWithoutAnAccessToken()
    {
        var services = new ServiceCollection()
            .AddApplication(BuildConfiguration("Server=localhost;Database=whatchawatching"));

        using var provider = services.BuildServiceProvider();

        Assert.IsType<TmdbClient>(provider.GetRequiredService<ITmdbClient>());
        Assert.False(provider.GetRequiredService<TmdbOptions>().IsConfigured);
        Assert.Equal(ExternalApiConstants.TMDB_DEFAULT_WATCH_REGION, provider.GetRequiredService<TmdbOptions>().WatchRegion);
    }

    [Fact]
    public void AddApplication_RegistersTheFallbackProviderAndOneSharedCooldown()
    {
        var services = new ServiceCollection()
            .AddApplication(BuildConfiguration("Server=localhost;Database=whatchawatching"));

        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        Assert.IsType<FallbackAnimeProvider>(firstScope.ServiceProvider.GetRequiredService<IAnimeProvider>());
        Assert.Same(
            firstScope.ServiceProvider.GetRequiredService<ProviderCooldown>(),
            secondScope.ServiceProvider.GetRequiredService<ProviderCooldown>());
    }

    [Fact]
    public void AddApplication_RegistersTheApplicationServices()
    {
        var services = new ServiceCollection()
            .AddApplication(BuildConfiguration("Server=localhost;Database=whatchawatching"));

        // Not resolved: building the database options connects to MySQL to detect its version.
        Assert.Contains(services, d => d.ServiceType == typeof(IAnimeService) && d.ImplementationType == typeof(AnimeService));
        Assert.Contains(services, d => d.ServiceType == typeof(IWatchListService) && d.ImplementationType == typeof(WatchListService));
        Assert.Contains(services, d => d.ServiceType == typeof(IRandomPickerService) && d.ImplementationType == typeof(RandomPickerService));
        Assert.Contains(services, d => d.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    public void AddApplication_WithAConfiguredServerVersion_DoesNotConnectToTheDatabase()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new($"ConnectionStrings:{DatabaseConstants.CONNECTION_NAME}", "Server=127.0.0.1;Port=1;Database=x;Connection Timeout=1"),
                new(DatabaseConstants.SERVER_VERSION_KEY, "8.0.36-mysql")
            ])
            .Build();
        using var provider = new ServiceCollection().AddApplication(configuration).BuildServiceProvider();

        using var context = provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext();

        Assert.NotNull(context);
    }

    [Fact]
    public void AddApplication_WithoutConnectionString_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddApplication(BuildConfiguration(null)));
    }
}
