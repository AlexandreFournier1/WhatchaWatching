using Microsoft.EntityFrameworkCore;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Infrastructure.Data;
using WhatchaWatching.Infrastructure.Data.Entities;

namespace WhatchaWatching.Application.Services;

internal static class MediaGenreSync
{
    /// <summary>
    /// Makes the media link exactly the given genres, reusing the genres already stored. The ids are those of the
    /// provider of the media's type, so they are only looked up among the genres of the same source.
    /// </summary>
    public static async Task SyncAsync(
        ApplicationDbContext context,
        MediaEntity media,
        IReadOnlyList<ExternalGenreDto> genres,
        CancellationToken cancellationToken)
    {
        var source = SourceOf(media.Type);
        var wantedIds = genres.Select(g => g.ExternalId).ToList();
        var known = await context.Genres
            .Where(g => g.Source == source && wantedIds.Contains(g.ExternalId))
            .ToDictionaryAsync(g => g.ExternalId, cancellationToken);

        foreach (var genre in genres.Where(g => !known.ContainsKey(g.ExternalId)))
        {
            var created = new GenreEntity { Source = source, ExternalId = genre.ExternalId, Name = genre.Name };
            context.Genres.Add(created);
            known[genre.ExternalId] = created;
        }

        var outdated = media.MediaGenres.Where(mg => !wantedIds.Contains(mg.Genre.ExternalId)).ToList();
        context.MediaGenres.RemoveRange(outdated);

        var linked = media.MediaGenres.Except(outdated).Select(mg => mg.Genre.ExternalId).ToHashSet();
        foreach (var genreId in wantedIds.Where(id => !linked.Contains(id)).Distinct())
        {
            media.MediaGenres.Add(new MediaGenreEntity { Media = media, Genre = known[genreId] });
        }
    }

    private static GenreSource SourceOf(MediaType type) =>
        type is MediaType.Movie or MediaType.TVShow ? GenreSource.Tmdb : GenreSource.MyAnimeList;
}
