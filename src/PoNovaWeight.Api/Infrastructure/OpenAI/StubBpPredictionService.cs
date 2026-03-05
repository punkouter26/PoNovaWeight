using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Infrastructure.OpenAI;

/// <summary>
/// Stub implementation of IBpPredictionService for local development when Azure OpenAI is not configured.
/// </summary>
public sealed class StubBpPredictionService(ILogger<StubBpPredictionService> logger) : IBpPredictionService
{
    public Task<BpPredictionResultDto> PredictBpAsync(
        BpPredictionRequestDto request,
        string historicalDataSummary,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Using stub BP prediction service - Azure OpenAI not configured");

        var recommendations = new List<string>
        {
            "Continue tracking your BP daily for better predictions",
            "Maintain OMAD compliance for optimal cardiovascular health",
            "Limit alcohol consumption to once per week"
        };

        if (request.PlansOmad)
        {
            recommendations.Add("OMAD days typically show lower BP readings");
        }

        if (request.PlansAlcohol)
        {
            recommendations.Add("Alcohol may temporarily elevate BP by 5-10 points");
        }

        var result = new BpPredictionResultDto
        {
            Success = true,
            PredictedBpRange = "118-125 / 75-82 mmHg",
            Recommendations = recommendations,
            ConfidenceScore = 50, // Low confidence for stub data
            ErrorMessage = null
        };

        return Task.FromResult(result);
    }
}
