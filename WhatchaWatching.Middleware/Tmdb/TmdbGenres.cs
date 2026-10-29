using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Middleware.Tmdb;

/// <summary>
/// TMDB's search results only carry genre ids. Its genre lists are fixed and public, so they are kept here rather than
/// fetched on every search. The details of a title come with the real names.
/// </summary>
internal static class TmdbGenres
{
    private static readonly Dictionary<int, string> Movie = new()
    {
        [28] = "Action", [12] = "Adventure", [16] = "Animation", [35] = "Comedy", [80] = "Crime",
        [99] = "Documentary", [18] = "Drama", [10751] = "Family", [14] = "Fantasy", [36] = "History",
        [27] = "Horror", [10402] = "Music", [9648] = "Mystery", [10749] = "Romance", [878] = "Science Fiction",
        [10770] = "TV Movie", [53] = "Thriller", [10752] = "War", [37] = "Western"
    };

    private static readonly Dictionary<int, string> Tv = new()
    {
        [10759] = "Action & Adventure", [16] = "Animation", [35] = "Comedy", [80] = "Crime",
        [99] = "Documentary", [18] = "Drama", [10751] = "Family", [10762] = "Kids", [9648] = "Mystery",
        [10763] = "News", [10764] = "Reality", [10765] = "Sci-Fi & Fantasy", [10766] = "Soap",
        [10767] = "Talk", [10768] = "War & Politics", [37] = "Western"
    };

    /// <summary>The genres with these ids, leaving out the ones TMDB added since this list was written.</summary>
    public static IReadOnlyList<ExternalGenreDto> FromIds(MediaType type, IEnumerable<int>? ids)
    {
        var names = type == MediaType.Movie ? Movie : Tv;

        return (ids ?? [])
            .Where(names.ContainsKey)
            .Select(id => new ExternalGenreDto(id, names[id]))
            .ToList();
    }
}
