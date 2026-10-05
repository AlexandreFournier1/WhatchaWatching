namespace WhatchaWatching.Infrastructure.Data.Entities;

public class MediaGenreEntity
{
    public int MediaId { get; set; }
    public int GenreId { get; set; }

    public MediaEntity Media { get; set; } = null!;
    public GenreEntity Genre { get; set; } = null!;
}
