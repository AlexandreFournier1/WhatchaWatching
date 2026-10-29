using System.Globalization;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Tmdb.Models;

namespace WhatchaWatching.Middleware.Tmdb;

internal static class TmdbMapper
{
    public static ExternalMediaDto ToDto(TmdbTitle title, MediaType type, string watchRegion)
    {
        var isMovie = type == MediaType.Movie;
        var releaseDate = ParseDate(isMovie ? title.ReleaseDate : title.FirstAirDate);

        return new ExternalMediaDto(
            type,
            title.Id,
            (isMovie ? title.Title ?? title.OriginalTitle : title.Name ?? title.OriginalName) ?? string.Empty,
            isMovie ? title.OriginalTitle : title.OriginalName,
            NullIfBlank(title.Overview),
            string.IsNullOrWhiteSpace(title.PosterPath) ? null : ExternalApiConstants.TMDB_IMAGE_BASE_URL + title.PosterPath,
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
            StreamingLinks = ToStreamingLinks(title.WatchProviders, watchRegion),
            IsAdult = title.Adult == true
        };
    }

    // Details give the names; search results only give ids.
    private static IReadOnlyList<ExternalGenreDto> ToGenres(TmdbTitle title, MediaType type) =>
        title.Genres is { } genres
            ? genres.Where(g => !string.IsNullOrWhiteSpace(g.Name)).Select(g => new ExternalGenreDto(g.Id, g.Name!)).ToList()
            : TmdbGenres.FromIds(type, title.GenreIds);

    // TMDB has no address for each platform, only one page for the country, so every platform shares it.
    private static IReadOnlyList<StreamingLinkDto> ToStreamingLinks(TmdbWatchProviders? providers, string watchRegion)
    {
        if (providers?.Results is not { } results ||
            !results.TryGetValue(watchRegion, out var region) ||
            region.Flatrate is not { } platforms)
        {
            return [];
        }

        return platforms
            .SelectMany(p => StreamingLinkFilter.Clean([(p.ProviderName, region.Link)]))
            .DistinctBy(l => l.Site, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

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
