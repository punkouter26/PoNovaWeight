using FluentValidation;
using PoNovaWeight.Shared.Contracts;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Shared.Validation;

/// <summary>
/// Validates DailyLogDto ensuring values are within acceptable ranges.
/// </summary>
public class DailyLogDtoValidator : AbstractValidator<DailyLogDto>
{
    public DailyLogDtoValidator()
    {
        RuleFor(x => x.Date)
            .Must(BeWithinCurrentWeek)
            .WithMessage("Can only log for current week");

        RuleFor(x => x.Proteins).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Vegetables).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Fruits).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Starches).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Fats).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dairy).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WaterSegments).InclusiveBetween(0, UnitCategoryInfo.WaterTargetSegments);
    }

    private static bool BeWithinCurrentWeek(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var (weekStart, weekEnd) = GetWeekBounds(today);
        return date >= weekStart && date <= weekEnd;
    }

    /// <summary>
    /// Gets the week bounds (Sunday to Saturday) for the given date.
    /// </summary>
    public static (DateOnly Start, DateOnly End) GetWeekBounds(DateOnly date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
        var start = date.AddDays(-diff);
        var end = start.AddDays(6);
        return (start, end);
    }
}
