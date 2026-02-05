using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PoNovaWeight.Client.Services;

/// <summary>
/// Custom authentication state provider for development that uses JWT tokens from /api/auth/dev-login.
/// In production, this is replaced by the standard OIDC provider.
/// </summary>
public class DevAuthStateProvider(IHttpClientFactory httpClientFactory, IJSRuntime jsRuntime) : AuthenticationStateProvider
{
    private const string TokenKey = "dev_auth_token";
    private const string UserKey = "dev_auth_user";
    
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                var userJson = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", UserKey);
                if (!string.IsNullOrEmpty(userJson))
                {
                    var user = JsonSerializer.Deserialize<DevUser>(userJson);
                    if (user != null)
                    {
                        var identity = CreateIdentity(user.Email, user.DisplayName);
                        _currentUser = new ClaimsPrincipal(identity);
                    }
                }
            }
        }
        catch
        {
            // JavaScript not available during prerendering
        }
        
        return new AuthenticationState(_currentUser);
    }

    /// <summary>
    /// Performs a dev login by calling the API endpoint.
    /// </summary>
    public async Task<bool> LoginAsync(string email = "dev-user@local")
    {
        try
        {
            // Use the unauthenticated client for login
            var httpClient = httpClientFactory.CreateClient("UnauthenticatedClient");
            var response = await httpClient.PostAsync($"/api/auth/dev-login?email={Uri.EscapeDataString(email)}", null);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DevLoginResponse>();
                if (result?.Token != null)
                {
                    await StoreTokenAsync(result.Token);
                    
                    var user = new DevUser { Email = result.User?.Email ?? email, DisplayName = result.User?.DisplayName ?? email };
                    var userJson = JsonSerializer.Serialize(user);
                    await jsRuntime.InvokeVoidAsync("localStorage.setItem", UserKey, userJson);
                    
                    var identity = CreateIdentity(user.Email, user.DisplayName);
                    _currentUser = new ClaimsPrincipal(identity);
                    
                    NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dev login failed: {ex.Message}");
        }
        
        return false;
    }

    /// <summary>
    /// Logs out by clearing the stored token.
    /// </summary>
    public async Task LogoutAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserKey);
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    /// <summary>
    /// Gets the stored token for API calls.
    /// </summary>
    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        }
        catch
        {
            return null;
        }
    }

    private async Task StoreTokenAsync(string token)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
    }

    private static ClaimsIdentity CreateIdentity(string email, string displayName)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim("email", email),
            new Claim(ClaimTypes.Name, displayName),
            new Claim("name", displayName),
            new Claim(ClaimTypes.NameIdentifier, email)
        };
        
        return new ClaimsIdentity(claims, "DevAuth");
    }

    private record DevUser
    {
        public string Email { get; init; } = "";
        public string DisplayName { get; init; } = "";
    }

    private record DevLoginResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; init; }
        
        [JsonPropertyName("isAuthenticated")]
        public bool IsAuthenticated { get; init; }
        
        [JsonPropertyName("user")]
        public UserInfo? User { get; init; }
    }

    private record UserInfo
    {
        [JsonPropertyName("email")]
        public string? Email { get; init; }
        
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; init; }
    }
}
