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

        // GET /api/daily-logs/monthly/{year}/{month}
        group.MapGet("/monthly/{year:int}/{month:int}", async (int year, int month, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetMonthlyLogsQuery(year, month), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetMonthlyLogs")
        .WithSummary("Get daily log summaries for a specific month")
        .Produces<MonthlyLogsDto>(StatusCodes.Status200OK);

        // GET /api/daily-logs/streak
        group.MapGet("/streak", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CalculateStreakQuery(), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetStreak")
        .WithSummary("Calculate the current OMAD streak")
        .Produces<StreakDto>(StatusCodes.Status200OK);

        // GET /api/daily-logs/trends?days=30
        group.MapGet("/trends", async (int days, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetWeightTrendsQuery(days), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetWeightTrends")
        .WithSummary("Get weight trend data for the specified number of days")
        .Produces<WeightTrendsDto>(StatusCodes.Status200OK);

        // GET /api/daily-logs/alcohol-correlation?days=90
        group.MapGet("/alcohol-correlation", async (int days, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetAlcoholCorrelationQuery(days), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetAlcoholCorrelation")
        .WithSummary("Get weight correlation between alcohol and non-alcohol days")
        .Produces<AlcoholCorrelationDto>(StatusCodes.Status200OK);

        // DELETE /api/daily-logs/{date}
        group.MapDelete("/{date}", async (DateOnly date, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var deleted = await mediator.Send(new DeleteDailyLogCommand(date), cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteDailyLog")
        .WithSummary("Delete a daily log entry")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
