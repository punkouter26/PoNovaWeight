using MediatR;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Endpoints for daily log operations.
/// </summary>
public static class Endpoints
{
    public static void MapDailyLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/daily-logs")
            .WithTags("Daily Logs");

        // GET /api/daily-logs/{date}
        group.MapGet("/{date}", async (DateOnly date, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetDailyLogQuery(date), cancellationToken);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetDailyLog")
        .WithSummary("Get daily log for a specific date")
        .Produces<DailyLogDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // PUT /api/daily-logs
        group.MapPut("/", async (DailyLogDto dailyLog, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpsertDailyLogCommand(dailyLog), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("UpsertDailyLog")
        .WithSummary("Create or update a daily log")
        .Produces<DailyLogDto>(StatusCodes.Status200OK);

        // POST /api/daily-logs/increment
        group.MapPost("/increment", async (IncrementUnitRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new IncrementUnitCommand(request), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("IncrementUnit")
        .WithSummary("Increment or decrement a unit count for a category")
        .Produces<DailyLogDto>(StatusCodes.Status200OK);

        // POST /api/daily-logs/water
        group.MapPost("/water", async (UpdateWaterRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpdateWaterCommand(request), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("UpdateWater")
        .WithSummary("Update water intake segments for a day")
        .Produces<DailyLogDto>(StatusCodes.Status200OK);
    }
}
