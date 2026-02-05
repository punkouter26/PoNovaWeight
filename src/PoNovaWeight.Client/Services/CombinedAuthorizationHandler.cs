using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace PoNovaWeight.Client.Services;

/// <summary>
/// HTTP message handler that attaches authorization tokens from either:
/// 1. Dev login (localStorage dev_auth_token)
/// 2. Google OIDC (id_token from OIDC library's session storage)
/// </summary>
public class CombinedAuthorizationHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;
    private const string DevTokenKey = "dev_auth_token";

    public CombinedAuthorizationHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            // First, try dev token from localStorage
            var devToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, DevTokenKey);

            if (!string.IsNullOrEmpty(devToken))
            {
                await _jsRuntime.InvokeVoidAsync("console.log", cancellationToken, "[Auth] Using dev token");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", devToken);
                return await base.SendAsync(request, cancellationToken);
            }

            // Try Microsoft id_token from sessionStorage
            var msToken = await _jsRuntime.InvokeAsync<string?>(
                "sessionStorage.getItem",
                cancellationToken,
                "microsoft_id_token");

            if (!string.IsNullOrEmpty(msToken))
            {
                await _jsRuntime.InvokeVoidAsync("console.log", cancellationToken, "[Auth] Using Microsoft token, length: " + msToken.Length);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", msToken);
                return await base.SendAsync(request, cancellationToken);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("console.log", cancellationToken, "[Auth] No Microsoft token found in sessionStorage");
            }

            // Try Google OIDC id_token from sessionStorage (where the OIDC library stores user state)
            var idToken = await _jsRuntime.InvokeAsync<string?>(
                "eval",
                cancellationToken,
                @"(function() {
                    try {
                        for (let i = 0; i < sessionStorage.length; i++) {
                            const key = sessionStorage.key(i);
                            if (key && key.startsWith('oidc.')) {
                                const val = JSON.parse(sessionStorage.getItem(key));
                                if (val && val.id_token) return val.id_token;
                            }
                        }
                    } catch(e) {}
                    return null;
                })()");

            if (!string.IsNullOrEmpty(idToken))
            {
                await _jsRuntime.InvokeVoidAsync("console.log", cancellationToken, "[Auth] Using Google OIDC token");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("console.log", cancellationToken, "[Auth] No token found at all");
            }
        }
        catch (Exception ex)
        {
            try { await _jsRuntime.InvokeVoidAsync("console.error", "[Auth] Handler error: " + ex.Message); } catch { }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
