using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Services;

/// <summary>How the three types of title appear in addresses and on screen.</summary>
public static class MediaRoutes
{
    public static IReadOnlyList<MediaType> Types { get; } = [MediaType.Anime, MediaType.Movie, MediaType.TVShow];

    /// <summary>The word in the address: <c>/movie/603</c>, <c>?type=tv</c>.</summary>
    public static string Slug(MediaType type) => type switch
    {
        MediaType.Anime => "anime",
        MediaType.Movie => "movie",
        MediaType.TVShow => "tv",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "This type has no page.")
    };

    /// <summary>The type of a slug, or null when it is missing or unknown.</summary>
    public static MediaType? FromSlug(string? slug) => Types.Cast<MediaType?>().FirstOrDefault(
        type => string.Equals(Slug(type!.Value), slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>Relative to the site root, like the other links of the site.</summary>
    public static string DetailUrl(MediaType type, int id) => $"{Slug(type)}/{id}";

    public static string SearchUrl(MediaType type) => $"search?type={Slug(type)}";

    /// <summary>One title: "anime", "movie", "series".</summary>
    public static string Singular(MediaType type) => type switch
    {
        MediaType.Movie => "movie",
        MediaType.TVShow => "series",
        _ => "anime"
    };

    /// <summary>The name of the type as a tab or a badge.</summary>
    public static string Label(MediaType type) => type switch
    {
        MediaType.Movie => "Movies",
        MediaType.TVShow => "TV series",
        _ => "Anime"
    };

    /// <summary>The name of one title's type, for a badge on a card.</summary>
    public static string Badge(MediaType type) => type switch
    {
        MediaType.Movie => "Movie",
        MediaType.TVShow => "TV series",
        _ => "Anime"
    };
}
