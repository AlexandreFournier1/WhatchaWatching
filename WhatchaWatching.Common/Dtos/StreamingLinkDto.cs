namespace WhatchaWatching.Common.Dtos;

/// <summary>A page where the title can be watched legally, on a streaming platform.</summary>
public record StreamingLinkDto(string Site, string Url)
{
    /// <summary>The country the platform is available in (ISO code). Null when it does not depend on the country.</summary>
    public string? Region { get; init; }
}
