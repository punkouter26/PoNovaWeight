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
/// Allows a single missing or unlogged day grace period.
/// </summary>
public sealed class CalculateStreakHandler(IDailyLogRepository repository, TimeProvider timeProvider) : IRequestHandler<CalculateStreakQuery, StreakDto>
{
    private const int AllowedGapDays = 1;

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

        // Calculate streak starting from today, going backwards.
        // A single missing/unlogged day is tolerated once streak has started.
        int streak = 0;
        DateOnly? streakStartDate = null;
        var expectedDate = today;
        var remainingGapDays = AllowedGapDays;

        while (expectedDate >= startDate)
        {
            if (!logsByDate.TryGetValue(expectedDate, out var log))
            {
                // If the streak has started, tolerate a limited number of data gaps.
                if (streak > 0 && remainingGapDays > 0)
                {
                    remainingGapDays--;
                    expectedDate = expectedDate.AddDays(-1);
                    continue;
                }

                break;
            }

            if (!log.OmadCompliant.HasValue)
            {
                if (streak > 0 && remainingGapDays > 0)
                {
                    remainingGapDays--;
                    expectedDate = expectedDate.AddDays(-1);
                    continue;
                }

                break;
            }

            if (log.OmadCompliant == true)
            {
                // OMAD compliant day extends the active streak.
                streak++;
                streakStartDate = expectedDate;
                expectedDate = expectedDate.AddDays(-1);
            }
            else
            {
                // OmadCompliant = false explicitly breaks streak.
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
