namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Response containing the current OMAD streak information.
/// </summary>
public record StreakDto
{
    /// <summary>
    /// Number of consecutive OMAD-compliant days in the current streak.
    /// </summary>
    public required int CurrentStreak { get; init; }

    /// <summary>
    /// The date when the current streak started. Null if no streak.
    /// </summary>
    public DateOnly? StreakStartDate { get; init; }
}
