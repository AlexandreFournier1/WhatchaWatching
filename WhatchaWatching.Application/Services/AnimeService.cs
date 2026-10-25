using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatchaWatching.Application.Exceptions;
using WhatchaWatching.Application.Mappers;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Middleware;

namespace WhatchaWatching.Application.Services;

public class AnimeService(
    IAnimeProvider animeProvider,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    TimeProvider timeProvider,
    ILogger<AnimeService> logger) : IAnimeService
{
    // Scores and episode counts change slowly: a week-old copy saves a provider call.
    private static readonly TimeSpan SyncMaxAge = TimeSpan.FromDays(7);

    public async Task<AnimeSearchResultDto> SearchAsync(
        string query, int page, bool includeAdult = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new AnimeSearchResultDto(new PagedResultDto<AnimeSummaryDto>([], 1, 1, 0), IsLocalOnly: false);
        }

        try
        {
            var result = await animeProvider.SearchAnimeAsync(query, page, includeAdult, cancellationToken);

            // The provider was asked for safe anime already; this keeps an adult one out if it slips through.
            var results = new PagedResultDto<AnimeSummaryDto>(
                result.Items.Where(a => includeAdult || !a.IsAdult).Select(AnimeMapper.ToSummary).ToList(),
                result.Page,
                result.TotalPages,
                result.TotalItems);

            return new AnimeSearchResultDto(results, IsLocalOnly: false);
        }
        catch (ExternalApiException ex)
        {
            logger.LogWarning(ex, "No anime provider answered the search, looking in the stored anime.");
            return await SearchStoredAsync(query.Trim(), page, includeAdult, ex, cancellationToken);
        }
    }

    /// <summary>The last resort of a search: the anime this site already stored when they were opened or added.</summary>
    private async Task<AnimeSearchResultDto> SearchStoredAsync(
        string query, int page, bool includeAdult, ExternalApiException cause, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var matches = context.Media
            .AsNoTracking()
            .Where(m => m.Type == MediaType.Anime && m.CreatedByUserId == null && (includeAdult || !m.IsAdult) &&
                        (m.Title.Contains(query) || (m.OriginalTitle != null && m.OriginalTitle.Contains(query))));

        var total = await matches.CountAsync(cancellationToken);
        if (total == 0)
        {
            // Saying "no results" would be wrong: nobody could look.
            throw new AnimeProviderUnavailableException(cause);
        }

        var currentPage = Math.Max(page, 1);
        var stored = await matches
            .Include(m => m.MediaGenres)
            .ThenInclude(mg => mg.Genre)
            .OrderBy(m => m.Title)
            .Skip((currentPage - 1) * ExternalApiConstants.SEARCH_PAGE_SIZE)
            .Take(ExternalApiConstants.SEARCH_PAGE_SIZE)
            .ToListAsync(cancellationToken);

        var results = new PagedResultDto<AnimeSummaryDto>(
            stored.Select(AnimeMapper.ToSummary).ToList(),
            currentPage,
            (int)Math.Ceiling(total / (double)ExternalApiConstants.SEARCH_PAGE_SIZE),
            total);

        return new AnimeSearchResultDto(results, IsLocalOnly: true);
    }

    public async Task<AnimeSummaryDto?> GetAsync(
        int externalId, bool includeAdult = false, CancellationToken cancellationToken = default)
    {
        var anime = await GetCoreAsync(externalId, includeAdult, cancellationToken);

        // An adult anime does not exist for someone who did not turn adult content on.
        return anime is { IsAdult: true } && !includeAdult ? null : anime;
    }

    private async Task<AnimeSummaryDto?> GetCoreAsync(int externalId, bool includeAdult, CancellationToken cancellationToken)
    {
        // Provider ids are positive. A zero or negative id is an anime added by hand (never in the shared catalog) or junk:
        // asking a provider about it would only waste a call, and a failure would make the next requests skip Jikan.
        if (externalId <= 0)
        {
            return null;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var stored = await FindStoredAsync(context, externalId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (stored?.LastSyncedAt is { } syncedAt && now - syncedAt < SyncMaxAge)
        {
            return AnimeMapper.ToSummary(stored);
        }

        ExternalAnimeDto? external;
        try
        {
            external = await animeProvider.GetAnimeAsync(externalId, cancellationToken);
        }
        catch (ExternalApiException ex)
        {
            if (stored is null)
            {
                throw new AnimeProviderUnavailableException(ex);
            }

            logger.LogWarning(ex, "No anime provider answered, serving the stored copy of anime {ExternalId}.", externalId);
            return AnimeMapper.ToSummary(stored);
        }

        if (external is null)
        {
            return stored is null ? null : AnimeMapper.ToSummary(stored);
        }

        // Nothing is stored on behalf of someone who may not see it.
        if (external.IsAdult && !includeAdult)
        {
            return null;
        }

        try
        {
            return await StoreAsync(context, stored, external, now, cancellationToken);
        }
        catch (DbUpdateException) when (stored is null)
        {
            // Another request stored the same anime first: use its copy.
            await using var retryContext = await contextFactory.CreateDbContextAsync(cancellationToken);
            var concurrent = await FindStoredAsync(retryContext, externalId, cancellationToken);

            return concurrent is null ? throw new InvalidOperationException(
                $"Anime {externalId} could not be stored.") : AnimeMapper.ToSummary(concurrent);
        }
    }

    private static Task<MediaEntity?> FindStoredAsync(
        ApplicationDbContext context, int externalId, CancellationToken cancellationToken) =>
        context.Media
            .Include(m => m.MediaGenres)
            .ThenInclude(mg => mg.Genre)
            .Include(m => m.StreamingLinks)
            .AsSplitQuery()
            // An anime added by hand belongs to its user: the shared lookup never returns it, whatever the id.
            .FirstOrDefaultAsync(
                m => m.Type == MediaType.Anime && m.ExternalId == externalId && m.CreatedByUserId == null,
                cancellationToken);

    private static async Task<AnimeSummaryDto> StoreAsync(
        ApplicationDbContext context,
        MediaEntity? stored,
        ExternalAnimeDto external,
        DateTime syncedAt,
        CancellationToken cancellationToken)
    {
        var media = stored ?? new MediaEntity { Type = MediaType.Anime, ExternalId = external.ExternalId };
        if (stored is null)
        {
            context.Media.Add(media);
        }

        AnimeMapper.Apply(media, external, syncedAt);
        await MediaGenreSync.SyncAsync(context, media, external.Genres, cancellationToken);
        SyncStreamingLinks(context, media, external.StreamingLinks);
        await context.SaveChangesAsync(cancellationToken);

        return AnimeMapper.ToSummary(media);
    }

    /// <summary>
    /// Makes the anime carry exactly the given links. An empty list changes nothing: it is more often a provider that
    /// could not give its links than an anime that left every platform, and an old link is better than none.
    /// </summary>
    private static void SyncStreamingLinks(
        ApplicationDbContext context, MediaEntity media, IReadOnlyList<StreamingLinkDto> links)
    {
        if (links.Count == 0)
        {
            return;
        }

        var outdated = media.StreamingLinks.Where(stored => links.All(l => l.Url != stored.Url)).ToList();
        context.StreamingLinks.RemoveRange(outdated);

        foreach (var link in links)
        {
            var stored = media.StreamingLinks.FirstOrDefault(s => s.Url == link.Url);
            if (stored is null)
            {
                media.StreamingLinks.Add(new StreamingLinkEntity { Site = link.Site, Url = link.Url });
            }
            else
            {
                stored.Site = link.Site;
            }
        }
    }
}
