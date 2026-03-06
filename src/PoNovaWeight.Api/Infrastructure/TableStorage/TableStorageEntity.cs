using Azure;
using Azure.Data.Tables;

namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Base class for all Azure Table Storage entities.
/// Consolidates common ITableEntity properties and methods.
/// </summary>
public abstract class TableStorageEntity : ITableEntity
{
    /// <summary>
    /// Gets or sets the partition key, typically representing a user or logical grouping.
    /// </summary>
    public virtual string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the row key, which uniquely identifies this entity within the partition.
    /// </summary>
    public virtual string RowKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure-managed timestamp for concurrency control.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the Azure-managed ETag for optimistic concurrency control.
    /// </summary>
    public ETag ETag { get; set; }
}
