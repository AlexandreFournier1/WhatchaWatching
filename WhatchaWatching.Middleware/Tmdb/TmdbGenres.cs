using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Middleware.Tmdb;

/// <summary>
/// TMDB's search results only carry genre ids. Its genre lists are fixed and public, so they are kept here rather than
/// fetched on every search. The details of a title come with the real names.
/// </summary>
/// <remarks>
/// The names are also brought to those of MyAnimeList, so that one filter of the watchlist covers all three types:
/// "Science Fiction" is "Sci-Fi", and the pairs TMDB gives to series ("Action &amp; Adventure", "Sci-Fi &amp; Fantasy")
/// become the two genres movies already have.
/// </remarks>
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

    // A genre of series that is two of the movie genres (or one, with politics dropped): the ids are the movie ones.
    private static readonly Dictionary<int, (int Id, string Name)[]> Pairs = new()
    {
        [10759] = [(28, "Action"), (12, "Adventure")],
        [10765] = [(878, "Sci-Fi"), (14, "Fantasy")],
        [10768] = [(10752, "War")]
    };

    private static readonly Dictionary<int, string> Renamed = new() { [878] = "Sci-Fi" };

    /// <summary>The genres with these ids, leaving out the ones TMDB added since this list was written.</summary>
    public static IReadOnlyList<ExternalGenreDto> FromIds(MediaType type, IEnumerable<int>? ids)
    {
        var names = type == MediaType.Movie ? Movie : Tv;

        return Canonical((ids ?? []).Where(names.ContainsKey).Select(id => (id, names[id])));
    }

    /// <summary>The genres of a title as the site names them, each once.</summary>
    public static IReadOnlyList<ExternalGenreDto> Canonical(IEnumerable<(int Id, string Name)> genres) => genres
        .SelectMany(g => Pairs.TryGetValue(g.Id, out var parts)
            ? parts.Select(p => new ExternalGenreDto(p.Id, p.Name))
            : [new ExternalGenreDto(g.Id, Renamed.GetValueOrDefault(g.Id, g.Name))])
        .DistinctBy(g => g.ExternalId)
        .ToList();
}
