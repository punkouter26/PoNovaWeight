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
