using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhatchaWatching.Application;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Infrastructure.Data;

namespace WhatchaWatching.Tests;

public class DatabaseConnectionErrorTests
{
    [Fact]
    public void ResolvingTheDatabase_WhenItDoesNotAnswer_NamesTheServerButNeverTheCredentials()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new($"ConnectionStrings:{DatabaseConstants.CONNECTION_NAME}",
                    "Server=127.0.0.1;Port=1;Database=x;User ID=root;Password=hunter2-secret;Connection Timeout=1")
            ])
            .Build();
        using var provider = new ServiceCollection().AddApplication(configuration).BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

        Assert.Contains("127.0.0.1:1", exception.Message);
        Assert.DoesNotContain("hunter2-secret", exception.Message);
        Assert.DoesNotContain("root", exception.Message);
    }
}
