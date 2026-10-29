using Microsoft.EntityFrameworkCore;
using WhatchaWatching.Infrastructure.Data;

namespace WhatchaWatching.Application.Services;

public class AccountService(IDbContextFactory<ApplicationDbContext> contextFactory) : IAccountService
{
    public async Task<bool> DeleteAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        // The list goes first: a title cannot be removed while a list points to it.
        context.WatchListItems.RemoveRange(context.WatchListItems.Where(i => i.UserId == userId));

        // An anime added by hand exists only for its owner: nothing of it stays behind.
        var custom = await context.Media
            .Include(m => m.MediaGenres)
            .Include(m => m.StreamingLinks)
            .Where(m => m.CreatedByUserId == userId)
            .ToListAsync(cancellationToken);
        context.Media.RemoveRange(custom);

        context.Users.Remove(user);

        // One save: the account is deleted whole or not at all.
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
