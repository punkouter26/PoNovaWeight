using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Query to get blood pressure trend data for a specified number of days.
/// </summary>
public record GetBloodPressureTrendsQuery(int Days = 30, string UserId = "dev-user") : IRequest<BloodPressureTrendsDto>;

/// <summary>
/// Handler for GetBloodPressureTrendsQuery.
/// Implements carry-forward gap-filling for missing days.
/// </summary>
public sealed class GetBloodPressureTrendsHandler(IDailyLogRepository repository, TimeProvider timeProvider) 
    : IRequestHandler<GetBloodPressureTrendsQuery, BloodPressureTrendsDto>
{
    public async Task<BloodPressureTrendsDto> Handle(GetBloodPressureTrendsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var startDate = today.AddDays(-request.Days + 1);

        var entities = await repository.GetRangeAsync(
            request.UserId,
            startDate,
            today,
            cancellationToken);

        if (entities.Count == 0)
        {
            return new BloodPressureTrendsDto
            {
                DataPoints = [],
                StartDate = startDate,
                EndDate = today
            };
        }

        // Create a dictionary for quick lookup
        var logsByDate = entities.ToDictionary(e => e.GetDate());

        // Build data points with carry-forward
        var dataPoints = new List<BpTrendDataPoint>();
        decimal? lastKnownSystolic = null;
        decimal? lastKnownDiastolic = null;
        int? lastKnownHeartRate = null;
        string? lastKnownReadingTime = null;

        for (var date = startDate; date <= today; date = date.AddDays(1))
        {
            if (logsByDate.TryGetValue(date, out var log) && 
                (log.SystolicBP.HasValue || log.DiastolicBP.HasValue))
            {
                // Actual logged BP
                var systolic = log.SystolicBP.HasValue ? (decimal?)log.SystolicBP.Value : null;
                var diastolic = log.DiastolicBP.HasValue ? (decimal?)log.DiastolicBP.Value : null;
                
                dataPoints.Add(new BpTrendDataPoint
                {
                    Date = date,
                    Systolic = systolic,
                    Diastolic = diastolic,
                    HeartRate = log.HeartRate,
                    ReadingTime = log.BpReadingTime,
                    IsCarryForward = false,
                    AlcoholConsumed = log.AlcoholConsumed
                });

                // Update last known values
                if (systolic.HasValue) lastKnownSystolic = systolic;
                if (diastolic.HasValue) lastKnownDiastolic = diastolic;
                if (log.HeartRate.HasValue) lastKnownHeartRate = log.HeartRate;
                if (!string.IsNullOrWhiteSpace(log.BpReadingTime)) lastKnownReadingTime = log.BpReadingTime;
            }
            else
            {
                // No BP logged - carry forward if we have previous values
                var hasCarryForward = lastKnownSystolic.HasValue || lastKnownDiastolic.HasValue;
                
                dataPoints.Add(new BpTrendDataPoint
                {
                    Date = date,
                    Systolic = lastKnownSystolic,
                    Diastolic = lastKnownDiastolic,
                    HeartRate = lastKnownHeartRate,
                    ReadingTime = lastKnownReadingTime,
                    IsCarryForward = hasCarryForward,
                    AlcoholConsumed = logsByDate.TryGetValue(date, out var l) ? l.AlcoholConsumed : null
                });
            }
        }

        return new BloodPressureTrendsDto
        {
            DataPoints = dataPoints,
            StartDate = startDate,
            EndDate = today
        };
    }
}
