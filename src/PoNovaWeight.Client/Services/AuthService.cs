using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace PoNovaWeight.Client.Services;

/// <summary>
/// Client-side authentication service that works with both dev auth and OIDC.
/// </summary>
public class AuthService(
    AuthenticationStateProvider authStateProvider, 
    NavigationManager navigationManager,
    IConfiguration configuration,
    IJSRuntime jsRuntime)
{
    private bool IsDevMode
    {
        get
        {
            var clientId = configuration["Google:ClientId"];
            return string.IsNullOrEmpty(clientId) || clientId == "YOUR_GOOGLE_CLIENT_ID";
        }
    }

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
        if (IsDevMode)
        {
            navigationManager.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        }
        else
        {
            navigationManager.NavigateToLogin($"authentication/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        }
    }

    /// <summary>
    /// Initiates the logout flow.
    /// </summary>
    public async Task LogoutAsync()
    {
        // Always clear all token types to prevent stale tokens
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "dev_auth_token");
            await jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "microsoft_id_token");
        }
        catch { /* JS interop may not be available */ }

        if (IsDevMode && authStateProvider is DevAuthStateProvider devAuth)
        {
            await devAuth.LogoutAsync();
            navigationManager.NavigateTo("/login", forceLoad: true);
        }
        else
        {
            navigationManager.NavigateToLogout("authentication/logout");
        }
    }
}
