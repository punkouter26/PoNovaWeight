using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace PoNovaWeight.Api.Features.Diagnostics;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/diag", ([FromServices] IConfiguration config) =>
        {
            var dict = new Dictionary<string, string>();
            
            // Collect all relevant configuration keys
            AddConfigValue(dict, config, "KeyVault:VaultUri");
            AddConfigValue(dict, config, "ConnectionStrings:AzureStorage", true);
            AddConfigValue(dict, config, "ConnectionStrings:tables", true);
            AddConfigValue(dict, config, "AzureOpenAI:Endpoint");
            AddConfigValue(dict, config, "AzureOpenAI:ApiKey", true);
            AddConfigValue(dict, config, "Google:ClientId");
            AddConfigValue(dict, config, "Google:ClientSecret", true);
            AddConfigValue(dict, config, "ApplicationInsights:ConnectionString", true);
            
            // Prefixed versions
            AddConfigValue(dict, config, "PoNovaWeight:AzureStorage:ConnectionString", true);
            AddConfigValue(dict, config, "PoNovaWeight:AzureOpenAI:ApiKey", true);
            AddConfigValue(dict, config, "PoNovaWeight:Google:ClientSecret", true);

            return Results.Ok(dict);
        })
        .WithName("GetDiagnostics")
        .WithTags("Diagnostics")
        .RequireAuthorization() // Should be protected in production
        .Produces<Dictionary<string, string>>(StatusCodes.Status200OK);

        return app;
    }

    private static void AddConfigValue(Dictionary<string, string> dict, IConfiguration config, string key, bool isSecret = false)
    {
        var value = config[key];
        if (value == null) return;
        
        dict[key] = isSecret ? MaskValue(value) : value;
    }

    private static string MaskValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return "EMPTY";
        if (value.Length <= 8) return "****";
        return $"{value[..4]}...{value[^4..]}";
    }
}
