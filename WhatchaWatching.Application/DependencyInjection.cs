using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhatchaWatching.Infrastructure;
using WhatchaWatching.Middleware;

namespace WhatchaWatching.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        services.AddMiddleware();
        return services;
    }
}
