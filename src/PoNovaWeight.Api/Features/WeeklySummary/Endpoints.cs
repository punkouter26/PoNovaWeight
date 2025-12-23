using MediatR;
using PoNovaWeight.Shared.DTOs;
using System.Security.Claims;

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
        group.MapGet("/{date}", async (DateOnly date, HttpContext httpContext, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.Email) ?? throw new UnauthorizedAccessException();
            var result = await mediator.Send(new GetWeeklySummaryQuery(date, userId), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetWeeklySummary")
        .WithSummary("Get weekly summary for a date within the week")
        .Produces<WeeklySummaryDto>(StatusCodes.Status200OK);
    }
}
