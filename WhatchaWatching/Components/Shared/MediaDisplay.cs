using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Components.Shared;

public static class MediaDisplay
{
    public static string Duration(MediaSummaryDto media) => media.EpisodeDurationMinutes switch
    {
        { } minutes when media.Type == MediaType.Movie => $"{minutes} min",
        { } minutes => $"{minutes} min / episode",
        _ => "Duration unknown"
    };

    /// <summary>The episodes (and seasons) of a series or an anime; null for a movie, which has none.</summary>
    public static string? Episodes(MediaSummaryDto media)
    {
        if (media.Type == MediaType.Movie)
        {
            return null;
        }

        var episodes = media.Episodes is { } count ? $"{count} episodes" : "Episodes unknown";

        return media.Seasons is { } seasons ? $"{episodes} \u00b7 {seasons} {(seasons == 1 ? "season" : "seasons")}" : episodes;
    }

    /// <summary>A released movie is not "finished airing".</summary>
    public static string? Status(MediaSummaryDto media) =>
        media.Type == MediaType.Movie && media.Status == nameof(MediaStatus.Finished) ? "Released" : media.Status;

    /// <summary>What the score is, for its tooltip.</summary>
    public static string ScoreSource(MediaSummaryDto media) =>
        media.Type == MediaType.Anime ? "MyAnimeList score" : "TMDB user score";
}
