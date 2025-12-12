using Moq;
using PoNovaWeight.Api.Features.MealScan;
using PoNovaWeight.Api.Infrastructure.OpenAI;
using PoNovaWeight.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace PoNovaWeight.Api.Tests.Features.MealScan;

/// <summary>
/// Unit tests for ScanMealHandler.
/// </summary>
public class ScanMealHandlerTests
{
    private readonly Mock<IMealAnalysisService> _analysisServiceMock;
    private readonly Mock<ILogger<ScanMealHandler>> _loggerMock;
    private readonly ScanMealHandler _handler;

    public ScanMealHandlerTests()
    {
        _analysisServiceMock = new Mock<IMealAnalysisService>();
        _loggerMock = new Mock<ILogger<ScanMealHandler>>();
        _handler = new ScanMealHandler(_analysisServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidImage_ReturnsUnitSuggestions()
    {
        // Arrange
        var validBase64 = Convert.ToBase64String(new byte[1000]); // Simulated image data
        var date = DateOnly.FromDateTime(DateTime.Today);
        var request = new MealScanRequestDto { ImageBase64 = validBase64, Date = date };
        var command = new ScanMealCommand(request);

        var expectedResult = MealScanResultDto.FromSuggestions(
            new MealSuggestions
            {
                Proteins = 2,
                Vegetables = 1,
                Fruits = 0,
                Starches = 1,
                Fats = 1,
                Dairy = 0
            },
            description: "Grilled chicken with salad and rice",
            confidence: 85);

        _analysisServiceMock
            .Setup(s => s.AnalyzeMealAsync(validBase64, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Suggestions);
        Assert.Equal(2, result.Suggestions.Proteins);
        Assert.Equal(1, result.Suggestions.Vegetables);
        Assert.Equal(85, result.ConfidenceScore);
    }

    [Fact]
    public async Task Handle_EmptyImage_ReturnsError()
    {
        // Arrange
        var request = new MealScanRequestDto { ImageBase64 = "", Date = DateOnly.FromDateTime(DateTime.Today) };
        var command = new ScanMealCommand(request);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("No image data", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_InvalidBase64_ReturnsError()
    {
        // Arrange
        var request = new MealScanRequestDto { ImageBase64 = "not-valid-base64!!!", Date = DateOnly.FromDateTime(DateTime.Today) };
        var command = new ScanMealCommand(request);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Invalid image format", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_TooSmallImage_ReturnsError()
    {
        // Arrange
        var smallBase64 = Convert.ToBase64String(new byte[10]); // Too small
        var request = new MealScanRequestDto { ImageBase64 = smallBase64, Date = DateOnly.FromDateTime(DateTime.Today) };
        var command = new ScanMealCommand(request);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("too small", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ServiceReturnsError_PropagatesError()
    {
        // Arrange
        var validBase64 = Convert.ToBase64String(new byte[1000]);
        var request = new MealScanRequestDto { ImageBase64 = validBase64, Date = DateOnly.FromDateTime(DateTime.Today) };
        var command = new ScanMealCommand(request);

        _analysisServiceMock
            .Setup(s => s.AnalyzeMealAsync(validBase64, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MealScanResultDto.FromError("Could not identify food in image"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Could not identify food in image", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_AnalysisService_IsCalledWithCorrectImage()
    {
        // Arrange
        var validBase64 = Convert.ToBase64String(new byte[1000]);
        var request = new MealScanRequestDto { ImageBase64 = validBase64, Date = DateOnly.FromDateTime(DateTime.Today) };
        var command = new ScanMealCommand(request);

        _analysisServiceMock
            .Setup(s => s.AnalyzeMealAsync(validBase64, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MealScanResultDto.FromSuggestions(new MealSuggestions()));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _analysisServiceMock.Verify(
            s => s.AnalyzeMealAsync(validBase64, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
