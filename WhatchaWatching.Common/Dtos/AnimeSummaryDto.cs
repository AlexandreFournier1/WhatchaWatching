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
    /// <summary>True for an anime a user added by hand: it is private to that user and has no provider data.</summary>
    public bool IsCustom { get; init; }

    /// <summary>Streaming platforms offering the anime. Only filled for the details of one anime.</summary>
    public IReadOnlyList<StreamingLinkDto> StreamingLinks { get; init; } = [];

    /// <summary>True for adult anime (Hentai and the like), which are only shown to users who turned adult content on.</summary>
    public bool IsAdult { get; init; }
}
