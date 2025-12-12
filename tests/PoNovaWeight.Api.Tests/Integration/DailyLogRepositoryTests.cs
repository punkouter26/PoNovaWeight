using Azure.Data.Tables;
using PoNovaWeight.Api.Infrastructure.TableStorage;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Integration tests for DailyLogRepository against Azurite.
/// These tests require Azurite to be running locally.
/// Run: azurite --silent --location ./azurite-data
/// </summary>
[Collection("TableStorageIntegration")]
public class DailyLogRepositoryTests : IAsyncLifetime
{
    private const string ConnectionString = "UseDevelopmentStorage=true";
    private const string TestUserId = "integration-test-user";

    private readonly TableServiceClient _tableServiceClient;
    private readonly DailyLogRepository _repository;
    private readonly List<(string PartitionKey, string RowKey)> _createdEntities = new();

    public DailyLogRepositoryTests()
    {
        _tableServiceClient = new TableServiceClient(ConnectionString);
        _repository = new DailyLogRepository(_tableServiceClient);
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _repository.InitializeAsync();
        }
        catch (Exception ex)
        {
            // Skip tests if Azurite is not running
            throw new SkipException($"Azurite not available: {ex.Message}. Start Azurite with: azurite --silent");
        }
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        var tableClient = _tableServiceClient.GetTableClient("DailyLogs");
        foreach (var (partitionKey, rowKey) in _createdEntities)
        {
            try
            {
                await tableClient.DeleteEntityAsync(partitionKey, rowKey);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [SkippableFact]
    public async Task UpsertAsync_NewEntity_CreatesEntity()
    {
        Skip.If(!IsAzuriteAvailable(), "Azurite is not running");

        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var entity = CreateTestEntity(TestUserId, date);
        _createdEntities.Add((entity.PartitionKey, entity.RowKey));

        // Act
        await _repository.UpsertAsync(entity);

        // Assert
        var retrieved = await _repository.GetAsync(TestUserId, date);
        Assert.NotNull(retrieved);
        Assert.Equal(entity.PartitionKey, retrieved.PartitionKey);
        Assert.Equal(entity.RowKey, retrieved.RowKey);
        Assert.Equal(entity.Proteins, retrieved.Proteins);
    }

    [SkippableFact]
    public async Task UpsertAsync_ExistingEntity_UpdatesEntity()
    {
        Skip.If(!IsAzuriteAvailable(), "Azurite is not running");

        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var entity = CreateTestEntity(TestUserId, date);
        _createdEntities.Add((entity.PartitionKey, entity.RowKey));
        await _repository.UpsertAsync(entity);

        // Act - Update with new values
        entity.Proteins = 5;
        entity.Vegetables = 3;
        await _repository.UpsertAsync(entity);

        // Assert
        var retrieved = await _repository.GetAsync(TestUserId, date);
        Assert.NotNull(retrieved);
        Assert.Equal(5, retrieved.Proteins);
        Assert.Equal(3, retrieved.Vegetables);
    }

    [SkippableFact]
    public async Task GetAsync_NonExistentEntity_ReturnsNull()
    {
        Skip.If(!IsAzuriteAvailable(), "Azurite is not running");

        // Arrange
        var nonExistentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10));

        // Act
        var result = await _repository.GetAsync(TestUserId, nonExistentDate);

        // Assert
        Assert.Null(result);
    }

    [SkippableFact]
    public async Task GetRangeAsync_WithMultipleEntities_ReturnsOrderedList()
    {
        Skip.If(!IsAzuriteAvailable(), "Azurite is not running");

        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var dates = new[] { startDate, startDate.AddDays(1), startDate.AddDays(2), endDate };
        foreach (var date in dates)
        {
            var entity = CreateTestEntity(TestUserId, date);
            _createdEntities.Add((entity.PartitionKey, entity.RowKey));
            await _repository.UpsertAsync(entity);
        }

        // Act
        var results = await _repository.GetRangeAsync(TestUserId, startDate, endDate);

        // Assert
        Assert.Equal(4, results.Count);
        Assert.Equal(startDate.ToString("yyyy-MM-dd"), results[0].RowKey);
        Assert.Equal(endDate.ToString("yyyy-MM-dd"), results[^1].RowKey);
    }

    [SkippableFact]
    public async Task GetRangeAsync_EmptyRange_ReturnsEmptyList()
    {
        Skip.If(!IsAzuriteAvailable(), "Azurite is not running");

        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5));
        var endDate = startDate.AddDays(7);

        // Act
        var results = await _repository.GetRangeAsync(TestUserId, startDate, endDate);

        // Assert
        Assert.Empty(results);
    }

    [SkippableFact]
    public async Task GetRangeAsync_MultipleUsers_ReturnsOnlyRequestedUser()
    {
        Skip.If(!IsAzuriteAvailable(), "Azurite is not running");

        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var entity1 = CreateTestEntity("user-1", date);
        var entity2 = CreateTestEntity("user-2", date);
        _createdEntities.Add((entity1.PartitionKey, entity1.RowKey));
        _createdEntities.Add((entity2.PartitionKey, entity2.RowKey));
        await _repository.UpsertAsync(entity1);
        await _repository.UpsertAsync(entity2);

        // Act
        var results = await _repository.GetRangeAsync("user-1", date.AddDays(-1), date);

        // Assert
        Assert.Single(results);
        Assert.Equal("user-1", results[0].PartitionKey);
    }

    private static DailyLogEntity CreateTestEntity(string userId, DateOnly date)
    {
        return new DailyLogEntity
        {
            PartitionKey = userId,
            RowKey = date.ToString("yyyy-MM-dd"),
            Proteins = 2,
            Vegetables = 2,
            Fruits = 1,
            Starches = 1,
            Fats = 1,
            Dairy = 1,
            WaterSegments = 4
        };
    }

    private static bool IsAzuriteAvailable()
    {
        try
        {
            var client = new TableServiceClient(ConnectionString);
            // Try to list tables - this will fail if Azurite isn't running
            var tables = client.Query().Take(1).ToList();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Custom exception for skippable tests when Azurite is not available.
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}

/// <summary>
/// Attribute for skippable facts.
/// </summary>
public class SkippableFactAttribute : FactAttribute
{
}

/// <summary>
/// Static helper for skipping tests.
/// </summary>
public static class Skip
{
    public static void If(bool condition, string reason)
    {
        if (condition)
        {
            throw new SkipException(reason);
        }
    }
}
