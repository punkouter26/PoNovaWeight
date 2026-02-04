using Azure.Data.Tables;
using FluentAssertions;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Api.Tests.Integration.Fixtures;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Integration tests for DailyLogRepository using real Azurite in Docker.
/// These tests verify actual Table Storage operations, not mocks.
/// </summary>
/// <remarks>
/// Tests are skipped if Docker is not available.
/// Run with: dotnet test --filter "Category=Docker"
/// </remarks>
[Collection("Azurite")]
[Trait("Category", "Docker")]
public class AzuriteRepositoryTests : IAsyncLifetime
{
    private readonly AzuriteFixture _fixture;
    private readonly string _tableName;
    private DailyLogRepository _repository = null!;

    public AzuriteRepositoryTests(AzuriteFixture fixture)
    {
        _fixture = fixture;
        // Unique table name per test run to ensure isolation
        _tableName = $"DailyLogs{Guid.NewGuid():N}".Substring(0, 30);
    }

    public async Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsRunning, _fixture.StartupError ?? "Docker not available");

        var tableClient = new TableServiceClient(_fixture.ConnectionString);
        _repository = new DailyLogRepository(tableClient);
        await _repository.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (_fixture.IsRunning)
        {
            try
            {
                await _fixture.TableServiceClient.DeleteTableAsync("DailyLogs");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [SkippableFact]
    public async Task UpsertAndGet_RoundTrip_ReturnsCorrectData()
    {
        Skip.IfNot(_fixture.IsRunning, _fixture.StartupError);

        // Arrange
        var userId = "test@example.com";
        var date = new DateOnly(2025, 6, 15);
        var entity = new DailyLogEntity
        {
            PartitionKey = userId,
            RowKey = date.ToString("yyyy-MM-dd"),
            Proteins = 5,
            Vegetables = 3,
            Fruits = 2,
            Starches = 1,
            Fats = 2,
            Dairy = 1,
            WaterSegments = 6,
            Weight = 175.5,
            OmadCompliant = true,
            AlcoholConsumed = false
        };

        // Act
        await _repository.UpsertAsync(entity);
        var retrieved = await _repository.GetAsync(userId, date);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Proteins.Should().Be(5);
        retrieved.Vegetables.Should().Be(3);
        retrieved.Weight.Should().Be(175.5);
        retrieved.OmadCompliant.Should().BeTrue();
    }

    [SkippableFact]
    public async Task GetRange_MultipleEntries_ReturnsAllInRange()
    {
        Skip.IfNot(_fixture.IsRunning, _fixture.StartupError);

        // Arrange
        var userId = "range-test@example.com";
        var startDate = new DateOnly(2025, 6, 1);

        // Insert 7 days of data
        for (int i = 0; i < 7; i++)
        {
            var date = startDate.AddDays(i);
            await _repository.UpsertAsync(new DailyLogEntity
            {
                PartitionKey = userId,
                RowKey = date.ToString("yyyy-MM-dd"),
                Proteins = i + 1
            });
        }

        // Act
        var results = await _repository.GetRangeAsync(userId, startDate, startDate.AddDays(6));

        // Assert
        results.Should().HaveCount(7);
        results.Select(r => r.Proteins).Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7 });
    }

    [SkippableFact]
    public async Task Delete_ExistingEntry_RemovesFromStorage()
    {
        Skip.IfNot(_fixture.IsRunning, _fixture.StartupError);

        // Arrange
        var userId = "delete-test@example.com";
        var date = new DateOnly(2025, 6, 20);
        await _repository.UpsertAsync(new DailyLogEntity
        {
            PartitionKey = userId,
            RowKey = date.ToString("yyyy-MM-dd"),
            Proteins = 10
        });

        // Verify it exists
        var beforeDelete = await _repository.GetAsync(userId, date);
        beforeDelete.Should().NotBeNull();

        // Act
        await _repository.DeleteAsync(userId, date);

        // Assert
        var afterDelete = await _repository.GetAsync(userId, date);
        afterDelete.Should().BeNull();
    }

    [SkippableFact]
    public async Task Upsert_ExistingEntry_UpdatesData()
    {
        Skip.IfNot(_fixture.IsRunning, _fixture.StartupError);

        // Arrange
        var userId = "update-test@example.com";
        var date = new DateOnly(2025, 6, 25);

        await _repository.UpsertAsync(new DailyLogEntity
        {
            PartitionKey = userId,
            RowKey = date.ToString("yyyy-MM-dd"),
            Proteins = 5,
            WaterSegments = 4
        });

        // Act - Update with new values
        await _repository.UpsertAsync(new DailyLogEntity
        {
            PartitionKey = userId,
            RowKey = date.ToString("yyyy-MM-dd"),
            Proteins = 10,
            WaterSegments = 8,
            OmadCompliant = true
        });

        var result = await _repository.GetAsync(userId, date);

        // Assert
        result.Should().NotBeNull();
        result!.Proteins.Should().Be(10);
        result.WaterSegments.Should().Be(8);
        result.OmadCompliant.Should().BeTrue();
    }

    [SkippableFact]
    public async Task GetRange_EmptyRange_ReturnsEmptyList()
    {
        Skip.IfNot(_fixture.IsRunning, _fixture.StartupError);

        // Arrange
        var userId = "empty-range@example.com";
        var startDate = new DateOnly(2099, 1, 1); // Far future, no data

        // Act
        var results = await _repository.GetRangeAsync(userId, startDate, startDate.AddDays(7));

        // Assert
        results.Should().BeEmpty();
    }
}
