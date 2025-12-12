using Microsoft.AspNetCore.Components.Authorization;
using PoNovaWeight.Shared.DTOs;
using System.Security.Claims;

namespace PoNovaWeight.Client.Services;

/// <summary>
/// Client-side authentication service that manages auth state.
/// </summary>
public class AuthService
{
    private readonly ApiClient _apiClient;
    private readonly NovaAuthStateProvider _authStateProvider;

    public AuthService(ApiClient apiClient, AuthenticationStateProvider authStateProvider)
    {
        _apiClient = apiClient;
        _authStateProvider = (NovaAuthStateProvider)authStateProvider;
    }

    /// <summary>
    /// Gets the current user's authentication status.
    /// </summary>
    public async Task<AuthStatus?> GetCurrentUserAsync()
    {
        try
        {
            return await _apiClient.GetCurrentUserAsync();
        }
        catch
        {
            return AuthStatus.Unauthenticated();
        }
    }

    /// <summary>
    /// Refreshes the authentication state from the server.
    /// </summary>
    public async Task RefreshAuthStateAsync()
    {
        var status = await GetCurrentUserAsync();
        _authStateProvider.NotifyAuthStateChanged(status);
    }

    /// <summary>
    /// Gets the login URL for initiating Google sign-in.
    /// </summary>
    public static string GetLoginUrl(string? returnUrl = "/")
        => $"/api/auth/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";

    /// <summary>
    /// Gets the logout URL for signing out.
    /// </summary>
    public static string GetLogoutUrl() => "/api/auth/logout";
}
