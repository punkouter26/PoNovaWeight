using Microsoft.AspNetCore.Components.Authorization;
using PoNovaWeight.Shared.DTOs;
using System.Security.Claims;

namespace PoNovaWeight.Client.Services;

/// <summary>
/// Custom AuthenticationStateProvider for Blazor WASM.
/// Fetches auth state from the server API.
/// </summary>
public class NovaAuthStateProvider(ApiClient apiClient) : AuthenticationStateProvider
{
    private AuthStatus? _cachedStatus;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            _cachedStatus = await apiClient.GetCurrentUserAsync();
            return BuildAuthenticationState(_cachedStatus);
        }
        catch
        {
            // Failed to fetch auth state - user is not authenticated
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// Notifies that authentication state has changed.
    /// </summary>
    public void NotifyAuthStateChanged(AuthStatus? status)
    {
        _cachedStatus = status;
        NotifyAuthenticationStateChanged(Task.FromResult(BuildAuthenticationState(_cachedStatus)));
    }

    private static AuthenticationState BuildAuthenticationState(AuthStatus? status)
    {
        if (status?.IsAuthenticated != true || status.User is null)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, status.User.Email),
            new(ClaimTypes.Name, status.User.DisplayName)
        };

        if (!string.IsNullOrEmpty(status.User.PictureUrl))
        {
            claims.Add(new Claim("picture", status.User.PictureUrl));
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "Google")));
    }

    /// <summary>
    /// Gets the cached user info if available.
    /// </summary>
    public UserInfo? CachedUser => _cachedStatus?.User;
}
