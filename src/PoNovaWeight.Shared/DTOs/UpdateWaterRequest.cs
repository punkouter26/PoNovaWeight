namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Request to update water intake for a specific day.
/// </summary>
public record UpdateWaterRequest
{
    /// <summary>
    /// The date of the daily log to update.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// The new water segment count (0-8).
    /// </summary>
    public required int Segments { get; init; }
}
