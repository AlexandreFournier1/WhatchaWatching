using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Middleware.AniList;
using WhatchaWatching.Middleware.Jikan;
using WhatchaWatching.Middleware.Tmdb;

namespace WhatchaWatching.Middleware;

public static class DependencyInjection
{
    public static IServiceCollection AddMiddleware(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        // A missing token must not stop the site: anime keep working and TMDB calls report that it is unavailable.
        services.AddSingleton(new TmdbOptions(
            configuration["Tmdb:AccessToken"],
            configuration["Tmdb:WatchRegion"] is { Length: > 0 } region ? region : ExternalApiConstants.TMDB_DEFAULT_WATCH_REGION));

        services.AddHttpClient<ITmdbClient, TmdbClient>((provider, client) =>
            {
                client.BaseAddress = new Uri(ExternalApiConstants.TMDB_BASE_URL);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("WhatchaWatching");
                if (provider.GetRequiredService<TmdbOptions>().AccessToken is { Length: > 0 } token)
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            })
            .AddStandardResilienceHandler(ConfigureResilience);

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
