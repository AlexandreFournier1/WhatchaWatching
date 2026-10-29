using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Common.Dtos;

/// <summary>An anime, movie or TV series. <paramref name="Id"/> is its provider id, which is only unique per type.</summary>
public record MediaSummaryDto(
    MediaType Type,
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
    /// <summary>Number of seasons of a TV series. Null for anime and movies.</summary>
    public int? Seasons { get; init; }

    /// <summary>True for an anime a user added by hand: it is private to that user and has no provider data.</summary>
    public bool IsCustom { get; init; }

    /// <summary>Streaming platforms offering the title. Only filled for the details of one title.</summary>
    public IReadOnlyList<StreamingLinkDto> StreamingLinks { get; init; } = [];

    /// <summary>True for adult titles (Hentai and the like), which are only shown to users who turned adult content on.</summary>
    public bool IsAdult { get; init; }
}
