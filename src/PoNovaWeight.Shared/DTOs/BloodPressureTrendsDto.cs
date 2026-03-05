namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Represents a single data point in the blood pressure trend chart.
/// </summary>
public record BpTrendDataPoint
{
    public required DateOnly Date { get; init; }
    public decimal? Systolic { get; init; }
    public decimal? Diastolic { get; init; }
    public int? HeartRate { get; init; }
    public string? ReadingTime { get; init; }
    public bool IsCarryForward { get; init; }
    public bool? AlcoholConsumed { get; init; }
}

/// <summary>
/// Blood pressure trends over a date range with computed statistics.
/// </summary>
public record BloodPressureTrendsDto
{
    public required IReadOnlyList<BpTrendDataPoint> DataPoints { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    
    /// <summary>
    /// Average systolic BP for non-carry-forward readings.
    /// </summary>
    public decimal? AverageSystolic
    {
        get
        {
            var readings = DataPoints.Where(d => !d.IsCarryForward && d.Systolic.HasValue).ToList();
            return readings.Count > 0 ? readings.Average(d => d.Systolic!.Value) : (decimal?)null;
        }
    }
    
    /// <summary>
    /// Average diastolic BP for non-carry-forward readings.
    /// </summary>
    public decimal? AverageDiastolic
    {
        get
        {
            var readings = DataPoints.Where(d => !d.IsCarryForward && d.Diastolic.HasValue).ToList();
            return readings.Count > 0 ? readings.Average(d => d.Diastolic!.Value) : (decimal?)null;
        }
    }
    
    /// <summary>
    /// Average heart rate for non-carry-forward readings.
    /// </summary>
    public double? AverageHeartRate
    {
        get
        {
            var readings = DataPoints.Where(d => !d.IsCarryForward && d.HeartRate.HasValue).ToList();
            return readings.Count > 0 ? readings.Average(d => (double)d.HeartRate!.Value) : (double?)null;
        }
    }
    
    /// <summary>
    /// Number of days with actual BP readings (not carry-forward).
    /// </summary>
    public int DaysLogged => DataPoints.Count(d => !d.IsCarryForward && d.Systolic.HasValue);
}
