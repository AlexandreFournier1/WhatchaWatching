namespace WhatchaWatching.Middleware.Tmdb.Models;

internal record TmdbSearchResponse(int Page, int TotalPages, int TotalResults, List<TmdbTitle>? Results);
