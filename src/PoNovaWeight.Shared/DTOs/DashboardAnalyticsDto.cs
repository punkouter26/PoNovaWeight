namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Unified response containing all dashboard analytics data.
/// Consolidates Weight Trends, Alcohol Correlation, and Health Correlations into a single request.
/// </summary>
public record DashboardAnalyticsDto
{
    /// <summary>
    /// Weight trend data for the specified period (default 30 days).
    /// </summary>
    public required WeightTrendsDto? WeightTrends { get; init; }

    /// <summary>
    /// Alcohol correlation data comparing weight on alcohol vs non-alcohol days (default 90 days).
    /// </summary>
    public required AlcoholCorrelationDto? AlcoholCorrelation { get; init; }

    /// <summary>
    /// Health correlations between metrics like BP, weight, and OMAD compliance (default 90 days).
    /// </summary>
    public required HealthCorrelationsDto? HealthCorrelations { get; init; }
}
