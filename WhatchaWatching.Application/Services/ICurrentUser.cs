namespace WhatchaWatching.Application.Services;

public interface ICurrentUser
{
    /// <summary>The id of the signed-in user, or null when the visitor is anonymous.</summary>
    Task<int?> GetUserIdAsync();
}
