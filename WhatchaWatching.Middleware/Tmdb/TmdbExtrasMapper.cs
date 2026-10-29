using System.Globalization;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Tmdb.Models;

namespace WhatchaWatching.Middleware.Tmdb;

internal static partial class TmdbMapper
{
    // A page shows a handful of each: more would only push the rest of it down.
    private const int MAX_CAST = 8;
    private const int MAX_SIMILAR = 8;

    public static MediaExtrasDto ToExtras(TmdbTitle title, MediaType type) => new(
        ToTrailer(title.Videos),
        ToCast(title.Credits),
        type == MediaType.TVShow ? ToSeasons(title.Seasons) : [],
        ToSimilar(title.Recommendations, type));

    // Only YouTube is linked: it is the one site whose address can be built from the key. An official trailer comes
    // before a teaser, and an unofficial one before nothing.
    private static TrailerDto? ToTrailer(TmdbVideos? videos) => (videos?.Results ?? [])
        .Where(v => v.Site == "YouTube" && !string.IsNullOrWhiteSpace(v.Key) && v.Type is "Trailer" or "Teaser")
        .OrderBy(v => v.Type == "Trailer" ? 0 : 1)
        .ThenBy(v => v.Official == true ? 0 : 1)
        .Select(v => new TrailerDto(ExternalApiConstants.YOUTUBE_WATCH_URL + Uri.EscapeDataString(v.Key!), NullIfBlank(v.Name)))
        .FirstOrDefault();

    private static IReadOnlyList<CastMemberDto> ToCast(TmdbCredits? credits) => (credits?.Cast ?? [])
        .Where(c => !string.IsNullOrWhiteSpace(c.Name))
        .OrderBy(c => c.Order ?? int.MaxValue)
        .Take(MAX_CAST)
        .Select(c => new CastMemberDto(
            c.Name!.Trim(),
            NullIfBlank(c.Character),
            string.IsNullOrWhiteSpace(c.ProfilePath) ? null : ExternalApiConstants.TMDB_THUMBNAIL_BASE_URL + c.ProfilePath))
        .ToList();

    // Season 0 holds the specials, which are not part of the story.
    private static IReadOnlyList<SeasonDto> ToSeasons(List<TmdbSeason>? seasons) => (seasons ?? [])
        .Where(s => s.SeasonNumber > 0)
        .OrderBy(s => s.SeasonNumber)
        .Select(s => new SeasonDto(
            s.SeasonNumber,
            NullIfBlank(s.Name) ?? $"Season {s.SeasonNumber}",
            Positive(s.EpisodeCount),
            ParseDate(s.AirDate)?.Year))
        .ToList();

    private static IReadOnlyList<ExternalMediaDto> ToSimilar(TmdbSearchResponse? recommendations, MediaType type) =>
        (recommendations?.Results ?? [])
            .Select(t => ToDto(t, type, thumbnail: true))
            .Where(t => t.Title.Length > 0 && t.ImageUrl is not null)
            .Take(MAX_SIMILAR)
            .ToList();
}
