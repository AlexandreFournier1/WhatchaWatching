using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Infrastructure.Data;

namespace WhatchaWatching.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseConstants.CONNECTION_NAME)
            ?? throw new InvalidOperationException(
                $"Connection string '{DatabaseConstants.CONNECTION_NAME}' is not configured. " +
                "Set it in user-secrets or the environment variable 'ConnectionStrings__" + DatabaseConstants.CONNECTION_NAME + "'.");

        // A factory gives Blazor Server services short-lived contexts (a circuit-long context is not safe).
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mySql => mySql.EnableRetryOnFailure()));

        // Identity resolves the context directly, so also expose a scoped one built from the factory.
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

        return services;
    }
}
