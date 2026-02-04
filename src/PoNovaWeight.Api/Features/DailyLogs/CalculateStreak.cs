using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Query to calculate the current OMAD streak.
/// </summary>
public record CalculateStreakQuery(string UserId = "dev-user") : IRequest<StreakDto>;

/// <summary>
/// Handler for CalculateStreakQuery.
/// Calculates streak from stored logs on-demand.
/// Requires consecutive days - gaps break the streak.
/// </summary>
public sealed class CalculateStreakHandler(IDailyLogRepository repository, TimeProvider timeProvider) : IRequestHandler<CalculateStreakQuery, StreakDto>
{
    public async Task<StreakDto> Handle(CalculateStreakQuery request, CancellationToken cancellationToken)
    {
        // Query the last 365 days of logs
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var startDate = today.AddDays(-365);

        var entities = await repository.GetRangeAsync(
            request.UserId,
            startDate,
            today,
            cancellationToken);

        if (entities.Count == 0)
        {
            return new StreakDto { CurrentStreak = 0, StreakStartDate = null };
        }

        // Create a dictionary for O(1) lookup by date
        var logsByDate = entities.ToDictionary(e => e.GetDate());

        // Calculate streak starting from today, going backwards
        // Requires consecutive days with OmadCompliant = true
        int streak = 0;
        DateOnly? streakStartDate = null;
        var expectedDate = today;

        while (expectedDate >= startDate)
        {
            if (!logsByDate.TryGetValue(expectedDate, out var log))
            {
                // No log for this date - gap in data, break streak
                break;
            }

            if (!log.OmadCompliant.HasValue)
            {
                // Null (not logged) - treat as gap, break streak
                break;
            }

            if (log.OmadCompliant == true)
            {
                // OMAD compliant on this consecutive day - increment streak
                streak++;
                streakStartDate = expectedDate;
                expectedDate = expectedDate.AddDays(-1);
            }
            else
            {
                // OmadCompliant = false - explicitly broke streak
                break;
            }
        }

        return new StreakDto
        {
            CurrentStreak = streak,
            StreakStartDate = streakStartDate
        };
    }
}
