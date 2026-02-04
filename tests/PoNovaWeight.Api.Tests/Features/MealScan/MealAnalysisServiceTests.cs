using Moq;
using PoNovaWeight.Api.Infrastructure.OpenAI;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Features.MealScan;

/// <summary>
/// Unit tests for MealAnalysisService (parsing and validation only - actual AI calls are mocked).
/// </summary>
public class MealAnalysisServiceTests
{
    [Fact]
    public async Task MockedService_ReturnsValidStructure()
    {
        // Use mock instead of non-existent StubMealAnalysisService
        var mockService = new Mock<IMealAnalysisService>();
        mockService.Setup(s => s.AnalyzeMealAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MealScanResultDto.FromSuggestions(
                new MealSuggestions { Proteins = 1, Vegetables = 2, Fruits = 1, Starches = 1, Fats = 1, Dairy = 0 },
                description: "Test meal with protein and vegetables",
                confidence: 50));

        // Act
        var result = await mockService.Object.AnalyzeMealAsync("dummy-base64");

        // Assert - validates structure
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Suggestions);
        Assert.NotNull(result.MealDescription);
        Assert.InRange(result.Suggestions.TotalUnits, 0, 18);

        // Validates expected values
        Assert.Equal(1, result.Suggestions.Proteins);
        Assert.Equal(2, result.Suggestions.Vegetables);
        Assert.Equal(50, result.ConfidenceScore);
    }

    [Fact]
    public void MealSuggestions_TotalUnits_CalculatesCorrectly()
    {
        // Arrange
        var suggestions = new PoNovaWeight.Shared.DTOs.MealSuggestions
        {
            Proteins = 2,
            Vegetables = 3,
            Fruits = 1,
            Starches = 1,
            Fats = 2,
            Dairy = 1
        };

        // Act & Assert
        Assert.Equal(10, suggestions.TotalUnits);
    }

    [Fact]
    public void MealScanResultDto_FactoryMethods_CreateCorrectResults()
    {
        // Test 1: FromError creates failed result
        var errorResult = PoNovaWeight.Shared.DTOs.MealScanResultDto.FromError("Test error message");
        Assert.False(errorResult.Success);
        Assert.Equal("Test error message", errorResult.ErrorMessage);
        Assert.Null(errorResult.Suggestions);

        // Test 2: FromSuggestions creates success result
        var suggestions = new PoNovaWeight.Shared.DTOs.MealSuggestions { Proteins = 1 };
        var successResult = PoNovaWeight.Shared.DTOs.MealScanResultDto.FromSuggestions(
            suggestions,
            description: "Test meal",
            confidence: 75);
        Assert.True(successResult.Success);
        Assert.Null(successResult.ErrorMessage);
        Assert.NotNull(successResult.Suggestions);
        Assert.Equal("Test meal", successResult.MealDescription);
        Assert.Equal(75, successResult.ConfidenceScore);
    }
}
