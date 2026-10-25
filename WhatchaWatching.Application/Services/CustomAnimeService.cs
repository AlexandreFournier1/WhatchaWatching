using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using WhatchaWatching.Application.Mappers;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Application.Services;

public class CustomAnimeService(IDbContextFactory<ApplicationDbContext> contextFactory) : ICustomAnimeService
{
    private const int MaxAttempts = 3;

    public async Task<AnimeSummaryDto> CreateAsync(
        int userId, CustomAnimeDto input, CancellationToken cancellationToken = default)
    {
        var genres = Validate(input);

        for (var attempt = 1; ; attempt++)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var owned = await context.Media.CountAsync(m => m.CreatedByUserId == userId, cancellationToken);
            if (owned >= CustomAnimeConstants.MAX_PER_USER)
            {
                throw new ValidationException(
                    $"You can add up to {CustomAnimeConstants.MAX_PER_USER} anime by hand. Remove one to add another.");
            }

            // The providers' ids are positive: counting down from -1 never meets one of them.
            var lowest = await context.Media
                .Where(m => m.ExternalId < 0)
                .MinAsync(m => (int?)m.ExternalId, cancellationToken) ?? 0;

            var media = new MediaEntity
            {
                Type = MediaType.Anime,
                ExternalId = lowest - 1,
                CreatedByUserId = userId,
                Title = input.Title.Trim(),
                OriginalTitle = Clean(input.OriginalTitle),
                Synopsis = Clean(input.Synopsis),
                ImageUrl = Clean(input.ImageUrl),
                EpisodeCount = input.EpisodeCount,
                EpisodeDurationMinutes = input.EpisodeDurationMinutes,
                Year = input.Year,
                Status = input.Status
            };
            media.WatchListItems.Add(new WatchListItemEntity { UserId = userId });
            context.Media.Add(media);
            await MediaGenreSync.SyncAsync(context, media, genres, cancellationToken);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return AnimeMapper.ToSummary(media);
            }
            catch (DbUpdateException) when (attempt < MaxAttempts)
            {
                // Another request took the same id or created the same genre a moment ago: start again with fresh data.
            }
        }
    }

    public async Task<AnimeSummaryDto?> GetAsync(int userId, int externalId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var media = await context.Media
            .AsNoTracking()
            .Include(m => m.MediaGenres)
            .ThenInclude(mg => mg.Genre)
            .FirstOrDefaultAsync(
                m => m.Type == MediaType.Anime && m.ExternalId == externalId && m.CreatedByUserId == userId,
                cancellationToken);

        return media is null ? null : AnimeMapper.ToSummary(media);
    }

    // The form validates too, but the service cannot rely on it: whatever calls it gets the same rules.
    private static List<ExternalGenreDto> Validate(CustomAnimeDto input)
    {
        var problems = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), problems, validateAllProperties: true))
        {
            throw new ValidationException(problems[0].ErrorMessage);
        }

        var genres = new List<ExternalGenreDto>();
        foreach (var name in input.Genres.Select(g => g.Trim()).Where(g => g.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            genres.Add(GenreCatalog.Find(name) ?? throw new ValidationException($"Unknown genre '{name}'."));
        }

        return genres;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
