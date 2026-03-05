using MediatR;
using PoNovaWeight.Api.Infrastructure;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.Predictions;

/// <summary>
/// Endpoints for AI prediction operations.
/// </summary>
public static class Endpoints
{
    public static void MapPredictionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/predictions")
            .WithTags("Predictions")
            .RequireAuthorization();

        // POST /api/predictions/blood-pressure
        group.MapPost("/blood-pressure", async (
            BpPredictionRequestDto request,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new PredictBloodPressureCommand(request, httpContext.GetUserId()), 
                cancellationToken);
            return Results.Ok(result);
        })
        .WithName("PredictBloodPressure")
        .WithSummary("Get AI-powered blood pressure predictions based on planned lifestyle changes")
        .Produces<BpPredictionResultDto>(StatusCodes.Status200OK);
    }
}
