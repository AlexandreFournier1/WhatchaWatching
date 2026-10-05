using Microsoft.Extensions.DependencyInjection;

namespace WhatchaWatching.Middleware;

public static class DependencyInjection
{
    public static IServiceCollection AddMiddleware(this IServiceCollection services)
    {
        return services;
    }
}
