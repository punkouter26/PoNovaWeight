using Azure;
using Azure.Data.Tables;

namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Azure Table Storage implementation of user settings repository.
/// </summary>
public class UserSettingsRepository(TableServiceClient tableServiceClient) : IUserSettingsRepository
{
    private const string TableName = "UserSettings";
    private readonly TableClient _tableClient = tableServiceClient.GetTableClient(TableName);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _tableClient.CreateIfNotExistsAsync(cancellationToken);
    }

    public async Task<UserSettingsEntity?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<UserSettingsEntity>(
                "settings",
                userId,
                cancellationToken: cancellationToken);

            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task UpsertAsync(UserSettingsEntity entity, CancellationToken cancellationToken = default)
    {
        await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }
}
