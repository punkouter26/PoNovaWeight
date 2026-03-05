namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Request for AI-powered blood pressure predictions.
/// </summary>
public record BpPredictionRequestDto
{
    /// <summary>
    /// Planning to follow OMAD tomorrow/this week?
    /// </summary>
    public bool PlansOmad { get; init; }

    /// <summary>
    /// Planning to consume alcohol tomorrow/this week?
    /// </summary>
    public bool PlansAlcohol { get; init; }

    /// <summary>
    /// Expected weight change (positive = gain, negative = loss).
    /// </summary>
    public decimal? PlannedWeightChange { get; init; }

    /// <summary>
    /// Additional context for the AI (e.g., "starting new medication", "increased exercise").
    /// </summary>
    public string? AdditionalContext { get; init; }
}
