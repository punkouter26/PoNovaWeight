using System.Diagnostics;
using Serilog.Context;

namespace PoNovaWeight.Api.Infrastructure;

/// <summary>
/// Middleware that extracts or generates a correlation ID for distributed tracing.
/// Correlation ID is propagated through the request/response headers and enriched in Serilog logs.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeaderKey = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ExtractOrGenerateCorrelationId(context);
        
        // Add correlation ID to Serilog LogContext for all downstream logs
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Set correlation ID in Activity for distributed tracing
            Activity.Current?.SetTag("correlation_id", correlationId);
            
            // Add to response headers
            context.Response.Headers[CorrelationIdHeaderKey] = correlationId;
            
            await next(context);
        }
    }

    private static string ExtractOrGenerateCorrelationId(HttpContext context)
    {
        const string correlationIdQueryParam = "correlation_id";

        // 1. Try to extract from request header
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderKey, out var correlationIdHeader))
        {
            return correlationIdHeader.ToString();
        }

        // 2. Try to extract from query parameter
        if (context.Request.Query.TryGetValue(correlationIdQueryParam, out var correlationIdQuery))
        {
            return correlationIdQuery.ToString();
        }

        // 3. Check if Activity already exists (parent request span)
        if (Activity.Current?.Id is not null)
        {
            return Activity.Current.Id;
        }

        // 4. Generate new correlation ID
        return Guid.NewGuid().ToString("N");
    }
}
