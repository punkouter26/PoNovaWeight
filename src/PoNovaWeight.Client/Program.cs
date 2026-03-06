using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PoNovaWeight.Client;
using PoNovaWeight.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Check if Google ClientId is configured
var googleClientId = builder.Configuration["Google:ClientId"];
var hasGoogleClientId = !string.IsNullOrEmpty(googleClientId) && googleClientId != "YOUR_GOOGLE_CLIENT_ID";

builder.Services.AddScoped<CombinedAuthorizationHandler>();

// Register the authenticated HttpClient - CombinedAuthorizationHandler handles OIDC tokens
builder.Services.AddHttpClient("PoNovaWeight.Api", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<CombinedAuthorizationHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("PoNovaWeight.Api"));

if (hasGoogleClientId)
{
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
    builder.Services.AddAuthorizationCore();
}

// Register API client service
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
