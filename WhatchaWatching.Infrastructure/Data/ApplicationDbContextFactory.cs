using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using WhatchaWatching.Common.Constants;

namespace WhatchaWatching.Infrastructure.Data;

/// <summary>
/// Used by the EF Core tools (<c>dotnet ef ...</c>) only.
/// Reads <c>ConnectionStrings:DefaultConnection</c> from user-secrets (the Infrastructure project shares the web project's
/// <c>UserSecretsId</c>), overridden by the <c>ConnectionStrings__DefaultConnection</c> environment variable.
/// Creating migrations needs no connection, so a placeholder without credentials is used when none is configured.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string PlaceholderConnectionString = "Server=localhost;Database=whatchawatching";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ApplicationDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(DatabaseConstants.CONNECTION_NAME)
            ?? PlaceholderConnectionString;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new ApplicationDbContext(options);
    }
}
