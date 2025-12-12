namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Request DTO for meal image scanning.
/// </summary>
public record MealScanRequestDto
{
    /// <summary>
    /// Base64-encoded image data.
    /// </summary>
    public required string ImageBase64 { get; init; }

    /// <summary>
    /// Date for which to log the scanned meal.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Optional meal description or context (e.g., "breakfast", "lunch").
    /// </summary>
    public string? MealContext { get; init; }
}
