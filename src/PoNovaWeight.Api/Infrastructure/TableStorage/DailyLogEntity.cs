using Azure;
using Azure.Data.Tables;

namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Azure Table Storage entity for daily food journal logs.
/// </summary>
public class DailyLogEntity : TableStorageEntity
{
    /// <summary>
    /// User identifier. Fixed to "dev-user" for MVP (no authentication).
    /// </summary>
    public override string PartitionKey { get; set; } = "dev-user";

    /// <summary>
    /// Date in yyyy-MM-dd format.
    /// </summary>
    public override string RowKey { get; set; } = string.Empty;

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

    // Blood pressure and heart rate tracking (nullable for backward compatibility)
    /// <summary>
    /// Systolic blood pressure in mmHg (70-200 range).
    /// </summary>
    public double? SystolicBP { get; set; }

    /// <summary>
    /// Diastolic blood pressure in mmHg (40-130 range).
    /// </summary>
    public double? DiastolicBP { get; set; }

    /// <summary>
    /// Heart rate in beats per minute (30-220 range).
    /// </summary>
    public int? HeartRate { get; set; }

    /// <summary>
    /// Time of day when BP was recorded: "Morning", "Afternoon", "Evening".
    /// </summary>
    public string? BpReadingTime { get; set; }

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
