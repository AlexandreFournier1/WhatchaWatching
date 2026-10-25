using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Common.Constants;

/// <summary>
/// The genres the app knows, with their MyAnimeList ids (the ones Jikan returns). AniList names its genres without ids,
/// and an anime added by hand picks its genres from here, so that every source ends up on the same stored genres.
/// Adult genres are left out: the providers are asked for safe-for-work anime only.
/// </summary>
public static class GenreCatalog
{
    public static IReadOnlyList<ExternalGenreDto> All { get; } =
    [
        new(1, "Action"),
        new(2, "Adventure"),
        new(4, "Comedy"),
        new(8, "Drama"),
        new(9, "Ecchi"),
        new(10, "Fantasy"),
        new(14, "Horror"),
        new(66, "Mahou Shoujo"),
        new(18, "Mecha"),
        new(19, "Music"),
        new(7, "Mystery"),
        new(40, "Psychological"),
        new(22, "Romance"),
        new(24, "Sci-Fi"),
        new(36, "Slice of Life"),
        new(30, "Sports"),
        new(37, "Supernatural"),
        new(41, "Suspense")
    ];

    /// <summary>The genre with this name (ignoring case), or null.</summary>
    public static ExternalGenreDto? Find(string name) =>
        All.FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
}
