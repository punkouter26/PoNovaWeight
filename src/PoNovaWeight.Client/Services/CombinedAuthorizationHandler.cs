using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace PoNovaWeight.Client.Services;

/// <summary>
/// HTTP message handler that attaches authorization tokens from either:
/// 1. Microsoft OIDC (id_token from sessionStorage)
/// 2. Google OIDC (id_token from OIDC library's session storage)
/// </summary>
public class CombinedAuthorizationHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;

    public CombinedAuthorizationHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            // Try Microsoft id_token from sessionStorage
            var msToken = await _jsRuntime.InvokeAsync<string?>(
                "PoNovaWeightAuth.getMicrosoftToken",
                cancellationToken);

            if (!string.IsNullOrEmpty(msToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", msToken);
                return await base.SendAsync(request, cancellationToken);
            }

            // Try Google OIDC id_token from sessionStorage (where the OIDC library stores user state)
            var idToken = await _jsRuntime.InvokeAsync<string?>(
                "PoNovaWeightAuth.getOidcToken",
                cancellationToken);

            if (!string.IsNullOrEmpty(idToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
            }
        }
        catch (Exception ex)
        {
            try { await _jsRuntime.InvokeVoidAsync("console.error", "[Auth] Handler error: " + ex.Message); } catch { }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
