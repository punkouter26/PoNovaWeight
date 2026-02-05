using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PoNovaWeight.Client;
using PoNovaWeight.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Check if Google ClientId is configured
var googleClientId = builder.Configuration["Google:ClientId"];
var hasGoogleClientId = !string.IsNullOrEmpty(googleClientId) && googleClientId != "YOUR_GOOGLE_CLIENT_ID";

// Always register dev auth components for dev login fallback
builder.Services.AddScoped<DevAuthStateProvider>();
builder.Services.AddScoped<CombinedAuthorizationHandler>();

// Register a plain HttpClient for unauthenticated calls (like dev-login)
builder.Services.AddHttpClient("UnauthenticatedClient", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

// Register the authenticated HttpClient - CombinedAuthorizationHandler handles both dev and OIDC tokens
builder.Services.AddHttpClient("PoNovaWeight.Api", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<CombinedAuthorizationHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("PoNovaWeight.Api"));

if (hasGoogleClientId)
{
    // Google OIDC configured: Use OIDC for primary auth, but keep dev login available
    builder.Services.AddOidcAuthentication(options =>
    {
        options.ProviderOptions.Authority = "https://accounts.google.com";
        options.ProviderOptions.ClientId = googleClientId;
        options.ProviderOptions.ResponseType = "id_token";
        options.ProviderOptions.DefaultScopes.Clear();
        options.ProviderOptions.DefaultScopes.Add("openid");
        options.ProviderOptions.DefaultScopes.Add("profile");
        options.ProviderOptions.DefaultScopes.Add("email");
        options.UserOptions.NameClaim = "name";
        options.UserOptions.RoleClaim = "role";
    });
}
else
{
    // No Google ClientId: Use dev auth only
    builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<DevAuthStateProvider>());
    builder.Services.AddAuthorizationCore();
}

// Register API client service
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
