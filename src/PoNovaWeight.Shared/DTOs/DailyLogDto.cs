using PoNovaWeight.Shared.Contracts;

namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Represents a daily food journal entry with unit counts for each category.
/// </summary>
public record DailyLogDto
{
    public required DateOnly Date { get; init; }
    public required int Proteins { get; init; }
    public required int Vegetables { get; init; }
    public required int Fruits { get; init; }
    public required int Starches { get; init; }
    public required int Fats { get; init; }
    public required int Dairy { get; init; }
    public required int WaterSegments { get; init; }

    // OMAD tracking fields
    /// <summary>
    /// Weight in pounds (50-500 lbs range). Null if not logged.
    /// </summary>
    public decimal? Weight { get; init; }

    /// <summary>
    /// True if OMAD (One Meal A Day) was followed, null if not logged.
    /// </summary>
    public bool? OmadCompliant { get; init; }

    /// <summary>
    /// True if alcohol was consumed, null if not logged.
    /// </summary>
    public bool? AlcoholConsumed { get; init; }

    /// <summary>
    /// Checks if the given category is over its daily target.
    /// </summary>
    public bool IsOverTarget(UnitCategory category) => category switch
    {
        UnitCategory.Proteins => Proteins > UnitCategoryInfo.GetDailyTarget(UnitCategory.Proteins),
        UnitCategory.Vegetables => Vegetables > UnitCategoryInfo.GetDailyTarget(UnitCategory.Vegetables),
        UnitCategory.Fruits => Fruits > UnitCategoryInfo.GetDailyTarget(UnitCategory.Fruits),
        UnitCategory.Starches => Starches > UnitCategoryInfo.GetDailyTarget(UnitCategory.Starches),
        UnitCategory.Fats => Fats > UnitCategoryInfo.GetDailyTarget(UnitCategory.Fats),
        UnitCategory.Dairy => Dairy > UnitCategoryInfo.GetDailyTarget(UnitCategory.Dairy),
        _ => false
    };

    /// <summary>
    /// Gets the current unit count for a specific category.
    /// </summary>
    public int GetUnits(UnitCategory category) => category switch
    {
        UnitCategory.Proteins => Proteins,
        UnitCategory.Vegetables => Vegetables,
        UnitCategory.Fruits => Fruits,
        UnitCategory.Starches => Starches,
        UnitCategory.Fats => Fats,
        UnitCategory.Dairy => Dairy,
        _ => 0
    };

    /// <summary>
    /// Creates an empty daily log for the specified date.
    /// </summary>
    public static DailyLogDto Empty(DateOnly date) => new()
    {
        Date = date,
        Proteins = 0,
        Vegetables = 0,
        Fruits = 0,
        Starches = 0,
        Fats = 0,
        Dairy = 0,
        WaterSegments = 0,
        Weight = null,
        OmadCompliant = null,
        AlcoholConsumed = null
    };
}
