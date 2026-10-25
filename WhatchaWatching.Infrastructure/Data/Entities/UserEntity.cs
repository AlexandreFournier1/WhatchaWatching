using Microsoft.AspNetCore.Identity;

namespace WhatchaWatching.Infrastructure.Data.Entities;

/// <summary>
/// Application user. Id, UserName, Email, PasswordHash and the lockout/security fields come from ASP.NET Core Identity.
/// </summary>
public class UserEntity : IdentityUser<int>, IAuditableEntity
{
    public DateTime CreatedAt { get; set; }

    // Off by default: the user must choose to see adult anime (and confirms being an adult when doing so).
    public bool ShowAdultContent { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<WatchListItemEntity> WatchListItems { get; set; } = new List<WatchListItemEntity>();
}
