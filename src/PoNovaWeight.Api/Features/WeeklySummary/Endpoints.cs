using MediatR;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.WeeklySummary;

/// <summary>
/// Endpoints for weekly summary operations.
/// </summary>
public static class Endpoints
{
    public static void MapWeeklySummaryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weekly-summary")
            .WithTags("Weekly Summary");

        // GET /api/weekly-summary/{date}
        group.MapGet("/{date}", async (DateOnly date, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetWeeklySummaryQuery(date), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetWeeklySummary")
        .WithSummary("Get weekly summary for a date within the week")
        .Produces<WeeklySummaryDto>(StatusCodes.Status200OK);
    }
}
