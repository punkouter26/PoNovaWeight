using Microsoft.AspNetCore.Components.Authorization;
using PoNovaWeight.Shared.DTOs;
using System.Security.Claims;

namespace PoNovaWeight.Client.Services;

/// <summary>
/// Custom AuthenticationStateProvider for Blazor WASM.
/// Fetches auth state from the server API.
/// </summary>
public class NovaAuthStateProvider : AuthenticationStateProvider
{
    private readonly ApiClient _apiClient;
    private AuthStatus? _cachedStatus;

    public NovaAuthStateProvider(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            _cachedStatus = await _apiClient.GetCurrentUserAsync();

            if (_cachedStatus?.IsAuthenticated == true && _cachedStatus.User is not null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Email, _cachedStatus.User.Email),
                    new(ClaimTypes.Name, _cachedStatus.User.DisplayName)
                };

                if (!string.IsNullOrEmpty(_cachedStatus.User.PictureUrl))
                {
                    claims.Add(new Claim("picture", _cachedStatus.User.PictureUrl));
                }

                var identity = new ClaimsIdentity(claims, "Google");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
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
        NotifyAuthenticationStateChanged(GetAuthenticationStateFromCache());
    }

    private Task<AuthenticationState> GetAuthenticationStateFromCache()
    {
        if (_cachedStatus?.IsAuthenticated == true && _cachedStatus.User is not null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, _cachedStatus.User.Email),
                new(ClaimTypes.Name, _cachedStatus.User.DisplayName)
            };

            if (!string.IsNullOrEmpty(_cachedStatus.User.PictureUrl))
            {
                claims.Add(new Claim("picture", _cachedStatus.User.PictureUrl));
            }

            var identity = new ClaimsIdentity(claims, "Google");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    /// <summary>
    /// Gets the cached user info if available.
    /// </summary>
    public UserInfo? CachedUser => _cachedStatus?.User;
}
