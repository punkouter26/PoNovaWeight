using FluentAssertions;
using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.Contracts;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Unit;

public class IncrementUnitHandlerTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly IncrementUnitHandler _handler;

    public IncrementUnitHandlerTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _handler = new IncrementUnitHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task IncrementUnit_ValidCategory_UpdatesCount()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        var existingEntity = new DailyLogEntity
        {
            PartitionKey = "dev-user",
            RowKey = "2025-01-15",
            Proteins = 5,
            Vegetables = 2,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0
        };

        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        var request = new IncrementUnitRequest
        {
            Date = date,
            Category = UnitCategory.Proteins,
            Delta = 1
        };
        var command = new IncrementUnitCommand(request);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Proteins.Should().Be(6);
        _repositoryMock.Verify(r => r.UpsertAsync(It.Is<DailyLogEntity>(e => e.Proteins == 6), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IncrementUnit_NewDate_CreatesNewEntity()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 16);

        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyLogEntity?)null);

        var request = new IncrementUnitRequest
        {
            Date = date,
            Category = UnitCategory.Vegetables,
            Delta = 1
        };
        var command = new IncrementUnitCommand(request);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Vegetables.Should().Be(1);
        result.Proteins.Should().Be(0);
        _repositoryMock.Verify(r => r.UpsertAsync(It.Is<DailyLogEntity>(e => e.Vegetables == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IncrementUnit_Decrement_DecreasesCount()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        var existingEntity = new DailyLogEntity
        {
            PartitionKey = "dev-user",
            RowKey = "2025-01-15",
            Proteins = 5
        };

        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        var request = new IncrementUnitRequest
        {
            Date = date,
            Category = UnitCategory.Proteins,
            Delta = -1
        };
        var command = new IncrementUnitCommand(request);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Proteins.Should().Be(4);
    }

    [Fact]
    public async Task IncrementUnit_DecrementBelowZero_ClampsToZero()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        var existingEntity = new DailyLogEntity
        {
            PartitionKey = "dev-user",
            RowKey = "2025-01-15",
            Proteins = 0
        };

        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        var request = new IncrementUnitRequest
        {
            Date = date,
            Category = UnitCategory.Proteins,
            Delta = -1
        };
        var command = new IncrementUnitCommand(request);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Proteins.Should().Be(0);
    }

    [Fact]
    public async Task IncrementUnit_AllCategories_IncrementCorrectly()
    {
        // Consolidates 6 category tests into one
        var categories = new[] { UnitCategory.Proteins, UnitCategory.Vegetables, UnitCategory.Fruits, UnitCategory.Starches, UnitCategory.Fats, UnitCategory.Dairy };

        foreach (var category in categories)
        {
            // Arrange
            var date = new DateOnly(2025, 1, 15);
            var existingEntity = new DailyLogEntity
            {
                PartitionKey = "dev-user",
                RowKey = "2025-01-15"
            };

            _repositoryMock
                .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            var request = new IncrementUnitRequest
            {
                Date = date,
                Category = category,
                Delta = 1
            };
            var command = new IncrementUnitCommand(request);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.GetUnits(category).Should().Be(1, $"category {category} should have incremented");
        }
    }
}
