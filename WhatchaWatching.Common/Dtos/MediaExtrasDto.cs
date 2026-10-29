namespace WhatchaWatching.Common.Dtos;

/// <summary>What a title page shows beyond the title itself, and that is not stored: it is read when the page is opened.</summary>
/// <param name="Trailer">A video of the trailer, when there is one.</param>
/// <param name="Cast">The main actors, the first ones first.</param>
/// <param name="Seasons">The seasons of a TV series (without the specials), in order.</param>
/// <param name="Similar">Titles of the same type that people who liked this one also liked.</param>
public record MediaExtrasDto(
    TrailerDto? Trailer,
    IReadOnlyList<CastMemberDto> Cast,
    IReadOnlyList<SeasonDto> Seasons,
    IReadOnlyList<ExternalMediaDto> Similar)
{
    public static readonly MediaExtrasDto Empty = new(null, [], [], []);

    public bool IsEmpty => Trailer is null && Cast.Count == 0 && Seasons.Count == 0 && Similar.Count == 0;
}

/// <param name="Url">The address of the video on YouTube.</param>
public record TrailerDto(string Url, string? Name);

public record CastMemberDto(string Name, string? Character, string? PhotoUrl);

public record SeasonDto(int Number, string Name, int? EpisodeCount, int? Year);
