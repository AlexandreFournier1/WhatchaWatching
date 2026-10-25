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
        string query, int page, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new AnimeSearchResultDto(new PagedResultDto<AnimeSummaryDto>([], 1, 1, 0), IsLocalOnly: false);
        }

        try
        {
            var result = await animeProvider.SearchAnimeAsync(query, page, cancellationToken);
            var results = new PagedResultDto<AnimeSummaryDto>(
                result.Items.Select(AnimeMapper.ToSummary).ToList(),
                result.Page,
                result.TotalPages,
                result.TotalItems);

            return new AnimeSearchResultDto(results, IsLocalOnly: false);
        }
        catch (ExternalApiException ex)
        {
            logger.LogWarning(ex, "No anime provider answered the search, looking in the stored anime.");
            return await SearchStoredAsync(query.Trim(), page, ex, cancellationToken);
        }
    }

    /// <summary>The last resort of a search: the anime this site already stored when they were opened or added.</summary>
    private async Task<AnimeSearchResultDto> SearchStoredAsync(
        string query, int page, ExternalApiException cause, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var matches = context.Media
            .AsNoTracking()
            .Where(m => m.Type == MediaType.Anime &&
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

    public async Task<AnimeSummaryDto?> GetAsync(int externalId, CancellationToken cancellationToken = default)
    {
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
            .FirstOrDefaultAsync(m => m.Type == MediaType.Anime && m.ExternalId == externalId, cancellationToken);

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
        await SyncGenresAsync(context, media, external.Genres, cancellationToken);
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

    /// <summary>Makes the anime link exactly the given genres, reusing the genres already stored.</summary>
    private static async Task SyncGenresAsync(
        ApplicationDbContext context,
        MediaEntity media,
        IReadOnlyList<ExternalGenreDto> genres,
        CancellationToken cancellationToken)
    {
        var wantedIds = genres.Select(g => g.ExternalId).ToList();
        var known = await context.Genres
            .Where(g => wantedIds.Contains(g.ExternalId))
            .ToDictionaryAsync(g => g.ExternalId, cancellationToken);

        foreach (var genre in genres.Where(g => !known.ContainsKey(g.ExternalId)))
        {
            var created = new GenreEntity { ExternalId = genre.ExternalId, Name = genre.Name };
            context.Genres.Add(created);
            known[genre.ExternalId] = created;
        }

        var outdated = media.MediaGenres.Where(mg => !wantedIds.Contains(mg.Genre.ExternalId)).ToList();
        context.MediaGenres.RemoveRange(outdated);

        var linked = media.MediaGenres.Except(outdated).Select(mg => mg.Genre.ExternalId).ToHashSet();
        foreach (var genreId in wantedIds.Where(id => !linked.Contains(id)).Distinct())
        {
            media.MediaGenres.Add(new MediaGenreEntity { Media = media, Genre = known[genreId] });
        }
    }
}
