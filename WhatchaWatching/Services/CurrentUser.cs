using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using WhatchaWatching.Application.Services;

namespace WhatchaWatching.Services;

public sealed class CurrentUser(AuthenticationStateProvider authenticationStateProvider) : ICurrentUser
{
    public async Task<int?> GetUserIdAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        var id = state.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(id, out var userId) ? userId : null;
    }
}
