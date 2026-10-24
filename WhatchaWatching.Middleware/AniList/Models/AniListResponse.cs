namespace WhatchaWatching.Middleware.AniList.Models;

internal record AniListResponse<T>(T? Data, List<AniListError>? Errors);
