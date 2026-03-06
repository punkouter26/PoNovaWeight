using Azure;
using Azure.Data.Tables;

namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Azure Table Storage entity for user settings.
/// </summary>
public class UserSettingsEntity : TableStorageEntity
{
    /// <summary>
    /// Fixed partition key for all user settings.
    /// </summary>
    public override string PartitionKey { get; set; } = "settings";

    /// <summary>
    /// User identifier as row key.
    /// </summary>
    public override string RowKey { get; set; } = string.Empty;

    // BP and HR goals
    public int? TargetSystolic { get; set; }
    public int? TargetDiastolic { get; set; }
    public int? TargetHeartRate { get; set; }

    /// <summary>
    /// Creates an entity for the specified user.
    /// </summary>
    public static UserSettingsEntity Create(string userId) => new()
    {
        PartitionKey = "settings",
        RowKey = userId
    };
}
