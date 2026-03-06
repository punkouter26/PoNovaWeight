using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace PoNovaWeight.Client.Services;

/// <summary>
/// Client-side authentication service that works with Google and Microsoft OIDC.
/// </summary>
public class AuthService(
    AuthenticationStateProvider authStateProvider, 
    NavigationManager navigationManager,
    IJSRuntime jsRuntime)
{

    /// <summary>
    /// Gets the current user's email from the authentication state.
    /// </summary>
    public async Task<string?> GetCurrentUserEmailAsync()
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        return authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value 
            ?? authState.User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
    }

    /// <summary>
    /// Gets whether the user is currently authenticated.
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated == true;
    }

    /// <summary>
    /// Initiates the login flow.
    /// </summary>
    public void Login(string? returnUrl = "/")
    {
        navigationManager.NavigateToLogin($"authentication/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    /// <summary>
    /// Initiates the logout flow.
    /// </summary>
    public async Task LogoutAsync()
    {
        // Clear all token types to prevent stale tokens
        try
        {
            await jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "microsoft_id_token");
        }
        catch { /* JS interop may not be available */ }

        navigationManager.NavigateToLogout("authentication/logout");
    }
}
