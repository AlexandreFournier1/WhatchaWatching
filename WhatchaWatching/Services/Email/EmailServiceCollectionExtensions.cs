using Microsoft.AspNetCore.Identity;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Services.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>
    /// The messages of the account. Without a mailbox in the configuration the site still starts: sending then fails
    /// and the pages say so (or, for a password reset, stay silent about it, like for an unknown address).
    /// </summary>
    public static IServiceCollection AddAccountEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SECTION));

        var configured = configuration.GetSection(EmailOptions.SECTION).Get<EmailOptions>()?.IsConfigured == true;
        if (configured)
        {
            services.AddSingleton<IEmailTransport, SmtpEmailTransport>();
        }
        else
        {
            services.AddSingleton<IEmailTransport, UnconfiguredEmailTransport>();
        }

        services.AddScoped<IEmailSender<UserEntity>, IdentityEmailSender>();
        services.AddScoped<IAccountEmailService, AccountEmailService>();

        return services;
    }
}
