using FluentAssertions;
using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.Contracts;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Unit;

/// <summary>
/// Consolidated unit tests for IncrementUnit handler using Theory/InlineData
/// to reduce test count while maintaining coverage.
/// </summary>
public class IncrementUnitConsolidatedTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly IncrementUnitHandler _handler;

    public IncrementUnitConsolidatedTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _handler = new IncrementUnitHandler(_repositoryMock.Object);
    }

    /// <summary>
    /// Consolidates increment/decrement tests for all categories into one Theory.
    /// Tests: increment existing, decrement existing, create new, clamp to zero, clamp to max.
    /// </summary>
    [Theory]
    [InlineData(UnitCategory.Proteins, 5, 1, 6, "increment existing")]
    [InlineData(UnitCategory.Vegetables, 3, -1, 2, "decrement existing")]
    [InlineData(UnitCategory.Fruits, 0, 1, 1, "increment from zero")]
    [InlineData(UnitCategory.Starches, 0, -1, 0, "clamp to zero on negative")]
    [InlineData(UnitCategory.Fats, 10, 1, 11, "increment beyond 10 (no max clamp)")]
    [InlineData(UnitCategory.Dairy, 5, 2, 7, "increment by 2")]
    public async Task IncrementUnit_VariousScenarios_HandlesCorrectly(
        UnitCategory category, int initial, int delta, int expected, string scenario)
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        var existingEntity = CreateEntityWithCategory(category, initial);

        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        var command = new IncrementUnitCommand(new IncrementUnitRequest
        {
            Date = date,
            Category = category,
            Delta = delta
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        GetCategoryValue(result, category).Should().Be(expected, because: scenario);
    }

    [Fact]
    public async Task IncrementUnit_NewDate_CreatesEntityWithDefaultsAndIncrement()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 16);
        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyLogEntity?)null);

        var command = new IncrementUnitCommand(new IncrementUnitRequest
        {
            Date = date,
            Category = UnitCategory.Proteins,
            Delta = 1
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - New entity created with increment applied
        result.Proteins.Should().Be(1);
        result.Vegetables.Should().Be(0);
        result.Fruits.Should().Be(0);
        _repositoryMock.Verify(
            r => r.UpsertAsync(It.Is<DailyLogEntity>(e => e.Proteins == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(UnitCategory.Proteins)]
    [InlineData(UnitCategory.Vegetables)]
    [InlineData(UnitCategory.Fruits)]
    [InlineData(UnitCategory.Starches)]
    [InlineData(UnitCategory.Fats)]
    [InlineData(UnitCategory.Dairy)]
    public async Task IncrementUnit_AllCategories_UpsertsCalled(UnitCategory category)
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DailyLogEntity { PartitionKey = "dev-user", RowKey = "2025-01-15" });

        var command = new IncrementUnitCommand(new IncrementUnitRequest
        {
            Date = date,
            Category = category,
            Delta = 1
        });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #region Helpers

    private static DailyLogEntity CreateEntityWithCategory(UnitCategory category, int value)
    {
        var entity = new DailyLogEntity
        {
            PartitionKey = "dev-user",
            RowKey = "2025-01-15"
        };

        switch (category)
        {
            case UnitCategory.Proteins: entity.Proteins = value; break;
            case UnitCategory.Vegetables: entity.Vegetables = value; break;
            case UnitCategory.Fruits: entity.Fruits = value; break;
            case UnitCategory.Starches: entity.Starches = value; break;
            case UnitCategory.Fats: entity.Fats = value; break;
            case UnitCategory.Dairy: entity.Dairy = value; break;
        }

        return entity;
    }

    private static int GetCategoryValue(DailyLogDto dto, UnitCategory category) => category switch
    {
        UnitCategory.Proteins => dto.Proteins,
        UnitCategory.Vegetables => dto.Vegetables,
        UnitCategory.Fruits => dto.Fruits,
        UnitCategory.Starches => dto.Starches,
        UnitCategory.Fats => dto.Fats,
        UnitCategory.Dairy => dto.Dairy,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    #endregion
}
