using PoNovaWeight.Shared.Contracts;

namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Represents a weekly summary of food journal entries with totals and targets.
/// </summary>
public record WeeklySummaryDto
{
    public required DateOnly WeekStart { get; init; } // Sunday
    public required DateOnly WeekEnd { get; init; }   // Saturday
    public required IReadOnlyList<DailyLogDto> Days { get; init; }

    public int TotalProteins => Days.Sum(d => d.Proteins);
    public int TotalVegetables => Days.Sum(d => d.Vegetables);
    public int TotalFruits => Days.Sum(d => d.Fruits);
    public int TotalStarches => Days.Sum(d => d.Starches);
    public int TotalFats => Days.Sum(d => d.Fats);
    public int TotalDairy => Days.Sum(d => d.Dairy);

    /// <summary>
    /// Dairy converted to protein equivalent (1 dairy = 2 protein units).
    /// </summary>
    public int DairyAsProteinEquivalent => TotalDairy * UnitCategoryInfo.DairyToProteinFactor;

    // Weekly targets (daily * 7)
    public int WeeklyProteinTarget => UnitCategoryInfo.GetDailyTarget(UnitCategory.Proteins) * 7; // 105
    public int WeeklyVegetableTarget => UnitCategoryInfo.GetDailyTarget(UnitCategory.Vegetables) * 7; // 35
    public int WeeklyFruitTarget => UnitCategoryInfo.GetDailyTarget(UnitCategory.Fruits) * 7; // 14
    public int WeeklyStarchTarget => UnitCategoryInfo.GetDailyTarget(UnitCategory.Starches) * 7; // 14
    public int WeeklyFatTarget => UnitCategoryInfo.GetDailyTarget(UnitCategory.Fats) * 7; // 28
    public int WeeklyDairyMax => UnitCategoryInfo.GetDailyTarget(UnitCategory.Dairy) * 7; // 21

    /// <summary>
    /// Creates an empty weekly summary for the given week bounds.
    /// </summary>
    public static WeeklySummaryDto Empty(DateOnly weekStart, DateOnly weekEnd)
    {
        var days = new List<DailyLogDto>();
        for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
        {
            days.Add(DailyLogDto.Empty(date));
        }

        return new WeeklySummaryDto
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Days = days
        };
    }
}
