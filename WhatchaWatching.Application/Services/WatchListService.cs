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
    IMediaService mediaService) : IWatchListService
{
    public async Task<IReadOnlyList<MediaSummaryDto>> GetAsync(
        int userId,
        WatchListFilterDto? filter = null,
        WatchListSort sort = WatchListSort.RecentlyAdded,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var showAdult = await UserSettingsService.ShowsAdultContentAsync(context, userId, cancellationToken)
            && (filter?.IncludeAdult ?? true);

        filter = await WithRegionAsync(context, userId, filter, cancellationToken);

        var items = await context.WatchListItems
            .AsNoTracking()
            .ForUser(userId)
            .ApplyAdultContent(showAdult)
            .ApplyFilter(filter)
            .ApplySort(sort)
            .Include(i => i.Media)
            .ThenInclude(m => m.MediaGenres)
            .ThenInclude(mg => mg.Genre)
            .ToListAsync(cancellationToken);

        return items.Select(i => MediaMapper.ToSummary(i.Media)).ToList();
    }

    /// <summary>The filter with the country of the user, which the offers of a platform depend on.</summary>
    internal static async Task<WatchListFilterDto?> WithRegionAsync(
        ApplicationDbContext context, int userId, WatchListFilterDto? filter, CancellationToken cancellationToken) =>
        filter is { Platforms.Count: > 0 }
            ? filter with { Region = await UserSettingsService.CountryAsync(context, userId, cancellationToken) }
            : filter;

    public async Task<IReadOnlyList<string>> GetPlatformsAsync(
        int userId, MediaType? type = null, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var showAdult = await UserSettingsService.ShowsAdultContentAsync(context, userId, cancellationToken);
        var region = await UserSettingsService.CountryAsync(context, userId, cancellationToken);

        var items = context.WatchListItems
            .AsNoTracking()
            .ForUser(userId)
            .ApplyAdultContent(showAdult);
        if (type is { } only)
        {
            items = items.Where(i => i.Media.Type == only);
        }

        return await items
            .SelectMany(i => i.Media.StreamingLinks)
            .Where(l => l.Region == null || l.Region == region)
            .Select(l => l.Site)
            .Distinct()
            .OrderBy(site => site)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetGenresAsync(
        int userId, MediaType? type = null, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var showAdult = await UserSettingsService.ShowsAdultContentAsync(context, userId, cancellationToken);

        var items = context.WatchListItems
            .AsNoTracking()
            .ForUser(userId)
            .ApplyAdultContent(showAdult);
        if (type is { } only)
        {
            items = items.Where(i => i.Media.Type == only);
        }

        return await items
            .SelectMany(i => i.Media.MediaGenres.Select(mg => mg.Genre.Name))
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<int>> GetListedIdsAsync(
        int userId, MediaType type, IReadOnlyCollection<int> externalIds, CancellationToken cancellationToken = default)
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
            .Where(i => i.Media.Type == type && ids.Contains(i.Media.ExternalId))
            .Select(i => i.Media.ExternalId)
            .ToListAsync(cancellationToken);

        return listed.ToHashSet();
    }

    public async Task<WatchListAddResult> AddAsync(
        int userId, MediaType type, int externalId, CancellationToken cancellationToken = default)
    {
        if (await IsListedAsync(userId, type, externalId, cancellationToken))
        {
            return WatchListAddResult.AlreadyInList;
        }

        // Makes sure the title is stored locally before the list points to it.
        bool showAdult;
        await using (var settingsContext = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            showAdult = await UserSettingsService.ShowsAdultContentAsync(settingsContext, userId, cancellationToken);
        }

        if (await mediaService.GetAsync(type, externalId, showAdult, cancellationToken) is null)
        {
            return WatchListAddResult.NotFound;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var mediaId = await context.Media
            .Where(m => m.Type == type && m.ExternalId == externalId)
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
            if (await IsListedAsync(userId, type, externalId, cancellationToken))
            {
                return WatchListAddResult.AlreadyInList;
            }

            throw;
        }

        return WatchListAddResult.Added;
    }

    public async Task<bool> RemoveAsync(
        int userId, MediaType type, int externalId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var item = await context.WatchListItems
            .ForUser(userId)
            .FirstOrDefaultAsync(i => i.Media.Type == type && i.Media.ExternalId == externalId, cancellationToken);

        if (item is null)
        {
            return false;
        }

        context.WatchListItems.Remove(item);

        // An anime the user added by hand exists only for their list: removing it from the list deletes it, so that no
        // invisible leftover stays in the database. A shared anime stays, other users may list it.
        var custom = await context.Media
            .Include(m => m.MediaGenres)
            .Include(m => m.StreamingLinks)
            .FirstOrDefaultAsync(m => m.Id == item.MediaId && m.CreatedByUserId == userId, cancellationToken);
        if (custom is not null)
        {
            context.Media.Remove(custom);
        }

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> IsListedAsync(int userId, MediaType type, int externalId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.WatchListItems
            .AsNoTracking()
            .ForUser(userId)
            .AnyAsync(i => i.Media.Type == type && i.Media.ExternalId == externalId, cancellationToken);
    }
}
