using Moq;
using PoNovaWeight.Api.Features.Predictions;
using PoNovaWeight.Api.Infrastructure.OpenAI;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace PoNovaWeight.Api.Tests.Features.Predictions;

/// <summary>
/// Unit tests for PredictBloodPressureHandler.
/// Verifies BP prediction logic with mocked AI service (no real OpenAI calls).
/// </summary>
public class PredictBloodPressureHandlerTests
{
    private readonly Mock<IBpPredictionService> _predictionServiceMock;
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly Mock<ILogger<PredictBloodPressureHandler>> _loggerMock;
    private readonly PredictBloodPressureHandler _handler;
    private readonly TimeProvider _timeProvider;

    public PredictBloodPressureHandlerTests()
    {
        _predictionServiceMock = new Mock<IBpPredictionService>();
        _repositoryMock = new Mock<IDailyLogRepository>();
        _loggerMock = new Mock<ILogger<PredictBloodPressureHandler>>();
        _timeProvider = TimeProvider.System;
        _handler = new PredictBloodPressureHandler(
            _predictionServiceMock.Object,
            _repositoryMock.Object,
            _timeProvider,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithSufficientHistoricalData_ReturnsPrediction()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new BpPredictionRequestDto { PlansOmad = true, PlansAlcohol = false };
        var command = new PredictBloodPressureCommand(request, "test-user");

        // Create historical BP data (90 days)
        var entities = Enumerable.Range(0, 90)
            .Select(i => new DailyLogEntity
            {
                PartitionKey = "test-user",
                RowKey = today.AddDays(-i).ToString("yyyy-MM-dd"),
                SystolicBP = 120.0 + (i % 10),
                DiastolicBP = 80.0 + (i % 8),
                Weight = 180.0
            })
            .ToList();

        _repositoryMock
            .Setup(r => r.GetRangeAsync(
                "test-user",
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var expectedResult = new BpPredictionResultDto
        {
            Success = true,
            PredictedBpRange = "115-125 / 75-85 mmHg",
            Recommendations = ["Maintain OMAD compliance", "Continue daily tracking"],
            ConfidenceScore = 85,
            ErrorMessage = null
        };

        _predictionServiceMock
            .Setup(s => s.PredictBpAsync(
                request,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(85, result.ConfidenceScore);
        Assert.Contains("Maintain OMAD compliance", result.Recommendations);
        _predictionServiceMock.Verify(
            s => s.PredictBpAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InsufficientHistoricalData_ReturnsError()
    {
        // Arrange
        var request = new BpPredictionRequestDto { PlansOmad = true, PlansAlcohol = false };
        var command = new PredictBloodPressureCommand(request, "new-user");

        _repositoryMock
            .Setup(r => r.GetRangeAsync(
                "new-user",
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyLogEntity>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Insufficient data", result.ErrorMessage);
        _predictionServiceMock.Verify(
            s => s.PredictBpAsync(It.IsAny<BpPredictionRequestDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "AI service should not be called with insufficient data");
    }

    [Fact]
    public async Task Handle_WithPlannedLiefstyleChanges_IncludesRecommendations()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new BpPredictionRequestDto { PlansOmad = true, PlansAlcohol = true };
        var command = new PredictBloodPressureCommand(request, "test-user");

        var entities = Enumerable.Range(0, 30)
            .Select(i => new DailyLogEntity
            {
                PartitionKey = "test-user",
                RowKey = today.AddDays(-i).ToString("yyyy-MM-dd"),
                SystolicBP = 125.0,
                DiastolicBP = 85.0,
                Weight = 185.0,
                AlcoholConsumed = i % 7 == 0 // Every 7th day
            })
            .ToList();

        _repositoryMock
            .Setup(r => r.GetRangeAsync(
                "test-user",
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var expectedResult = new BpPredictionResultDto
        {
            Success = true,
            PredictedBpRange = "118-128 / 78-88 mmHg",
            Recommendations = [
                "Limit alcohol consumption to once per week",
                "OMAD days typically show lower BP readings",
                "Alcohol may temporarily elevate BP by 5-10 points"
            ],
            ConfidenceScore = 75,
            ErrorMessage = null
        };

        _predictionServiceMock
            .Setup(s => s.PredictBpAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Recommendations.Count);
        Assert.Contains("OMAD days typically show lower BP readings", result.Recommendations);
        Assert.Contains("Alcohol may temporarily elevate BP by 5-10 points", result.Recommendations);
    }

    [Fact]
    public async Task Handle_WhenAiServiceFails_ReturnsErrorFromService()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new BpPredictionRequestDto { PlansOmad = false, PlansAlcohol = false };
        var command = new PredictBloodPressureCommand(request, "test-user");

        var entities = Enumerable.Range(0, 30)
            .Select(i => new DailyLogEntity
            {
                PartitionKey = "test-user",
                RowKey = today.AddDays(-i).ToString("yyyy-MM-dd"),
                SystolicBP = 130.0,
                DiastolicBP = 90.0
            })
            .ToList();

        _repositoryMock
            .Setup(r => r.GetRangeAsync(
                "test-user",
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var failureResult = new BpPredictionResultDto
        {
            Success = false,
            PredictedBpRange = null,
            Recommendations = [],
            ConfidenceScore = 0,
            ErrorMessage = "Unable to generate prediction from historical patterns"
        };

        _predictionServiceMock
            .Setup(s => s.PredictBpAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Unable to generate prediction from historical patterns", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_DefaultUserId_UsesDevUser()
    {
        // Arrange
        var request = new BpPredictionRequestDto { PlansOmad = true, PlansAlcohol = false };
        var command = new PredictBloodPressureCommand(request); // No userId specified, defaults to "dev-user"

        var entities = Enumerable.Range(0, 30)
            .Select(i => new DailyLogEntity
            {
                PartitionKey = "dev-user",
                RowKey = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-i).ToString("yyyy-MM-dd"),
                SystolicBP = 115.0,
                DiastolicBP = 75.0
            })
            .ToList();

        _repositoryMock
            .Setup(r => r.GetRangeAsync(
                "dev-user",
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        _predictionServiceMock
            .Setup(s => s.PredictBpAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BpPredictionResultDto { Success = true, ConfidenceScore = 90 });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        _repositoryMock.Verify(
            r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
