using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Features.DailyLogs;

/// <summary>
/// Edge case tests for UpsertDailyLogHandler.
/// Validates handling of extreme, boundary, and invalid values.
/// </summary>
public class UpsertDailyLogEdgeCaseTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly UpsertDailyLogHandler _handler;

    public UpsertDailyLogEdgeCaseTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _handler = new UpsertDailyLogHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithNegativeBloodPressure_PersistsValues()
    {
        // Arrange - Note: Validation should reject this, but handler persists what's sent
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
            SystolicBP = -10, // Invalid negative value
            DiastolicBP = -5,
            Weight = 180m
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
        Assert.Equal(-10, capturedEntity.SystolicBP);
        Assert.Equal(-5, capturedEntity.DiastolicBP);
        _repositoryMock.Verify(
            r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExtremeHighBloodPressure_PersistsValues()
    {
        // Arrange - Extreme but technically possible in emergency situations
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
            SystolicBP = 300, // Extremely high
            DiastolicBP = 200,
            Weight = 150m
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
        Assert.Equal(300, capturedEntity.SystolicBP);
        Assert.Equal(200, capturedEntity.DiastolicBP);
    }

    [Fact]
    public async Task Handle_WithZeroWeight_PersistsZero()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var dailyLog = new DailyLogDto
        {
            Date = date,
            Weight = 0m,
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0
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
        Assert.Equal(0, capturedEntity.Weight);
    }

    [Fact]
    public async Task Handle_WithNegativeWeight_PersistsNegative()
    {
        // Arrange - Invalid but handler doesn't validate
        var date = DateOnly.FromDateTime(DateTime.Today);
        var dailyLog = new DailyLogDto
        {
            Date = date,
            Weight = -50m, // Invalid negative weight
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0
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
        Assert.Equal(-50, capturedEntity.Weight);
    }

    [Fact]
    public async Task Handle_WithVeryLargeWeight_PersistsValue()
    {
        // Arrange - Extreme but technically possible
        var date = DateOnly.FromDateTime(DateTime.Today);
        var dailyLog = new DailyLogDto
        {
            Date = date,
            Weight = 999.99m, // Very large weight
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0
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
        Assert.NotNull(capturedEntity.Weight);
        Assert.Equal(999.99, capturedEntity.Weight.Value, precision: 2);
    }

    [Fact]
    public async Task Handle_WithMaxUnitValues_PersistsAllValues()
    {
        // Arrange - Maximum values for food units
        var date = DateOnly.FromDateTime(DateTime.Today);
        var dailyLog = new DailyLogDto
        {
            Date = date,
            Proteins = 99,
            Vegetables = 99,
            Fruits = 99,
            Starches = 99,
            Fats = 99,
            Dairy = 99,
            WaterSegments = 99,
            Weight = 180m
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
        Assert.Equal(99, capturedEntity.Proteins);
        Assert.Equal(99, capturedEntity.Vegetables);
        Assert.Equal(99, capturedEntity.Fruits);
        Assert.Equal(99, capturedEntity.Starches);
        Assert.Equal(99, capturedEntity.Fats);
        Assert.Equal(99, capturedEntity.Dairy);
        Assert.Equal(99, capturedEntity.WaterSegments);
    }

    [Fact]
    public async Task Handle_WithNegativeUnitValues_PersistsNegativeValues()
    {
        // Arrange - Negative units (should be validated by FluentValidation)
        var date = DateOnly.FromDateTime(DateTime.Today);
        var dailyLog = new DailyLogDto
        {
            Date = date,
            Proteins = -5,
            Vegetables = -3,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0,
            Weight = 180m
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
        Assert.Equal(-5, capturedEntity.Proteins);
        Assert.Equal(-3, capturedEntity.Vegetables);
    }

    [Fact]
    public async Task Handle_DiastolicHigherThanSystolic_BothValuesStored()
    {
        // Arrange - Physically impossible but handler doesn't validate  
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
            SystolicBP = 70,  // Lower than diastolic
            DiastolicBP = 120, // Higher than systolic
            Weight = 180m
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
        Assert.Equal(70, capturedEntity.SystolicBP);
        Assert.Equal(120, capturedEntity.DiastolicBP);
        // Validation should catch this error, but storage doesn't prevent it
    }

    [Fact]
    public async Task Handle_WithHeartRateBoundaries_PersistsValues()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);

        // Test boundary: minimum resting HR
        var lowHrLog = new DailyLogDto
        {
            Date = date,
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0,
            HeartRate = 30, // Athlete resting
            Weight = 180m
        };

        var command = new UpsertDailyLogCommand(lowHrLog);
        DailyLogEntity? capturedEntity = null;

        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DailyLogEntity, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedEntity);
        Assert.Equal(30, capturedEntity.HeartRate);
    }

    [Fact]
    public async Task Handle_WithMaxHeartRate_PersistsValue()
    {
        // Arrange - Max HR during intense exercise
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
            HeartRate = 220, // Approximate max for young person
            Weight = 180m
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
        Assert.Equal(220, capturedEntity.HeartRate);
    }
}
