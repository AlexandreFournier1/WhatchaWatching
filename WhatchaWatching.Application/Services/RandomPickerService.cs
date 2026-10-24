using Microsoft.EntityFrameworkCore;
using WhatchaWatching.Application.Mappers;
using WhatchaWatching.Application.Queries;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Infrastructure.Data;

namespace WhatchaWatching.Application.Services;

public class RandomPickerService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    Random? random = null) : IRandomPickerService
{
    private readonly Random _random = random ?? Random.Shared;

    public async Task<AnimeSummaryDto?> PickAsync(
        int userId,
        WatchListFilterDto? filter = null,
        int? excludeExternalId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var matches = context.WatchListItems
            .AsNoTracking()
            .ForUser(userId)
            .ApplyFilter(filter);

        if (excludeExternalId is { } excluded && await matches.CountAsync(cancellationToken) > 1)
        {
            matches = matches.Where(i => i.Media.ExternalId != excluded);
        }

        var count = await matches.CountAsync(cancellationToken);
        if (count == 0)
        {
            return null;
        }

        // Count then skip: only the chosen anime is loaded, not the whole list.
        var mediaId = await matches
            .OrderBy(i => i.Id)
            .Skip(_random.Next(count))
            .Select(i => i.MediaId)
            .FirstAsync(cancellationToken);

        var media = await context.Media
            .AsNoTracking()
            .Include(m => m.MediaGenres)
            .ThenInclude(mg => mg.Genre)
            .SingleAsync(m => m.Id == mediaId, cancellationToken);

        return AnimeMapper.ToSummary(media);
    }
}
