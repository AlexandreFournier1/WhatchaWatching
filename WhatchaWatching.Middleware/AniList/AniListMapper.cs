using System.Net;
using System.Text.RegularExpressions;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.AniList.Models;

namespace WhatchaWatching.Middleware.AniList;

internal static partial class AniListMapper
{
    // AniList names its genres but gives them no id. The stored genres use MyAnimeList ids (the ones Jikan returns),
    // so a genre is translated by name. A genre this table does not know is left out rather than stored with a made-up id.
    private static readonly Dictionary<string, ExternalGenreDto> Genres = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Action"] = new(1, "Action"),
        ["Adventure"] = new(2, "Adventure"),
        ["Comedy"] = new(4, "Comedy"),
        ["Drama"] = new(8, "Drama"),
        ["Ecchi"] = new(9, "Ecchi"),
        ["Fantasy"] = new(10, "Fantasy"),
        ["Hentai"] = new(12, "Hentai"),
        ["Horror"] = new(14, "Horror"),
        ["Mecha"] = new(18, "Mecha"),
        ["Music"] = new(19, "Music"),
        ["Mystery"] = new(7, "Mystery"),
        ["Psychological"] = new(40, "Psychological"),
        ["Romance"] = new(22, "Romance"),
        ["Sci-Fi"] = new(24, "Sci-Fi"),
        ["Slice of Life"] = new(36, "Slice of Life"),
        ["Sports"] = new(30, "Sports"),
        ["Supernatural"] = new(37, "Supernatural"),
        ["Mahou Shoujo"] = new(66, "Mahou Shoujo"),
        ["Thriller"] = new(41, "Suspense")
    };

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLinesPattern();

    /// <summary>Returns null for an anime without a MyAnimeList id: the rest of the app cannot identify it.</summary>
    public static ExternalAnimeDto? ToDto(AniListMedia media)
    {
        if (media.IdMal is not { } id)
        {
            return null;
        }

        var releaseDate = ToDate(media.StartDate);

        return new ExternalAnimeDto(
            id,
            media.Title?.English ?? media.Title?.Romaji ?? media.Title?.Native ?? string.Empty,
            media.Title?.Native ?? media.Title?.Romaji,
            CleanDescription(media.Description),
            media.CoverImage?.Large,
            media.Episodes,
            media.Duration,
            ParseStatus(media.Status),
            media.AverageScore is { } score ? Math.Round(score / 10m, 1) : null,
            media.SeasonYear ?? media.StartDate?.Year,
            media.Season?.ToLowerInvariant(),
            null,
            releaseDate,
            ToGenres(media.Genres))
        {
            StreamingLinks = ToStreamingLinks(media.ExternalLinks)
        };
    }

    // AniList also lists official sites and social networks: only the streaming platforms are of use here.
    public static IReadOnlyList<StreamingLinkDto> ToStreamingLinks(IEnumerable<AniListExternalLink>? links) =>
        StreamingLinkFilter.Clean(
            (links ?? [])
                .Where(l => l.Type == "STREAMING" && l.IsDisabled != true)
                .Select(l => (l.Site, l.Url)));

    public static MediaStatus? ParseStatus(string? status) => status switch
    {
        "RELEASING" => MediaStatus.Airing,
        "FINISHED" => MediaStatus.Finished,
        "NOT_YET_RELEASED" => MediaStatus.Upcoming,
        _ => null
    };

    public static IReadOnlyList<ExternalGenreDto> ToGenres(IEnumerable<string>? names) =>
        (names ?? [])
            .Select(name => Genres.GetValueOrDefault(name))
            .OfType<ExternalGenreDto>()
            .DistinctBy(g => g.ExternalId)
            .ToList();

    // AniList descriptions are HTML ("<br><br>", "<i>...</i>").
    public static string? CleanDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var text = LineBreakPattern().Replace(description, "\n");
        text = WebUtility.HtmlDecode(TagPattern().Replace(text, string.Empty));
        text = BlankLinesPattern().Replace(text.Trim(), "\n\n");

        return text.Length == 0 ? null : text;
    }

    private static DateTime? ToDate(AniListDate? date)
    {
        if (date?.Year is not { } year)
        {
            return null;
        }

        try
        {
            return new DateTime(year, date.Month ?? 1, date.Day ?? 1, 0, 0, 0, DateTimeKind.Utc);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
