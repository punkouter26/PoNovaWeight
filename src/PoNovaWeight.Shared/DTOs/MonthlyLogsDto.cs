namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Response containing daily log summaries for a specific month.
/// </summary>
public record MonthlyLogsDto
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required IReadOnlyList<DailyLogSummary> Days { get; init; }
}

/// <summary>
/// Summary of a single day's OMAD-related data for calendar display.
/// </summary>
public record DailyLogSummary
{
    public required DateOnly Date { get; init; }
    public bool? OmadCompliant { get; init; }
    public bool? AlcoholConsumed { get; init; }
    public decimal? Weight { get; init; }
}
