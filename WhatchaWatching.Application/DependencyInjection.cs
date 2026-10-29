using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Infrastructure;
using WhatchaWatching.Middleware;

namespace WhatchaWatching.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        services.AddMiddleware(configuration);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IWatchListService, WatchListService>();
        services.AddScoped<ICustomAnimeService, CustomAnimeService>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IRandomPickerService, RandomPickerService>();

        return services;
    }
}
