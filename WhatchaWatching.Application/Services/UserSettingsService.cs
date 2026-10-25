using Microsoft.EntityFrameworkCore;
using WhatchaWatching.Infrastructure.Data;

namespace WhatchaWatching.Application.Services;

public class UserSettingsService(IDbContextFactory<ApplicationDbContext> contextFactory) : IUserSettingsService
{
    public async Task<bool> ShowsAdultContentAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await ShowsAdultContentAsync(context, userId, cancellationToken);
    }

    public async Task SetShowsAdultContentAsync(int userId, bool value, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException($"There is no user {userId}.");

        user.ShowAdultContent = value;
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The same lookup for the services that already hold a context.</summary>
    internal static Task<bool> ShowsAdultContentAsync(
        ApplicationDbContext context, int userId, CancellationToken cancellationToken) =>
        context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.ShowAdultContent)
            .FirstOrDefaultAsync(cancellationToken);
}
