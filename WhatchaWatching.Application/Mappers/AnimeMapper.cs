using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Application.Mappers;

internal static class AnimeMapper
{
    public static AnimeSummaryDto ToSummary(MediaEntity media) => new(
        media.ExternalId,
        media.Title,
        media.ImageUrl,
        media.Synopsis,
        media.MediaGenres.Select(mg => mg.Genre.Name).Order().ToList(),
        media.EpisodeDurationMinutes,
        media.EpisodeCount,
        media.Year,
        media.Score,
        media.Status.ToString())
    {
        StreamingLinks = media.StreamingLinks.OrderBy(l => l.Site).Select(l => new StreamingLinkDto(l.Site, l.Url)).ToList()
    };

    public static AnimeSummaryDto ToSummary(ExternalAnimeDto anime) => new(
        anime.ExternalId,
        anime.Title,
        anime.ImageUrl,
        anime.Synopsis,
        anime.Genres.Select(g => g.Name).Order().ToList(),
        anime.EpisodeDurationMinutes,
        anime.EpisodeCount,
        anime.Year,
        anime.Score,
        anime.Status?.ToString())
    {
        StreamingLinks = anime.StreamingLinks
    };

    /// <summary>Copies what Jikan says onto the stored anime. Genres are handled by the caller.</summary>
    public static void Apply(MediaEntity media, ExternalAnimeDto anime, DateTime syncedAt)
    {
        media.Title = Truncate(anime.Title, ConfigurationConstants.LARGE_SIZE) ?? string.Empty;
        media.OriginalTitle = Truncate(anime.OriginalTitle, ConfigurationConstants.LARGE_SIZE);
        media.Synopsis = Truncate(anime.Synopsis, ConfigurationConstants.XXXL_SIZE);
        media.ImageUrl = Truncate(anime.ImageUrl, ConfigurationConstants.LARGE_SIZE);
        media.ReleaseDate = anime.ReleaseDate;
        media.EpisodeCount = anime.EpisodeCount;
        media.EpisodeDurationMinutes = anime.EpisodeDurationMinutes;
        // Jikan sometimes omits the status; an entry it cannot describe is most likely an old one.
        media.Status = anime.Status ?? MediaStatus.Finished;
        media.Score = anime.Score;
        media.Year = anime.Year;
        media.Season = Truncate(anime.Season, ConfigurationConstants.SMALL_SIZE);
        media.Rating = Truncate(anime.Rating, ConfigurationConstants.SMALL_SIZE);
        media.LastSyncedAt = syncedAt;
    }

    // A value longer than its column would make the whole save fail.
    public static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
