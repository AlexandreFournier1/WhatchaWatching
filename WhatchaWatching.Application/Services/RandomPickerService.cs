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

    public async Task<MediaSummaryDto?> PickAsync(
        int userId,
        WatchListFilterDto? filter = null,
        MediaKey? exclude = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var showAdult = await UserSettingsService.ShowsAdultContentAsync(context, userId, cancellationToken)
            && (filter?.IncludeAdult ?? true);

        var matches = context.WatchListItems
            .AsNoTracking()
            .ForUser(userId)
            .ApplyAdultContent(showAdult)
            .ApplyFilter(filter);

        if (exclude is not null && await matches.CountAsync(cancellationToken) > 1)
        {
            var (excludedType, excludedId) = exclude;
            matches = matches.Where(i => i.Media.Type != excludedType || i.Media.ExternalId != excludedId);
        }

        var count = await matches.CountAsync(cancellationToken);
        if (count == 0)
        {
            return null;
        }

        // Count then skip: only the chosen title is loaded, not the whole list.
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

        return MediaMapper.ToSummary(media);
    }
}
