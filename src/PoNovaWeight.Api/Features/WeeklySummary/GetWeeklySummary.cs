using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;
using PoNovaWeight.Shared.Validation;

namespace PoNovaWeight.Api.Features.WeeklySummary;

/// <summary>
/// Query to get the weekly summary for a date within the week.
/// </summary>
public record GetWeeklySummaryQuery(DateOnly Date, string UserId = "dev-user") : IRequest<WeeklySummaryDto>;

/// <summary>
/// Handler for GetWeeklySummaryQuery.
/// </summary>
public class GetWeeklySummaryHandler : IRequestHandler<GetWeeklySummaryQuery, WeeklySummaryDto>
{
    private readonly IDailyLogRepository _repository;

    public GetWeeklySummaryHandler(IDailyLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<WeeklySummaryDto> Handle(GetWeeklySummaryQuery request, CancellationToken cancellationToken)
    {
        // Calculate week boundaries (Sunday to Saturday)
        var (weekStart, weekEnd) = DailyLogDtoValidator.GetWeekBounds(request.Date);

        // Fetch all daily logs for the week
        var entities = await _repository.GetRangeAsync(request.UserId, weekStart, weekEnd, cancellationToken);

        // Create a dictionary for quick lookup
        var logsByDate = entities.ToDictionary(e => e.GetDate());

        // Build the list of daily logs for the week (7 days)
        var days = new List<DailyLogDto>();
        for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
        {
            if (logsByDate.TryGetValue(date, out var entity))
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
                days.Add(DailyLogDto.Empty(date));
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
