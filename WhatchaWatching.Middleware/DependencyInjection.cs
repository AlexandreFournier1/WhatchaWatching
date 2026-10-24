using Microsoft.Extensions.DependencyInjection;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Middleware.Jikan;

namespace WhatchaWatching.Middleware;

public static class DependencyInjection
{
    public static IServiceCollection AddMiddleware(this IServiceCollection services)
    {
        services.AddHttpClient<IJikanClient, JikanClient>(client =>
            {
                client.BaseAddress = new Uri(ExternalApiConstants.JIKAN_BASE_URL);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("WhatchaWatching");
            })
            .AddStandardResilienceHandler(options =>
            {
                // Jikan allows about 3 requests per second: a 429 is retried, honoring Retry-After.
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.Retry.ShouldRetryAfterHeader = true;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
