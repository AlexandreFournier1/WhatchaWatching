using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Services;

/// <summary>How the three types of title appear in addresses and on screen.</summary>
public static class MediaRoutes
{
    public static IReadOnlyList<MediaType> Types { get; } = [MediaType.Anime, MediaType.Movie, MediaType.TVShow];

    /// <summary>The type shown when the address does not say.</summary>
    public const MediaType DefaultType = MediaType.Anime;

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

    /// <summary>
    /// The type an address is about, from its path relative to the site: <c>movie/603</c> is a movie, <c>search?type=tv</c>
    /// is about series, and anything else is the default type. This is what the type switch of the header shows.
    /// </summary>
    public static MediaType FromAddress(string relativeAddress)
    {
        var path = PathOf(relativeAddress);

        if (IsTitlePage(path))
        {
            return FromSlug(path.Split('/')[0])!.Value;
        }

        var query = relativeAddress.Split('#')[0].Split('?', 2);
        var type = query.Length == 2 ? QueryHelpers.ParseQuery(query[1]).GetValueOrDefault("type").ToString() : null;

        return FromSlug(type) ?? DefaultType;
    }

    /// <summary>The path of an address relative to the site, without the query, the fragment or the outer slashes.</summary>
    public static string PathOf(string relativeAddress) => relativeAddress.Split('#', '?')[0].Trim('/');

    /// <summary>A page about one title: <c>anime/5114</c>, <c>movie/603</c>, <c>tv/1396</c>.</summary>
    public static bool IsTitlePage(string path)
    {
        var parts = path.Split('/');

        return parts.Length == 2 && FromSlug(parts[0]) is not null &&
               int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }

    /// <summary>The pages that follow the type switch of the header: the others (account, login...) have nothing to switch.</summary>
    public static bool FollowsType(string path) => path is "" or "search" or "my-list" || IsTitlePage(path);

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

    /// <summary>The name of the type as a tab or a title.</summary>
    public static string Label(MediaType type) => type switch
    {
        MediaType.Movie => "Movies",
        MediaType.TVShow => "TV series",
        _ => "Anime"
    };

    /// <summary>The name of one title's type.</summary>
    public static string Badge(MediaType type) => type switch
    {
        MediaType.Movie => "Movie",
        MediaType.TVShow => "TV series",
        _ => "Anime"
    };
}
