using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhatchaWatching.Application;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Infrastructure.Data;

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
    public void AddApplication_WithoutConnectionString_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddApplication(BuildConfiguration(null)));
    }
}
