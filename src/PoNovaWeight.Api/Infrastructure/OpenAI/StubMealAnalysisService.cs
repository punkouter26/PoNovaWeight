using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Infrastructure.OpenAI;

/// <summary>
/// Stub implementation of IMealAnalysisService for local development when Azure OpenAI is not configured.
/// </summary>
public class StubMealAnalysisService : IMealAnalysisService
{
    private readonly ILogger<StubMealAnalysisService> _logger;

    public StubMealAnalysisService(ILogger<StubMealAnalysisService> logger)
    {
        _logger = logger;
    }

    public Task<MealScanResultDto> AnalyzeMealAsync(string imageBase64, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Using stub meal analysis service - Azure OpenAI not configured");

        // Return mock data for development/testing
        var result = MealScanResultDto.FromSuggestions(
            new MealSuggestions
            {
                Proteins = 1,
                Vegetables = 2,
                Fruits = 1,
                Starches = 1,
                Fats = 1,
                Dairy = 0
            },
            description: "[STUB] Mock meal analysis - configure Azure OpenAI for real results",
            confidence: 50);

        return Task.FromResult(result);
    }
}
