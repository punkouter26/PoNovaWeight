using Azure;
using Azure.Data.Tables;

namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Azure Table Storage implementation of user repository.
/// </summary>
public class UserRepository : IUserRepository
{
    private const string TableName = "Users";
    private readonly TableClient _tableClient;

    public UserRepository(TableServiceClient tableServiceClient)
    {
        _tableClient = tableServiceClient.GetTableClient(TableName);
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _tableClient.CreateIfNotExistsAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UserEntity?> GetAsync(string email, CancellationToken cancellationToken = default)
    {
        var partitionKey = UserEntity.NormalizeEmail(email);

        try
        {
            var response = await _tableClient.GetEntityAsync<UserEntity>(
                partitionKey,
                "profile",
                cancellationToken: cancellationToken);

            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(UserEntity entity, CancellationToken cancellationToken = default)
    {
        await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }
}
