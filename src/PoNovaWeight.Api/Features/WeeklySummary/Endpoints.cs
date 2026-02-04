using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;
using PoNovaWeight.Shared.Validation;
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
        // Direct handler - no MediatR overhead for single-use query
        group.MapGet("/{date}", async (DateOnly date, HttpContext httpContext, IDailyLogRepository repository, CancellationToken cancellationToken) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.Email) ?? throw new UnauthorizedAccessException();
            var result = await GetWeeklySummaryAsync(repository, date, userId, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetWeeklySummary")
        .WithSummary("Get weekly summary for a date within the week")
        .Produces<WeeklySummaryDto>(StatusCodes.Status200OK);
    }

    private static async Task<WeeklySummaryDto> GetWeeklySummaryAsync(
        IDailyLogRepository repository,
        DateOnly date,
        string userId,
        CancellationToken cancellationToken)
    {
        // Calculate week boundaries (Sunday to Saturday)
        var (weekStart, weekEnd) = DailyLogDtoValidator.GetWeekBounds(date);

        // Fetch all daily logs for the week
        var entities = await repository.GetRangeAsync(userId, weekStart, weekEnd, cancellationToken);

        // Create a dictionary for quick lookup
        var logsByDate = entities.ToDictionary(e => e.GetDate());

        // Build the list of daily logs for the week (7 days)
        var days = new List<DailyLogDto>();
        for (var d = weekStart; d <= weekEnd; d = d.AddDays(1))
        {
            if (logsByDate.TryGetValue(d, out var entity))
            {
                days.Add(new DailyLogDto
                {
                    Date = entity.GetDate(),
                    Proteins = entity.Proteins,
                    Vegetables = entity.Vegetables,
                    Fruits = entity.Fruits,
                    Starches = entity.Starches,
                    Fats = entity.Fats,
                    Dairy = entity.Dairy,
                    WaterSegments = entity.WaterSegments
                });
            }
            else
            {
                days.Add(DailyLogDto.Empty(d));
            }
        }

        return new WeeklySummaryDto
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Days = days
        };
    }
}
