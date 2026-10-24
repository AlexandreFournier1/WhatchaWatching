using Microsoft.EntityFrameworkCore;
using WhatchaWatching.Application.Mappers;
using WhatchaWatching.Application.Queries;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Application.Services;

public class WatchListService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IAnimeService animeService) : IWatchListService
{
    public async Task<IReadOnlyList<AnimeSummaryDto>> GetAsync(
        int userId,
        WatchListFilterDto? filter = null,
        WatchListSort sort = WatchListSort.RecentlyAdded,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var items = await context.WatchListItems
            .AsNoTracking()
            .ForUser(userId)
            .ApplyFilter(filter)
            .ApplySort(sort)
            .Include(i => i.Media)
            .ThenInclude(m => m.MediaGenres)
            .ThenInclude(mg => mg.Genre)
            .ToListAsync(cancellationToken);

        return items.Select(i => AnimeMapper.ToSummary(i.Media)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetGenresAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.WatchListItems
            .AsNoTracking()
            .ForUser(userId)
            .SelectMany(i => i.Media.MediaGenres.Select(mg => mg.Genre.Name))
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<int>> GetListedIdsAsync(
        int userId, IReadOnlyCollection<int> externalIds, CancellationToken cancellationToken = default)
    {
        if (externalIds.Count == 0)
        {
            return new HashSet<int>();
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var ids = externalIds.ToList();

        var listed = await context.WatchListItems
            .AsNoTracking()
            .ForUser(userId)
            .Where(i => i.Media.Type == MediaType.Anime && ids.Contains(i.Media.ExternalId))
            .Select(i => i.Media.ExternalId)
            .ToListAsync(cancellationToken);

        return listed.ToHashSet();
    }

    public async Task<WatchListAddResult> AddAsync(int userId, int externalId, CancellationToken cancellationToken = default)
    {
        if (await IsListedAsync(userId, externalId, cancellationToken))
        {
            return WatchListAddResult.AlreadyInList;
        }

        // Makes sure the anime is stored locally before the list points to it.
        if (await animeService.GetAsync(externalId, cancellationToken) is null)
        {
            return WatchListAddResult.AnimeNotFound;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var mediaId = await context.Media
            .Where(m => m.Type == MediaType.Anime && m.ExternalId == externalId)
            .Select(m => m.Id)
            .SingleAsync(cancellationToken);

        context.WatchListItems.Add(new WatchListItemEntity { UserId = userId, MediaId = mediaId });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The same request arrived twice (double click): the unique index kept one.
            if (await IsListedAsync(userId, externalId, cancellationToken))
            {
                return WatchListAddResult.AlreadyInList;
            }

            throw;
        }

        return WatchListAddResult.Added;
    }

    public async Task<bool> RemoveAsync(int userId, int externalId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var item = await context.WatchListItems
            .ForUser(userId)
            .FirstOrDefaultAsync(i => i.Media.Type == MediaType.Anime && i.Media.ExternalId == externalId, cancellationToken);

        if (item is null)
        {
            return false;
        }

        context.WatchListItems.Remove(item);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> IsListedAsync(int userId, int externalId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.WatchListItems
            .AsNoTracking()
            .ForUser(userId)
            .AnyAsync(i => i.Media.Type == MediaType.Anime && i.Media.ExternalId == externalId, cancellationToken);
    }
}
