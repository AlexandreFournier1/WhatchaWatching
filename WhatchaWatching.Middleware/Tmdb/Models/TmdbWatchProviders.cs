namespace WhatchaWatching.Middleware.Tmdb.Models;

/// <param name="Results">The platforms by country code ("FR", "US"...).</param>
internal record TmdbWatchProviders(Dictionary<string, TmdbRegionProviders>? Results);
