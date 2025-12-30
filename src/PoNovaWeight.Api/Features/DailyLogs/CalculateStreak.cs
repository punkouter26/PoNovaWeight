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
/// Only explicit OmadCompliant = false breaks streak; null (unlogged) does not.
/// </summary>
public class CalculateStreakHandler(IDailyLogRepository repository) : IRequestHandler<CalculateStreakQuery, StreakDto>
{
    public async Task<StreakDto> Handle(CalculateStreakQuery request, CancellationToken cancellationToken)
    {
        // Query the last 365 days of logs
        var today = DateOnly.FromDateTime(DateTime.Today);
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

        // Sort by date descending (most recent first)
        var sortedLogs = entities
            .OrderByDescending(e => e.RowKey)
            .ToList();

        // Calculate streak
        int streak = 0;
        DateOnly? streakStartDate = null;
        bool foundFirstCompliant = false;

        foreach (var log in sortedLogs)
        {
            var date = log.GetDate();

            if (!log.OmadCompliant.HasValue)
            {
                // Null (unlogged) - skip, doesn't break streak
                continue;
            }

            if (log.OmadCompliant == true)
            {
                // OMAD compliant - increment streak
                streak++;
                streakStartDate = date;
                foundFirstCompliant = true;
            }
            else
            {
                // OmadCompliant = false - breaks the streak
                if (foundFirstCompliant)
                {
                    // We already have some compliant days, stop counting
                    break;
                }
                else
                {
                    // Most recent logged day is non-compliant, streak is 0
                    return new StreakDto { CurrentStreak = 0, StreakStartDate = null };
                }
            }
        }

        return new StreakDto
        {
            CurrentStreak = streak,
            StreakStartDate = streakStartDate
        };
    }
}
