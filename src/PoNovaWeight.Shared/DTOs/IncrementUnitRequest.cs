using PoNovaWeight.Shared.Contracts;

namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Request to increment (or decrement) a unit count for a specific category.
/// </summary>
public record IncrementUnitRequest
{
    /// <summary>
    /// The date of the daily log to update.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// The unit category to increment.
    /// </summary>
    public required UnitCategory Category { get; init; }

    /// <summary>
    /// The amount to increment (positive) or decrement (negative). Typically +1 or -1.
    /// </summary>
    public required int Delta { get; init; }
}
