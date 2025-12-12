namespace PoNovaWeight.Shared.Contracts;

/// <summary>
/// Static information about unit categories including daily targets.
/// Dairy is treated as a maximum (not a target), and can be converted to protein equivalents.
/// </summary>
public static class UnitCategoryInfo
{
    /// <summary>
    /// Target/max values for each category. IsMax indicates if the target is a maximum limit (Dairy) vs a goal.
    /// </summary>
    public static readonly IReadOnlyDictionary<UnitCategory, (int Target, bool IsMax)> Targets =
        new Dictionary<UnitCategory, (int, bool)>
        {
            [UnitCategory.Proteins] = (15, false),
            [UnitCategory.Vegetables] = (5, false),
            [UnitCategory.Fruits] = (2, false),
            [UnitCategory.Starches] = (2, false),
            [UnitCategory.Fats] = (4, false),
            [UnitCategory.Dairy] = (3, true) // "max" not "target"
        };

    /// <summary>
    /// Conversion factor: 1 dairy unit = 2 protein units equivalent.
    /// </summary>
    public const int DairyToProteinFactor = 2;

    /// <summary>
    /// Daily water target in 8oz segments.
    /// </summary>
    public const int WaterTargetSegments = 8;

    /// <summary>
    /// Gets the daily target for a specific unit category.
    /// </summary>
    public static int GetDailyTarget(UnitCategory category) => Targets[category].Target;

    /// <summary>
    /// Returns true if the category is a maximum limit (like Dairy) rather than a goal.
    /// </summary>
    public static bool IsMaxLimit(UnitCategory category) => Targets[category].IsMax;

    /// <summary>
    /// Gets the display name for a unit category.
    /// </summary>
    public static string GetDisplayName(UnitCategory category) => category switch
    {
        UnitCategory.Proteins => "Proteins",
        UnitCategory.Vegetables => "Vegetables",
        UnitCategory.Fruits => "Fruits",
        UnitCategory.Starches => "Starches",
        UnitCategory.Fats => "Fats",
        UnitCategory.Dairy => "Dairy",
        _ => category.ToString()
    };

    /// <summary>
    /// Gets the emoji icon for a unit category.
    /// </summary>
    public static string GetEmoji(UnitCategory category) => category switch
    {
        UnitCategory.Proteins => "🍗",
        UnitCategory.Vegetables => "🥦",
        UnitCategory.Fruits => "🍎",
        UnitCategory.Starches => "🍞",
        UnitCategory.Fats => "🥜",
        UnitCategory.Dairy => "🥛",
        _ => "📦"
    };

    /// <summary>
    /// Gets the portion size description for a unit category.
    /// </summary>
    public static string GetPortionSize(UnitCategory category) => category switch
    {
        UnitCategory.Proteins => "Palm-sized portion",
        UnitCategory.Vegetables => "Fist-sized portion",
        UnitCategory.Fruits => "Fist-sized portion",
        UnitCategory.Starches => "Cupped-hand portion",
        UnitCategory.Fats => "Thumb-sized portion",
        UnitCategory.Dairy => "Cupped-hand portion",
        _ => "One serving"
    };
}
