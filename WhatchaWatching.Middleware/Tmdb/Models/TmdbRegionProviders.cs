namespace WhatchaWatching.Middleware.Tmdb.Models;

/// <param name="Link">TMDB gives one page per country (powered by JustWatch), not one address per platform.</param>
/// <param name="Flatrate">The platforms where the title is included in the subscription.</param>
internal record TmdbRegionProviders(string? Link, List<TmdbProvider>? Flatrate);
