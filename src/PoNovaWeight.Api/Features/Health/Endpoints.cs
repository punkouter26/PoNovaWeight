namespace PoNovaWeight.Api.Features.Health;

/// <summary>
/// Health check endpoints for monitoring.
/// </summary>
public static class Endpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/health")
            .WithTags("Health");

        group.MapGet("/", () => Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTimeOffset.UtcNow
        }))
        .WithName("GetHealth")
        .WithSummary("Health check endpoint")
        .Produces<object>(StatusCodes.Status200OK);
    }
}
