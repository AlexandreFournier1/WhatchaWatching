using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Common.Dtos;

/// <summary>An anime as the external provider (Jikan) describes it. Anything the provider omits is null.</summary>
public record ExternalAnimeDto(
    int ExternalId,
    string Title,
    string? OriginalTitle,
    string? Synopsis,
    string? ImageUrl,
    int? EpisodeCount,
    int? EpisodeDurationMinutes,
    MediaStatus? Status,
    decimal? Score,
    int? Year,
    string? Season,
    string? Rating,
    DateTime? ReleaseDate,
    IReadOnlyList<ExternalGenreDto> Genres);
