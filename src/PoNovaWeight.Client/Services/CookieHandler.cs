using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace PoNovaWeight.Client.Services;

/// <summary>
/// HTTP message handler that ensures cookies are included in requests.
/// Required for cookie-based authentication in Blazor WebAssembly.
/// </summary>
public class CookieHandler : DelegatingHandler
{
    public CookieHandler() : base(new HttpClientHandler())
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Include cookies with all requests (same-origin)
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        
        // Disable caching for API requests to ensure fresh auth state
        request.SetBrowserRequestCache(BrowserRequestCache.NoStore);
        
        return await base.SendAsync(request, cancellationToken);
    }
}
