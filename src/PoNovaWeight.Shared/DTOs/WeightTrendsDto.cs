namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Response containing weight trend data over a specified period.
/// </summary>
public record WeightTrendsDto
{
    /// <summary>
    /// Data points for the weight trend chart.
    /// </summary>
    public required IReadOnlyList<TrendDataPoint> DataPoints { get; init; }

    /// <summary>
    /// Total number of days with actual weight logged.
    /// </summary>
    public required int TotalDaysLogged { get; init; }

    /// <summary>
    /// Weight change from first to last logged weight. Null if insufficient data.
    /// </summary>
    public decimal? WeightChange { get; init; }
}

/// <summary>
/// A single data point in the weight trend chart.
/// </summary>
public record TrendDataPoint
{
    /// <summary>
    /// Date for this data point.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Weight value. Null if no weight logged for this day.
    /// </summary>
    public decimal? Weight { get; init; }

    /// <summary>
    /// True if this weight value was carried forward from a previous day.
    /// </summary>
    public required bool IsCarryForward { get; init; }

    /// <summary>
    /// Whether alcohol was consumed on this day. Null if not logged.
    /// </summary>
    public bool? AlcoholConsumed { get; init; }
}
