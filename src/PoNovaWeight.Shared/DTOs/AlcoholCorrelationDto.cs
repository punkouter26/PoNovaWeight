namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Response containing alcohol consumption and weight correlation data.
/// </summary>
public record AlcoholCorrelationDto
{
    /// <summary>
    /// Number of days where alcohol was consumed and weight was logged.
    /// </summary>
    public required int DaysWithAlcohol { get; init; }

    /// <summary>
    /// Number of days where alcohol was not consumed and weight was logged.
    /// </summary>
    public required int DaysWithoutAlcohol { get; init; }

    /// <summary>
    /// Average weight on days when alcohol was consumed. Null if no data.
    /// </summary>
    public decimal? AverageWeightWithAlcohol { get; init; }

    /// <summary>
    /// Average weight on days when alcohol was not consumed. Null if no data.
    /// </summary>
    public decimal? AverageWeightWithoutAlcohol { get; init; }

    /// <summary>
    /// Difference between average weight with and without alcohol.
    /// Positive means heavier on alcohol days. Null if insufficient data.
    /// </summary>
    public decimal? WeightDifference { get; init; }

    /// <summary>
    /// True if there is sufficient data to show meaningful correlation
    /// (at least 7 days with both alcohol and non-alcohol entries).
    /// </summary>
    public required bool HasSufficientData { get; init; }
}
