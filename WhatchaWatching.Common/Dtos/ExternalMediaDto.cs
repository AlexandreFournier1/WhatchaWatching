using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Common.Dtos;

/// <summary>An anime, movie or TV series as an external provider describes it. Anything the provider omits is null.</summary>
public record ExternalMediaDto(
    MediaType Type,
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
    IReadOnlyList<ExternalGenreDto> Genres)
{
    /// <summary>Number of seasons of a TV series. Null for anime and movies.</summary>
    public int? SeasonCount { get; init; }

    /// <summary>Streaming platforms offering the title. Empty when unknown.</summary>
    public IReadOnlyList<StreamingLinkDto> StreamingLinks { get; init; } = [];

    /// <summary>True for adult titles (Hentai and the like), which are only shown to users who turned adult content on.</summary>
    public bool IsAdult { get; init; }
}
