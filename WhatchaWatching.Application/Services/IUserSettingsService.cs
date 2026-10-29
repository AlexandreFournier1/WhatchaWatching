namespace WhatchaWatching.Application.Services;

public interface IUserSettingsService
{
    /// <summary>Whether the user chose to see adult anime. False by default, and for an unknown user.</summary>
    Task<bool> ShowsAdultContentAsync(int userId, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">There is no user with this id.</exception>
    Task SetShowsAdultContentAsync(int userId, bool value, CancellationToken cancellationToken = default);

    /// <summary>
    /// The country whose streaming platforms are shown to the user (an ISO code from <c>CountryCatalog</c>): the one they
    /// chose, or the default one. Also the default one for an unknown user.
    /// </summary>
    Task<string> GetCountryAsync(int userId, CancellationToken cancellationToken = default);

    /// <param name="country">A code from <c>CountryCatalog</c>, or null to go back to the default country.</param>
    /// <exception cref="ArgumentException">The code is not one of the catalog.</exception>
    /// <exception cref="InvalidOperationException">There is no user with this id.</exception>
    Task SetCountryAsync(int userId, string? country, CancellationToken cancellationToken = default);
}
