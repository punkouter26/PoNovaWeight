using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Infrastructure.OpenAI;

/// <summary>
/// Service for AI-powered blood pressure predictions.
/// </summary>
public interface IBpPredictionService
{
    /// <summary>
    /// Predicts blood pressure trends based on historical data and planned lifestyle changes.
    /// </summary>
    Task<BpPredictionResultDto> PredictBpAsync(
        BpPredictionRequestDto request,
        string historicalDataSummary,
        CancellationToken cancellationToken = default);
}
