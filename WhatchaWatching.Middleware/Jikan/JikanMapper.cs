using System.Text.RegularExpressions;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Jikan.Models;

namespace WhatchaWatching.Middleware.Jikan;

internal static partial class JikanMapper
{
    // Jikan describes durations as free text: "24 min per ep", "1 hr 30 min", "Unknown".
    [GeneratedRegex(@"^\s*(?:(?<hours>\d+)\s*hr)?\s*(?:(?<minutes>\d+)\s*min)?", RegexOptions.IgnoreCase)]
    private static partial Regex DurationPattern();

    public static ExternalAnimeDto ToDto(JikanAnime anime)
    {
        var title = anime.TitleEnglish ?? anime.Title ?? anime.TitleJapanese ?? string.Empty;
        var releaseDate = anime.Aired?.From?.UtcDateTime;

        return new ExternalAnimeDto(
            anime.MalId,
            title,
            anime.TitleJapanese ?? anime.Title,
            anime.Synopsis,
            anime.Images?.Jpg?.LargeImageUrl ?? anime.Images?.Jpg?.ImageUrl,
            anime.Episodes,
            ParseDurationMinutes(anime.Duration),
            ParseStatus(anime.Status),
            anime.Score,
            anime.Year ?? releaseDate?.Year,
            anime.Season,
            anime.Rating,
            releaseDate,
            ToGenres(anime.Genres));
    }

    public static ExternalGenreDto ToDto(JikanGenre genre) => new(genre.MalId, genre.Name ?? string.Empty);

    public static IReadOnlyList<ExternalGenreDto> ToGenres(IEnumerable<JikanGenre>? genres) =>
        (genres ?? [])
            .Where(g => !string.IsNullOrWhiteSpace(g.Name))
            .Select(ToDto)
            .ToList();

    public static int? ParseDurationMinutes(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
        {
            return null;
        }

        var match = DurationPattern().Match(duration);
        var hours = match.Groups["hours"].Success ? int.Parse(match.Groups["hours"].Value) : 0;
        var minutes = match.Groups["minutes"].Success ? int.Parse(match.Groups["minutes"].Value) : 0;
        var total = hours * 60 + minutes;

        return total > 0 ? total : null;
    }

    public static MediaStatus? ParseStatus(string? status) => status switch
    {
        "Currently Airing" => MediaStatus.Airing,
        "Finished Airing" => MediaStatus.Finished,
        "Not yet aired" => MediaStatus.Upcoming,
        _ => null
    };
}
