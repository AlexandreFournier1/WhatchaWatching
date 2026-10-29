using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Common.Dtos;

/// <summary>A movie or TV series as TMDB describes it. Anything the provider omits is null.</summary>
public record ExternalMediaDto(
    MediaType Type,
    int ExternalId,
    string Title,
    string? OriginalTitle,
    string? Synopsis,
    string? ImageUrl,
    int? EpisodeCount,
    int? SeasonCount,
    int? EpisodeDurationMinutes,
    MediaStatus? Status,
    decimal? Score,
    int? Year,
    DateTime? ReleaseDate,
    IReadOnlyList<ExternalGenreDto> Genres)
{
    /// <summary>Streaming platforms offering the title. Empty when unknown.</summary>
    public IReadOnlyList<StreamingLinkDto> StreamingLinks { get; init; } = [];

    /// <summary>True for adult titles, which are only shown to users who turned adult content on.</summary>
    public bool IsAdult { get; init; }
}
