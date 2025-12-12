using Azure;
using Azure.Data.Tables;

namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Azure Table Storage implementation of daily log repository.
/// </summary>
public class DailyLogRepository : IDailyLogRepository
{
    private const string TableName = "DailyLogs";
    private readonly TableClient _tableClient;

    public DailyLogRepository(TableServiceClient tableServiceClient)
    {
        _tableClient = tableServiceClient.GetTableClient(TableName);
    }

    /// <summary>
    /// Ensures the table exists. Should be called at startup.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _tableClient.CreateIfNotExistsAsync(cancellationToken);
    }

    public async Task<DailyLogEntity?> GetAsync(string userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var rowKey = date.ToString("yyyy-MM-dd");

        try
        {
            var response = await _tableClient.GetEntityAsync<DailyLogEntity>(
                userId,
                rowKey,
                cancellationToken: cancellationToken);

            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<DailyLogEntity>> GetRangeAsync(
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var startRowKey = startDate.ToString("yyyy-MM-dd");
        var endRowKey = endDate.ToString("yyyy-MM-dd");

        var filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {userId} and RowKey ge {startRowKey} and RowKey le {endRowKey}");

        var results = new List<DailyLogEntity>();

        await foreach (var entity in _tableClient.QueryAsync<DailyLogEntity>(filter, cancellationToken: cancellationToken))
        {
            results.Add(entity);
        }

        // Sort by date (RowKey) to ensure consistent ordering
        return results.OrderBy(e => e.RowKey).ToList();
    }

    public async Task UpsertAsync(DailyLogEntity entity, CancellationToken cancellationToken = default)
    {
        await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }
}
