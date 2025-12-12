namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Repository interface for daily log persistence operations.
/// </summary>
public interface IDailyLogRepository
{
    /// <summary>
    /// Gets a daily log for a specific user and date.
    /// Returns null if no entry exists.
    /// </summary>
    Task<DailyLogEntity?> GetAsync(string userId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all daily logs for a user within a date range (inclusive).
    /// </summary>
    Task<IReadOnlyList<DailyLogEntity>> GetRangeAsync(string userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or replaces a daily log entry.
    /// </summary>
    Task UpsertAsync(DailyLogEntity entity, CancellationToken cancellationToken = default);
}
