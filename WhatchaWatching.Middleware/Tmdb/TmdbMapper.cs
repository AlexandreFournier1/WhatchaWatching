using System.Globalization;
using System.Text.RegularExpressions;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Tmdb.Models;

namespace WhatchaWatching.Middleware.Tmdb;

internal static partial class TmdbMapper
{
    public static ExternalMediaDto ToDto(TmdbTitle title, MediaType type, bool thumbnail = false)
    {
        var isMovie = type == MediaType.Movie;
        var releaseDate = ParseDate(isMovie ? title.ReleaseDate : title.FirstAirDate);

        return new ExternalMediaDto(
            type,
            title.Id,
            (isMovie ? title.Title ?? title.OriginalTitle : title.Name ?? title.OriginalName) ?? string.Empty,
            isMovie ? title.OriginalTitle : title.OriginalName,
            NullIfBlank(title.Overview),
            string.IsNullOrWhiteSpace(title.PosterPath)
                ? null
                : (thumbnail ? ExternalApiConstants.TMDB_THUMBNAIL_BASE_URL : ExternalApiConstants.TMDB_IMAGE_BASE_URL) + title.PosterPath,
            isMovie ? null : title.NumberOfEpisodes,
            isMovie ? Positive(title.Runtime) : TvEpisodeDurationMinutes(title),
            ParseStatus(type, title.Status),
            // TMDB gives 0 to a title nobody has rated yet.
            title.VoteAverage > 0 ? Math.Round(title.VoteAverage.Value, 1) : null,
            releaseDate?.Year,
            null,
            null,
            releaseDate,
            ToGenres(title, type))
        {
            SeasonCount = isMovie ? null : title.NumberOfSeasons,
            StreamingLinks = ToStreamingLinks(title.WatchProviders),
            IsAdult = IsAdult(title)
        };
    }

    // Details give the names; search results only give ids.
    private static IReadOnlyList<ExternalGenreDto> ToGenres(TmdbTitle title, MediaType type) =>
        title.Genres is { } genres
            ? TmdbGenres.Canonical(genres.Where(g => !string.IsNullOrWhiteSpace(g.Name)).Select(g => (g.Id, g.Name!)))
            : TmdbGenres.FromIds(type, title.GenreIds);

    // TMDB's own adult flag misses many titles. These words only ever name explicit films: an ambiguous one (naked,
    // sexual) would also hide legitimate titles, so a title with none of them is still the provider's word.
    [GeneratedRegex(@"\b(porn|porno|pornographic|pornstar|xxx|hentai|erotica|softcore|sexploitation)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitWords();

    private static bool IsAdult(TmdbTitle title) =>
        title.Adult == true ||
        ExplicitWords().IsMatch(title.Title ?? title.Name ?? string.Empty) ||
        ExplicitWords().IsMatch(title.OriginalTitle ?? title.OriginalName ?? string.Empty);

    // TMDB has no address for each platform, only one page per country, so every platform of a country shares it. The
    // offers of the countries a person can choose are all kept: which ones are shown is decided when the page is read.
    private static IReadOnlyList<StreamingLinkDto> ToStreamingLinks(TmdbWatchProviders? providers)
    {
        if (providers?.Results is not { } results)
        {
            return [];
        }

        return CountryCatalog.Codes
            .Where(results.ContainsKey)
            .SelectMany(code => PlatformsOf(results[code]).Select(link => link with { Region = code }))
            .ToList();
    }

    private static IEnumerable<StreamingLinkDto> PlatformsOf(TmdbRegionProviders region) =>
        (region.Flatrate ?? [])
            .SelectMany(p => StreamingLinkFilter.Clean([(TmdbPlatforms.Canonical(p.ProviderName), region.Link)]))
            .DistinctBy(l => l.Site, StringComparer.OrdinalIgnoreCase);

    // episode_run_time is deprecated and often empty: fall back on the last aired episode.
    private static int? TvEpisodeDurationMinutes(TmdbTitle title) =>
        Positive(title.EpisodeRunTime?.FirstOrDefault(minutes => minutes > 0)) ?? Positive(title.LastEpisodeToAir?.Runtime);

    public static MediaStatus? ParseStatus(MediaType type, string? status) => type == MediaType.Movie
        ? status switch
        {
            "Released" => MediaStatus.Finished,
            "Rumored" or "Planned" or "In Production" or "Post Production" => MediaStatus.Upcoming,
            _ => null
        }
        : status switch
        {
            "Returning Series" => MediaStatus.Airing,
            "Ended" or "Canceled" => MediaStatus.Finished,
            "Planned" or "In Production" or "Pilot" => MediaStatus.Upcoming,
            _ => null
        };

    // TMDB sends an empty string, not null, for a date it does not know.
    private static DateTime? ParseDate(string? date) =>
        DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : null;

    private static int? Positive(int? value) => value > 0 ? value : null;

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
