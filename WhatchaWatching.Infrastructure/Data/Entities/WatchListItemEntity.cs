using WhatchaWatching.Common.Enum;

namespace WhatchaWatching.Infrastructure.Data.Entities;

public class WatchListItemEntity : IAuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MediaId { get; set; }
    public WatchListItemStatus Status { get; set; }
    public int? Progress { get; set; } // Number of episodes watched for TV shows and anime, null for movies
    public byte? UserScore { get; set; } // 1-10
    public string? Notes { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public MediaEntity Media { get; set; } = null!;
}
