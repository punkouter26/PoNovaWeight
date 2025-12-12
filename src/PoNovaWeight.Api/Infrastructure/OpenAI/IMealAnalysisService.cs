using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Infrastructure.OpenAI;

/// <summary>
/// Service interface for AI-powered meal analysis.
/// </summary>
public interface IMealAnalysisService
{
    /// <summary>
    /// Analyzes a meal image and returns suggested unit counts.
    /// </summary>
    /// <param name="imageBase64">Base64-encoded image data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis result with suggested unit counts.</returns>
    Task<MealScanResultDto> AnalyzeMealAsync(string imageBase64, CancellationToken cancellationToken = default);
}
