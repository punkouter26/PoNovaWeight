using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PoNovaWeight.Client;
using PoNovaWeight.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API calls with cookie credentials
// Note: In Blazor WASM, the browser handles HTTP natively via JS interop
// CookieHandler sets BrowserRequestCredentials.Include on each request
builder.Services.AddScoped<CookieHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<CookieHandler>();
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    };
});

// Register API client service
builder.Services.AddScoped<ApiClient>();

// Authentication services
builder.Services.AddScoped<NovaAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<NovaAuthStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
