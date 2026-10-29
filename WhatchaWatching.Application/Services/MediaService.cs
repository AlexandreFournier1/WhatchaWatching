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
using WhatchaWatching.Middleware.Tmdb;

namespace WhatchaWatching.Application.Services;

public class MediaService(
    IAnimeProvider animeProvider,
    ITmdbClient tmdbClient,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    TimeProvider timeProvider,
    ILogger<MediaService> logger) : IMediaService
{
    // Scores and episode counts change slowly: a week-old copy saves a provider call.
    private static readonly TimeSpan SyncMaxAge = TimeSpan.FromDays(7);

    private static void EnsureSupported(MediaType type)
    {
        if (type is not (MediaType.Anime or MediaType.Movie or MediaType.TVShow))
        {
            throw new NotSupportedException($"{type} titles are not supported.");
        }
    }

    // Anime come from Jikan (then AniList); movies and TV series only from TMDB.
    private Task<PagedResultDto<ExternalMediaDto>> SearchProviderAsync(
        MediaType type, string query, int page, bool includeAdult, CancellationToken cancellationToken) =>
        type == MediaType.Anime
            ? animeProvider.SearchAnimeAsync(query, page, includeAdult, cancellationToken)
            : tmdbClient.SearchAsync(type, query, page, includeAdult, cancellationToken);

    private Task<ExternalMediaDto?> GetFromProviderAsync(MediaType type, int externalId, CancellationToken cancellationToken) =>
        type == MediaType.Anime
            ? animeProvider.GetAnimeAsync(externalId, cancellationToken)
            : tmdbClient.GetAsync(type, externalId, cancellationToken);

    public async Task<MediaSearchResultDto> SearchAsync(
        MediaType type, string query, int page, bool includeAdult = false, CancellationToken cancellationToken = default)
    {
        EnsureSupported(type);

        if (string.IsNullOrWhiteSpace(query))
        {
            return new MediaSearchResultDto(new PagedResultDto<MediaSummaryDto>([], 1, 1, 0), IsLocalOnly: false);
        }

        try
        {
            var result = await SearchProviderAsync(type, query, page, includeAdult, cancellationToken);

            // The provider was asked for safe titles already; this keeps an adult one out if it slips through.
            var results = new PagedResultDto<MediaSummaryDto>(
                result.Items.Where(a => includeAdult || !a.IsAdult).Select(MediaMapper.ToSummary).ToList(),
                result.Page,
                result.TotalPages,
                result.TotalItems);

            return new MediaSearchResultDto(results, IsLocalOnly: false);
        }
        catch (ExternalApiException ex)
        {
            logger.LogWarning(ex, "No {Type} provider answered the search, looking in the stored titles.", type);
            return await SearchStoredAsync(type, query.Trim(), page, includeAdult, ex, cancellationToken);
        }
    }

    /// <summary>The last resort of a search: the titles this site already stored when they were opened or added.</summary>
    private async Task<MediaSearchResultDto> SearchStoredAsync(
        MediaType type, string query, int page, bool includeAdult, ExternalApiException cause, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var matches = context.Media
            .AsNoTracking()
            .Where(m => m.Type == type && m.CreatedByUserId == null && (includeAdult || !m.IsAdult) &&
                        (m.Title.Contains(query) || (m.OriginalTitle != null && m.OriginalTitle.Contains(query))));

        var total = await matches.CountAsync(cancellationToken);
        if (total == 0)
        {
            // Saying "no results" would be wrong: nobody could look.
            throw new MediaProviderUnavailableException(cause);
        }

        var currentPage = Math.Max(page, 1);
        var stored = await matches
            .Include(m => m.MediaGenres)
            .ThenInclude(mg => mg.Genre)
            .OrderBy(m => m.Title)
            .Skip((currentPage - 1) * ExternalApiConstants.SEARCH_PAGE_SIZE)
            .Take(ExternalApiConstants.SEARCH_PAGE_SIZE)
            .ToListAsync(cancellationToken);

        var results = new PagedResultDto<MediaSummaryDto>(
            stored.Select(MediaMapper.ToSummary).ToList(),
            currentPage,
            (int)Math.Ceiling(total / (double)ExternalApiConstants.SEARCH_PAGE_SIZE),
            total);

        return new MediaSearchResultDto(results, IsLocalOnly: true);
    }

    public async Task<MediaSummaryDto?> GetAsync(
        MediaType type, int externalId, bool includeAdult = false, CancellationToken cancellationToken = default)
    {
        EnsureSupported(type);

        var anime = await GetCoreAsync(type, externalId, includeAdult, cancellationToken);

        // An adult title does not exist for someone who did not turn adult content on.
        return anime is { IsAdult: true } && !includeAdult ? null : anime;
    }

    private async Task<MediaSummaryDto?> GetCoreAsync(
        MediaType type, int externalId, bool includeAdult, CancellationToken cancellationToken)
    {
        // Provider ids are positive. A zero or negative id is an anime added by hand (never in the shared catalog) or junk:
        // asking a provider about it would only waste a call, and a failure would make the next requests skip Jikan.
        if (externalId <= 0)
        {
            return null;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var stored = await FindStoredAsync(context, type, externalId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (stored?.LastSyncedAt is { } syncedAt && now - syncedAt < SyncMaxAge)
        {
            return MediaMapper.ToSummary(stored);
        }

        ExternalMediaDto? external;
        try
        {
            external = await GetFromProviderAsync(type, externalId, cancellationToken);
        }
        catch (ExternalApiException ex)
        {
            if (stored is null)
            {
                throw new MediaProviderUnavailableException(ex);
            }

            logger.LogWarning(ex, "No provider answered, serving the stored copy of {Type} {ExternalId}.", type, externalId);
            return MediaMapper.ToSummary(stored);
        }

        if (external is null)
        {
            return stored is null ? null : MediaMapper.ToSummary(stored);
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
            var concurrent = await FindStoredAsync(retryContext, type, externalId, cancellationToken);

            return concurrent is null ? throw new InvalidOperationException(
                $"{type} {externalId} could not be stored.") : MediaMapper.ToSummary(concurrent);
        }
    }

    private static Task<MediaEntity?> FindStoredAsync(
        ApplicationDbContext context, MediaType type, int externalId, CancellationToken cancellationToken) =>
        context.Media
            .Include(m => m.MediaGenres)
            .ThenInclude(mg => mg.Genre)
            .Include(m => m.StreamingLinks)
            .AsSplitQuery()
            // An anime added by hand belongs to its user: the shared lookup never returns it, whatever the id.
            .FirstOrDefaultAsync(
                m => m.Type == type && m.ExternalId == externalId && m.CreatedByUserId == null,
                cancellationToken);

    private static async Task<MediaSummaryDto> StoreAsync(
        ApplicationDbContext context,
        MediaEntity? stored,
        ExternalMediaDto external,
        DateTime syncedAt,
        CancellationToken cancellationToken)
    {
        var media = stored ?? new MediaEntity { Type = external.Type, ExternalId = external.ExternalId };
        if (stored is null)
        {
            context.Media.Add(media);
        }

        MediaMapper.Apply(media, external, syncedAt);
        await MediaGenreSync.SyncAsync(context, media, external.Genres, cancellationToken);
        SyncStreamingLinks(context, media, external.StreamingLinks);
        await context.SaveChangesAsync(cancellationToken);

        return MediaMapper.ToSummary(media);
    }

    /// <summary>
    /// Makes the title carry exactly the given links. An empty list changes nothing: it is more often a provider that
    /// could not give its links than an anime that left every platform, and an old link is better than none.
    /// </summary>
    private static void SyncStreamingLinks(
        ApplicationDbContext context, MediaEntity media, IReadOnlyList<StreamingLinkDto> links)
    {
        if (links.Count == 0)
        {
            return;
        }

        // A link is the pair (platform, address): TMDB gives every platform of a title the same address.
        var outdated = media.StreamingLinks
            .Where(stored => !links.Any(l => l.Site == stored.Site && l.Url == stored.Url))
            .ToList();
        context.StreamingLinks.RemoveRange(outdated);

        foreach (var link in links)
        {
            if (!media.StreamingLinks.Any(s => s.Site == link.Site && s.Url == link.Url))
            {
                media.StreamingLinks.Add(new StreamingLinkEntity { Site = link.Site, Url = link.Url });
            }
        }
    }
}
