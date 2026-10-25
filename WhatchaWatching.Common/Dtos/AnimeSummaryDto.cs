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
    string? Status)
{
    /// <summary>Streaming platforms offering the anime. Only filled for the details of one anime.</summary>
    public IReadOnlyList<StreamingLinkDto> StreamingLinks { get; init; } = [];
}
