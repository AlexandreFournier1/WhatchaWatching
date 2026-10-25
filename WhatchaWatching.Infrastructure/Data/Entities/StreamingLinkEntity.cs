namespace WhatchaWatching.Infrastructure.Data.Entities;

public class StreamingLinkEntity
{
    public int Id { get; set; }
    public int MediaId { get; set; }
    public string Site { get; set; } = string.Empty; // Platform name, for example "Crunchyroll"
    public string Url { get; set; } = string.Empty;

    public MediaEntity Media { get; set; } = null!;
}
