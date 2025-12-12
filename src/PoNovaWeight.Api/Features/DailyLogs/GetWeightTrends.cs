using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Query to get weight trend data for a specified number of days.
/// </summary>
public record GetWeightTrendsQuery(int Days = 30, string UserId = "dev-user") : IRequest<WeightTrendsDto>;

/// <summary>
/// Handler for GetWeightTrendsQuery.
/// Implements carry-forward gap-filling for missing days.
/// </summary>
public class GetWeightTrendsHandler : IRequestHandler<GetWeightTrendsQuery, WeightTrendsDto>
{
    private readonly IDailyLogRepository _repository;

    public GetWeightTrendsHandler(IDailyLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<WeightTrendsDto> Handle(GetWeightTrendsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = today.AddDays(-request.Days + 1);

        var entities = await _repository.GetRangeAsync(
            request.UserId,
            startDate,
            today,
            cancellationToken);

        if (entities.Count == 0)
        {
            return new WeightTrendsDto
            {
                DataPoints = [],
                TotalDaysLogged = 0,
                WeightChange = null
            };
        }

        // Create a dictionary for quick lookup
        var logsByDate = entities.ToDictionary(e => e.GetDate());

        // Build data points with carry-forward
        var dataPoints = new List<TrendDataPoint>();
        decimal? lastKnownWeight = null;
        DateOnly? firstWeightDate = null;
        decimal? firstWeight = null;
        decimal? lastWeight = null;

        for (var date = startDate; date <= today; date = date.AddDays(1))
        {
            if (logsByDate.TryGetValue(date, out var log) && log.Weight.HasValue)
            {
                // Actual logged weight
                var weight = (decimal)log.Weight.Value;
                dataPoints.Add(new TrendDataPoint
                {
                    Date = date,
                    Weight = weight,
                    IsCarryForward = false,
                    AlcoholConsumed = log.AlcoholConsumed
                });

                lastKnownWeight = weight;
                lastWeight = weight;

                if (!firstWeightDate.HasValue)
                {
                    firstWeightDate = date;
                    firstWeight = weight;
                }
            }
            else
            {
                // No weight logged - carry forward if we have a previous weight
                dataPoints.Add(new TrendDataPoint
                {
                    Date = date,
                    Weight = lastKnownWeight,
                    IsCarryForward = lastKnownWeight.HasValue,
                    AlcoholConsumed = logsByDate.TryGetValue(date, out var l) ? l.AlcoholConsumed : null
                });
            }
        }

        // Calculate weight change (last - first)
        decimal? weightChange = (firstWeight.HasValue && lastWeight.HasValue)
            ? lastWeight.Value - firstWeight.Value
            : null;

        // Count actual days logged (with weight)
        var totalDaysLogged = entities.Count(e => e.Weight.HasValue);

        return new WeightTrendsDto
        {
            DataPoints = dataPoints,
            TotalDaysLogged = totalDaysLogged,
            WeightChange = weightChange
        };
    }
}
