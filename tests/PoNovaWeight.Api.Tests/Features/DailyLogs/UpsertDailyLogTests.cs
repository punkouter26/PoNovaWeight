using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Features.DailyLogs;

/// <summary>
/// Unit tests for UpsertDailyLogHandler with OMAD fields.
/// </summary>
public class UpsertDailyLogTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly UpsertDailyLogHandler _handler;

    public UpsertDailyLogTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _handler = new UpsertDailyLogHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithOmadFields_PersistsAllFields()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var dailyLog = new DailyLogDto
        {
            Date = date,
            Proteins = 5,
            Vegetables = 3,
            Fruits = 2,
            Starches = 4,
            Fats = 1,
            Dairy = 2,
            WaterSegments = 6,
            Weight = 175.5m,
            OmadCompliant = true,
            AlcoholConsumed = false
        };

        var command = new UpsertDailyLogCommand(dailyLog);
        DailyLogEntity? capturedEntity = null;

        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DailyLogEntity, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(175.5m, result.Weight);
        Assert.True(result.OmadCompliant);
        Assert.False(result.AlcoholConsumed);

        Assert.NotNull(capturedEntity);
        Assert.Equal(175.5, capturedEntity.Weight);
        Assert.True(capturedEntity.OmadCompliant);
        Assert.False(capturedEntity.AlcoholConsumed);
    }

    [Fact]
    public async Task Handle_WithNullOmadFields_PersistsNulls()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var dailyLog = new DailyLogDto
        {
            Date = date,
            Proteins = 5,
            Vegetables = 3,
            Fruits = 2,
            Starches = 4,
            Fats = 1,
            Dairy = 2,
            WaterSegments = 6,
            Weight = null,
            OmadCompliant = null,
            AlcoholConsumed = null
        };

        var command = new UpsertDailyLogCommand(dailyLog);
        DailyLogEntity? capturedEntity = null;

        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DailyLogEntity, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Weight);
        Assert.Null(result.OmadCompliant);
        Assert.Null(result.AlcoholConsumed);

        Assert.NotNull(capturedEntity);
        Assert.Null(capturedEntity.Weight);
        Assert.Null(capturedEntity.OmadCompliant);
        Assert.Null(capturedEntity.AlcoholConsumed);
    }

    [Fact]
    public async Task Handle_OmadCompliantTrue_AlcoholConsumedTrue_BothPersisted()
    {
        // Arrange - Edge case: user can be OMAD compliant and still drink alcohol
        var date = DateOnly.FromDateTime(DateTime.Today);
        var dailyLog = new DailyLogDto
        {
            Date = date,
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0,
            Weight = 180m,
            OmadCompliant = true,
            AlcoholConsumed = true
        };

        var command = new UpsertDailyLogCommand(dailyLog);
        DailyLogEntity? capturedEntity = null;

        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DailyLogEntity, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedEntity);
        Assert.True(capturedEntity.OmadCompliant);
        Assert.True(capturedEntity.AlcoholConsumed);
    }

    [Fact]
    public async Task Handle_WeightWithDecimal_RoundsCorrectly()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var dailyLog = new DailyLogDto
        {
            Date = date,
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0,
            Weight = 165.7m,
            OmadCompliant = null,
            AlcoholConsumed = null
        };

        var command = new UpsertDailyLogCommand(dailyLog);
        DailyLogEntity? capturedEntity = null;

        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DailyLogEntity, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedEntity);
        Assert.Equal(165.7, capturedEntity.Weight!.Value, precision: 1);
    }
}
