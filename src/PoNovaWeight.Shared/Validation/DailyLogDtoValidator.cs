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
            .Must(NotBeInFuture)
            .WithMessage("Cannot log entries for future dates");

        RuleFor(x => x.Proteins).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Vegetables).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Fruits).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Starches).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Fats).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dairy).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WaterSegments).InclusiveBetween(0, UnitCategoryInfo.WaterTargetSegments);

        // OMAD validation rules
        RuleFor(x => x.Weight)
            .InclusiveBetween(50m, 500m)
            .When(x => x.Weight.HasValue)
            .WithMessage("Weight must be between 50 and 500 lbs");

        RuleFor(x => x.Weight)
            .Must(HaveAtMostOneDecimalPlace)
            .When(x => x.Weight.HasValue)
            .WithMessage("Weight can have at most 1 decimal place");
    }

    private static bool NotBeInFuture(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return date <= today;
    }

    private static bool HaveAtMostOneDecimalPlace(decimal? weight)
    {
        if (!weight.HasValue) return true;
        var scaled = weight.Value * 10;
        return scaled == Math.Floor(scaled);
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
