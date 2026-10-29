using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Common.Dtos;

/// <summary>Identifies a title: provider ids repeat from one type to another (TMDB id 1396 is a series and a movie).</summary>
public record MediaKey(MediaType Type, int ExternalId);
