namespace WhatchaWatching.Infrastructure.Data.Entities;

/// <summary>
/// Entity whose creation and last-update timestamps (UTC) are set by <see cref="ApplicationDbContext"/>.
/// </summary>
public interface IAuditableEntity
{
    DateTime UpdatedAt { get; set; }
}
