using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatchaWatching.Application.Exceptions;
using WhatchaWatching.Application.Mappers;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Middleware.Jikan;

namespace WhatchaWatching.Application.Services;

public class AnimeService(
    IJikanClient jikanClient,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    TimeProvider timeProvider,
    ILogger<AnimeService> logger) : IAnimeService
{
    // Scores and episode counts change slowly: a week-old copy saves a Jikan call.
    private static readonly TimeSpan SyncMaxAge = TimeSpan.FromDays(7);

    public async Task<PagedResultDto<AnimeSummaryDto>> SearchAsync(
        string query, int page, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new PagedResultDto<AnimeSummaryDto>([], 1, 1, 0);
        }

        try
        {
            var result = await jikanClient.SearchAnimeAsync(query, page, cancellationToken);

            return new PagedResultDto<AnimeSummaryDto>(
                result.Items.Select(AnimeMapper.ToSummary).ToList(),
                result.Page,
                result.TotalPages,
                result.TotalItems);
        }
        catch (JikanApiException ex)
        {
            throw new AnimeProviderUnavailableException(ex);
        }
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
            external = await jikanClient.GetAnimeAsync(externalId, cancellationToken);
        }
        catch (JikanApiException ex)
        {
            if (stored is null)
            {
                throw new AnimeProviderUnavailableException(ex);
            }

            logger.LogWarning(ex, "Jikan is unavailable, serving the stored copy of anime {ExternalId}.", externalId);
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
        await context.SaveChangesAsync(cancellationToken);

        return AnimeMapper.ToSummary(media);
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
