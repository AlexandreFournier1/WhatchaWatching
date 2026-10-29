namespace WhatchaWatching.Application.Services;

public interface IAccountService
{
    /// <summary>
    /// Deletes the account and everything that belongs to it: the list and the anime the user added by hand. Titles
    /// of the shared catalog stay, other users may list them.
    /// </summary>
    /// <returns>False when there is no such account.</returns>
    Task<bool> DeleteAsync(int userId, CancellationToken cancellationToken = default);
}
