namespace WhatchaWatching.Middleware.Tmdb.Models;

/// <param name="Key">The id of the video on its site.</param>
/// <param name="Site">"YouTube", "Vimeo"...</param>
/// <param name="Type">"Trailer", "Teaser", "Clip"...</param>
internal record TmdbVideo(string? Key, string? Site, string? Type, bool? Official, string? Name);
