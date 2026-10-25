namespace WhatchaWatching.Middleware.AniList.Models;

internal record AniListMedia(
    int? IdMal,
    AniListTitle? Title,
    string? Description,
    AniListImage? CoverImage,
    int? Episodes,
    int? Duration,
    string? Status,
    int? AverageScore,
    int? SeasonYear,
    string? Season,
    AniListDate? StartDate,
    List<string>? Genres,
    List<AniListExternalLink>? ExternalLinks);
