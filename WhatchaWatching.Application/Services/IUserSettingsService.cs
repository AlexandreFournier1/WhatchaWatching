namespace WhatchaWatching.Application.Services;

public interface IUserSettingsService
{
    /// <summary>Whether the user chose to see adult anime. False by default, and for an unknown user.</summary>
    Task<bool> ShowsAdultContentAsync(int userId, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">There is no user with this id.</exception>
    Task SetShowsAdultContentAsync(int userId, bool value, CancellationToken cancellationToken = default);
}
