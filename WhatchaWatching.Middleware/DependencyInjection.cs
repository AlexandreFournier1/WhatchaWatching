using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Middleware.AniList;
using WhatchaWatching.Middleware.Jikan;

namespace WhatchaWatching.Middleware;

public static class DependencyInjection
{
    public static IServiceCollection AddMiddleware(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddHttpClient<IJikanClient, JikanClient>(client =>
            {
                client.BaseAddress = new Uri(ExternalApiConstants.JIKAN_BASE_URL);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("WhatchaWatching");
            })
            .AddStandardResilienceHandler(ConfigureResilience);

        services.AddHttpClient<IAniListClient, AniListClient>(client =>
            {
                client.BaseAddress = new Uri(ExternalApiConstants.ANILIST_URL);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("WhatchaWatching");
            })
            .AddStandardResilienceHandler(ConfigureResilience);

        // Shared by every request, so that a provider that is down is skipped for a while.
        services.AddSingleton<ProviderCooldown>();
        services.AddScoped<IAnimeProvider, FallbackAnimeProvider>();

        return services;
    }

    // A person is waiting for the page: give up on a provider quickly, the next one may answer.
    private static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        // A 429 is retried once, honoring Retry-After (Jikan allows about 3 requests per second).
        options.Retry.MaxRetryAttempts = 1;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.ShouldRetryAfterHeader = true;
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(12);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    }
}
