using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Infrastructure.Data.Entities;

public class MediaEntity : IAuditableEntity
{
    public int Id { get; set; }
    public int ExternalId { get; set; } // Id in the external provider (Jikan/MyAnimeList), unique per Type
    public MediaType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? Synopsis { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public int? EpisodeCount { get; set; } // For TV shows and anime, null for movies
    public int? EpisodeDurationMinutes { get; set; }
    public int? SeasonCount { get; set; } // For TV shows and anime, null for movies
    public MediaStatus Status { get; set; }
    public decimal? Score { get; set; }
    public int? Year { get; set; }
    public string? Season { get; set; }
    public string? Rating { get; set; } // Age rating
    public string? ImageUrl { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MediaGenreEntity> MediaGenres { get; set; } = new List<MediaGenreEntity>();
    public ICollection<WatchListItemEntity> WatchListItems { get; set; } = new List<WatchListItemEntity>();
}
