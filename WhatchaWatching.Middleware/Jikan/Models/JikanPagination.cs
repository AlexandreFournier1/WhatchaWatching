namespace WhatchaWatching.Middleware.Jikan.Models;

internal record JikanPagination(int? LastVisiblePage, int? CurrentPage, JikanPaginationItems? Items);
