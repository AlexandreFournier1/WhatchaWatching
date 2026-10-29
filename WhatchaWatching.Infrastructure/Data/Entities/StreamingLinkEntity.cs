namespace WhatchaWatching.Infrastructure.Data.Entities;

public class StreamingLinkEntity
{
    public int Id { get; set; }
    public int MediaId { get; set; }
    public string Site { get; set; } = string.Empty; // Platform name, for example "Crunchyroll"
    public string Url { get; set; } = string.Empty;
    public string? Region { get; set; } // Country the offer is for (ISO code), null when it does not depend on the country

    public MediaEntity Media { get; set; } = null!;
}
