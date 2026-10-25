namespace WhatchaWatching.Middleware.Jikan.Models;

internal record JikanAnime(
    int MalId,
    string? Title,
    string? TitleEnglish,
    string? TitleJapanese,
    string? Synopsis,
    int? Episodes,
    string? Status,
    string? Duration,
    string? Rating,
    decimal? Score,
    string? Season,
    int? Year,
    JikanImages? Images,
    JikanAired? Aired,
    List<JikanGenre>? Genres,
    List<JikanGenre>? ExplicitGenres);
