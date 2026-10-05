namespace WhatchaWatching.Infrastructure.Data.Entities;

public class GenreEntity
{
    public int Id { get; set; }
    public int ExternalId { get; set; } // Id in the external provider (Jikan/MyAnimeList)
    public string Name { get; set; } = string.Empty;

    public ICollection<MediaGenreEntity> MediaGenres { get; set; } = new List<MediaGenreEntity>();
}
