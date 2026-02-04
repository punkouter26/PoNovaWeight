using Azure.Data.Tables;
using Testcontainers.Azurite;

namespace PoNovaWeight.Api.Tests.Integration.Fixtures;

/// <summary>
/// xUnit fixture that spins up Azurite in Docker for true integration testing.
/// Implements IAsyncLifetime for container lifecycle management.
/// </summary>
/// <remarks>
/// Requires Docker to be running. Tests using this fixture will be skipped
/// if Docker is not available (via Xunit.SkippableFact).
/// </remarks>
public class AzuriteFixture : IAsyncLifetime
{
    private readonly AzuriteContainer _container;

    /// <summary>
    /// Connection string for the running Azurite container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// TableServiceClient configured to use the Azurite container.
    /// </summary>
    public TableServiceClient TableServiceClient { get; private set; } = null!;

    /// <summary>
    /// Indicates whether the container started successfully.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Error message if container failed to start (for skip messages).
    /// </summary>
    public string? StartupError { get; private set; }

    public AzuriteFixture()
    {
        // Configure Azurite container with table service enabled
        _container = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
            TableServiceClient = new TableServiceClient(ConnectionString);
            IsRunning = true;
        }
        catch (Exception ex)
        {
            StartupError = $"Docker/Azurite not available: {ex.Message}";
            IsRunning = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (IsRunning)
        {
            await _container.StopAsync();
        }
    }

    /// <summary>
    /// Creates a fresh table for each test to ensure isolation.
    /// </summary>
    public async Task<TableClient> CreateTestTableAsync(string tableName)
    {
        var tableClient = TableServiceClient.GetTableClient(tableName);
        await tableClient.CreateIfNotExistsAsync();
        return tableClient;
    }

    /// <summary>
    /// Cleans up a test table after use.
    /// </summary>
    public async Task DeleteTestTableAsync(string tableName)
    {
        await TableServiceClient.DeleteTableAsync(tableName);
    }
}

/// <summary>
/// Collection definition for tests that share the Azurite container.
/// Using a collection allows container reuse across test classes.
/// </summary>
[CollectionDefinition("Azurite")]
public class AzuriteCollection : ICollectionFixture<AzuriteFixture>
{
    // This class has no code; it's just a marker for xUnit.
}
