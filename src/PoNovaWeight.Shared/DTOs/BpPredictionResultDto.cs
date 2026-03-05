namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// AI-generated blood pressure prediction result.
/// </summary>
public record BpPredictionResultDto
{
    public bool Success { get; init; }
    public string? PredictedBpRange { get; init; }
    public IReadOnlyList<string> Recommendations { get; init; } = [];
    public int ConfidenceScore { get; init; } // 0-100
    public string? ErrorMessage { get; init; }

    public static BpPredictionResultDto FromError(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}
