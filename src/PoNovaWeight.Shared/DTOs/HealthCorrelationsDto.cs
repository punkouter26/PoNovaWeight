namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Correlation analysis between health metrics.
/// </summary>
public record HealthCorrelationsDto
{
    public required IReadOnlyList<CorrelationInsight> Insights { get; init; }
    public int DaysAnalyzed { get; init; }
    
    /// <summary>
    /// Correlation coefficient between BP and weight (-1 to 1).
    /// </summary>
    public double? BpWeightCorrelation { get; init; }
    
    /// <summary>
    /// Average BP difference between OMAD and non-OMAD days.
    /// </summary>
    public BpDifference? OmadBpDifference { get; init; }
    
    /// <summary>
    /// Average BP difference between alcohol and non-alcohol days.
    /// </summary>
    public BpDifference? AlcoholBpDifference { get; init; }
}

/// <summary>
/// BP difference between two groups.
/// </summary>
public record BpDifference
{
    public decimal SystolicDifference { get; init; }
    public decimal DiastolicDifference { get; init; }
    public int Group1Count { get; init; }
    public int Group2Count { get; init; }
}

/// <summary>
/// Human-readable insight about health correlations.
/// </summary>
public record CorrelationInsight
{
    public required string Message { get; init; }
    public required string Category { get; init; } // "positive", "negative", "neutral"
    public required string Icon { get; init; } // "✓", "✗", "ℹ"
}
