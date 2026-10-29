using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Components.Shared;

public static class AnimeDisplay
{
    public static string Duration(MediaSummaryDto anime) =>
        anime.EpisodeDurationMinutes is { } minutes ? $"{minutes} min / episode" : "Duration unknown";

    public static string Episodes(MediaSummaryDto anime) =>
        anime.Episodes is { } count ? $"{count} episodes" : "Episodes unknown";
}
