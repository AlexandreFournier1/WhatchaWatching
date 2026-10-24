namespace WhatchaWatching.Common.Dtos;

public record AnimeSummaryDto(
    int Id,
    string Title,
    string? ImageUrl,
    string? Synopsis,
    IReadOnlyList<string> Genres,
    int? EpisodeDurationMinutes,
    int? Episodes,
    int? Year,
    decimal? Score,
    string? Status);
