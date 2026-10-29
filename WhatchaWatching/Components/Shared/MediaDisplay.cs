using System.Globalization;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Services.Localization;

namespace WhatchaWatching.Components.Shared;

/// <summary>The words that describe a title. Without a localizer they are in English.</summary>
public static class MediaDisplay
{
    public static string Duration(MediaSummaryDto media, ILocalizer? l = null) => media.EpisodeDurationMinutes switch
    {
        { } minutes when media.Type == MediaType.Movie => Loc.T(l, "{0} min", minutes),
        { } minutes => Loc.T(l, "{0} min / episode", minutes),
        _ => Loc.T(l, "Duration unknown")
    };

    /// <summary>
    /// The line under a title on a card: the year and the duration, whichever are known. A search of TMDB gives no
    /// duration, and a line that says "unknown" on every card is noise: it is simply left out.
    /// </summary>
    public static string? CardMeta(MediaSummaryDto media, ILocalizer? l = null)
    {
        var parts = new List<string>();
        if (media.Year is { } year)
        {
            parts.Add(year.ToString(CultureInfo.InvariantCulture));
        }

        if (media.EpisodeDurationMinutes is not null)
        {
            parts.Add(Duration(media, l));
        }

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    /// <summary>The episodes (and seasons) of a series or an anime; null for a movie, which has none.</summary>
    public static string? Episodes(MediaSummaryDto media, ILocalizer? l = null)
    {
        if (media.Type == MediaType.Movie)
        {
            return null;
        }

        var episodes = media.Episodes is { } count ? Loc.T(l, "{0} episodes", count) : Loc.T(l, "Episodes unknown");

        return media.Seasons is { } seasons
            ? $"{episodes} · {Loc.T(l, seasons == 1 ? "{0} season" : "{0} seasons", seasons)}"
            : episodes;
    }

    /// <summary>A released movie is not "finished airing".</summary>
    public static string? Status(MediaSummaryDto media, ILocalizer? l = null) =>
        media.Type == MediaType.Movie && media.Status == nameof(MediaStatus.Finished)
            ? Loc.T(l, "Released")
            : media.Status is null ? null : Loc.T(l, media.Status);

    /// <summary>What the score is, for its tooltip.</summary>
    public static string ScoreSource(MediaSummaryDto media, ILocalizer? l = null) =>
        Loc.T(l, media.Type == MediaType.Anime ? "MyAnimeList score" : "TMDB user score");
}
