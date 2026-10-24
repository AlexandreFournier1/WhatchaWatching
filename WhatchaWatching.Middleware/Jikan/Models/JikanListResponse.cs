namespace WhatchaWatching.Middleware.Jikan.Models;

internal record JikanListResponse<T>(List<T>? Data, JikanPagination? Pagination);
