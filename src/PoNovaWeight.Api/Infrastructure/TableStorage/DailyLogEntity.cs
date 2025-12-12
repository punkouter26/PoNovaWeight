using Azure;
using Azure.Data.Tables;

namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Azure Table Storage entity for daily food journal logs.
/// </summary>
public class DailyLogEntity : ITableEntity
{
    /// <summary>
    /// User identifier. Fixed to "dev-user" for MVP (no authentication).
    /// </summary>
    public string PartitionKey { get; set; } = "dev-user";

    /// <summary>
    /// Date in yyyy-MM-dd format.
    /// </summary>
    public string RowKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure-managed timestamp for concurrency.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Azure-managed ETag for optimistic concurrency.
    /// </summary>
    public ETag ETag { get; set; }

    public int Proteins { get; set; }
    public int Vegetables { get; set; }
    public int Fruits { get; set; }
    public int Starches { get; set; }
    public int Fats { get; set; }
    public int Dairy { get; set; }
    public int WaterSegments { get; set; }

    // OMAD tracking fields (nullable for backward compatibility)
    /// <summary>
    /// Weight in pounds (50-500 lbs range).
    /// </summary>
    public double? Weight { get; set; }

    /// <summary>
    /// True if OMAD (One Meal A Day) was followed, null if not logged.
    /// </summary>
    public bool? OmadCompliant { get; set; }

    /// <summary>
    /// True if alcohol was consumed, null if not logged.
    /// </summary>
    public bool? AlcoholConsumed { get; set; }

    /// <summary>
    /// Creates an empty entity for the specified user and date.
    /// </summary>
    public static DailyLogEntity Create(string userId, DateOnly date) => new()
    {
        PartitionKey = userId,
        RowKey = date.ToString("yyyy-MM-dd")
    };

    /// <summary>
    /// Parses the RowKey to get the date.
    /// </summary>
    public DateOnly GetDate() => DateOnly.ParseExact(RowKey, "yyyy-MM-dd");
}
