namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Result DTO for AI meal analysis.
/// </summary>
public record MealScanResultDto
{
    /// <summary>
    /// Whether the analysis was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if analysis failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// AI-generated description of the meal.
    /// </summary>
    public string? MealDescription { get; init; }

    /// <summary>
    /// Suggested unit counts per category.
    /// </summary>
    public MealSuggestions? Suggestions { get; init; }

    /// <summary>
    /// Confidence score (0-100) for the analysis.
    /// </summary>
    public int ConfidenceScore { get; init; }

    /// <summary>
    /// Creates a successful result with suggestions.
    /// </summary>
    public static MealScanResultDto FromSuggestions(MealSuggestions suggestions, string? description = null, int confidence = 80)
        => new()
        {
            Success = true,
            MealDescription = description,
            Suggestions = suggestions,
            ConfidenceScore = confidence
        };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static MealScanResultDto FromError(string errorMessage)
        => new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
}

/// <summary>
/// Suggested unit counts from AI analysis.
/// </summary>
public record MealSuggestions
{
    /// <summary>Suggested protein units (e.g., palm-sized portions).</summary>
    public int Proteins { get; init; }

    /// <summary>Suggested vegetable units (e.g., fist-sized portions).</summary>
    public int Vegetables { get; init; }

    /// <summary>Suggested fruit units (e.g., fist-sized portions).</summary>
    public int Fruits { get; init; }

    /// <summary>Suggested starch units (e.g., cupped-hand portions).</summary>
    public int Starches { get; init; }

    /// <summary>Suggested fat units (e.g., thumb-sized portions).</summary>
    public int Fats { get; init; }

    /// <summary>Suggested dairy units (e.g., cupped-hand portions).</summary>
    public int Dairy { get; init; }

    /// <summary>
    /// Returns the total number of units across all categories.
    /// </summary>
    public int TotalUnits => Proteins + Vegetables + Fruits + Starches + Fats + Dairy;
}
