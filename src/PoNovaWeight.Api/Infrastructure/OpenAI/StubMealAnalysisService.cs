using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Infrastructure.OpenAI;

/// <summary>
/// Stub implementation of IMealAnalysisService for development without Azure OpenAI.
/// Returns mock data for testing the UI flow.
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
        _logger.LogWarning("Using stub meal analysis service. Configure Azure OpenAI for real analysis.");

        // Return mock data simulating a balanced meal
        var result = MealScanResultDto.FromSuggestions(
            new MealSuggestions
            {
                Proteins = 1,
                Vegetables = 2,
                Fruits = 0,
                Starches = 1,
                Fats = 1,
                Dairy = 0
            },
            description: "[STUB] Sample meal analysis - configure Azure OpenAI for real results",
            confidence: 50);

        return Task.FromResult(result);
    }
}
