using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Components.Shared;

public static class AnimeDisplay
{
    public static string Duration(AnimeSummaryDto anime) =>
        anime.EpisodeDurationMinutes is { } minutes ? $"{minutes} min / episode" : "Duration unknown";

    public static string Episodes(AnimeSummaryDto anime) =>
        anime.Episodes is { } count ? $"{count} episodes" : "Episodes unknown";
}
