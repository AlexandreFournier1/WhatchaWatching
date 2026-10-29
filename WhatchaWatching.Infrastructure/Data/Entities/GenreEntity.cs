using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Infrastructure.Data.Entities;

public class GenreEntity
{
    public int Id { get; set; }
    public GenreSource Source { get; set; } // Anime genres come from MyAnimeList (Jikan), movie and TV genres from TMDB
    public int ExternalId { get; set; } // Id in the provider named by Source, unique per Source
    public string Name { get; set; } = string.Empty;

    public ICollection<MediaGenreEntity> MediaGenres { get; set; } = new List<MediaGenreEntity>();
}
