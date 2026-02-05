using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace PoNovaWeight.Client.Services;

/// <summary>
/// HTTP message handler that attaches the dev auth token to outgoing requests.
/// </summary>
public class DevTokenHandler(IJSRuntime jsRuntime) : DelegatingHandler
{
    private const string TokenKey = "dev_auth_token";
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, TokenKey);
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // JavaScript not available during prerendering
        }
        
        return await base.SendAsync(request, cancellationToken);
    }
}
