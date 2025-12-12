using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Features.DailyLogs;

/// <summary>
/// Unit tests for UpdateWaterHandler.
/// </summary>
public class UpdateWaterHandlerTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly UpdateWaterHandler _handler;

    public UpdateWaterHandlerTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _handler = new UpdateWaterHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_NewDay_SetsWaterSegments()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var request = new UpdateWaterRequest { Date = date, Segments = 5 };
        var command = new UpdateWaterCommand(request);

        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyLogEntity?)null);

        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.WaterSegments);
        Assert.Equal(date, result.Date);
    }

    [Fact]
    public async Task Handle_ExistingDay_UpdatesWaterSegments()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var request = new UpdateWaterRequest { Date = date, Segments = 8 };
        var command = new UpdateWaterCommand(request);

        var existingEntity = new DailyLogEntity
        {
            PartitionKey = "dev-user",
            RowKey = date.ToString("yyyy-MM-dd"),
            Proteins = 3,
            Vegetables = 2,
            WaterSegments = 3
        };

        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, result.WaterSegments);
        Assert.Equal(3, result.Proteins); // Original values preserved
        Assert.Equal(2, result.Vegetables);
    }

    [Fact]
    public async Task Handle_ValidSegmentValues_Succeeds()
    {
        // Consolidates valid segment tests (0, 1, 4, 8)
        var validSegments = new[] { 0, 1, 4, 8 };

        foreach (var segments in validSegments)
        {
            // Arrange
            var date = DateOnly.FromDateTime(DateTime.Today);
            var request = new UpdateWaterRequest { Date = date, Segments = segments };
            var command = new UpdateWaterCommand(request);

            _repositoryMock
                .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DailyLogEntity?)null);

            _repositoryMock
                .Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(segments, result.WaterSegments);
        }
    }

    [Fact]
    public async Task Handle_InvalidSegmentValues_ThrowsArgumentOutOfRange()
    {
        // Consolidates invalid segment tests (-1, 9, 100)
        var invalidSegments = new[] { -1, 9, 100 };

        foreach (var segments in invalidSegments)
        {
            // Arrange
            var date = DateOnly.FromDateTime(DateTime.Today);
            var request = new UpdateWaterRequest { Date = date, Segments = segments };
            var command = new UpdateWaterCommand(request);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Handle_UpsertCalledWithCorrectEntity()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var request = new UpdateWaterRequest { Date = date, Segments = 6 };
        var command = new UpdateWaterCommand(request);

        DailyLogEntity? savedEntity = null;

        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyLogEntity?)null);

        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DailyLogEntity, CancellationToken>((entity, _) => savedEntity = entity)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(savedEntity);
        Assert.Equal(6, savedEntity.WaterSegments);
        Assert.Equal("dev-user", savedEntity.PartitionKey);
        Assert.Equal(date.ToString("yyyy-MM-dd"), savedEntity.RowKey);
    }
}
