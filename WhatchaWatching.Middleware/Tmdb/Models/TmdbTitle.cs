using System.Text.Json.Serialization;

namespace WhatchaWatching.Middleware.Tmdb.Models;

/// <summary>
/// A movie or a TV series: TMDB uses different field names for each (title/name, release_date/first_air_date...), so
/// both are read here. Search results carry <c>GenreIds</c>, details carry <c>Genres</c> and the extra fields.
/// </summary>
internal record TmdbTitle(
    int Id,
    string? Title,
    string? OriginalTitle,
    string? Name,
    string? OriginalName,
    string? Overview,
    string? PosterPath,
    string? ReleaseDate,
    string? FirstAirDate,
    decimal? VoteAverage,
    int? VoteCount,
    bool? Adult,
    string? Status,
    int? Runtime,
    List<int>? EpisodeRunTime,
    int? NumberOfEpisodes,
    int? NumberOfSeasons,
    TmdbEpisode? LastEpisodeToAir,
    List<int>? GenreIds,
    List<TmdbGenre>? Genres,
    [property: JsonPropertyName("watch/providers")] TmdbWatchProviders? WatchProviders,
    TmdbVideos? Videos,
    TmdbCredits? Credits,
    TmdbSearchResponse? Recommendations,
    List<TmdbSeason>? Seasons);
