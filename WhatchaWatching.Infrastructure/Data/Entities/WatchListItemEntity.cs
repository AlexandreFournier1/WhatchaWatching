namespace WhatchaWatching.Infrastructure.Data.Entities;

public class WatchListItemEntity : IAuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MediaId { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public MediaEntity Media { get; set; } = null!;
}
