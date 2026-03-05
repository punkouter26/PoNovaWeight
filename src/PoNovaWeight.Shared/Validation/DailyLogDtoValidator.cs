using FluentValidation;
using PoNovaWeight.Shared.Contracts;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Shared.Validation;

/// <summary>
/// Validates DailyLogDto ensuring values are within acceptable ranges.
/// </summary>
public sealed class DailyLogDtoValidator : AbstractValidator<DailyLogDto>
{
    public DailyLogDtoValidator()
    {
        RuleFor(x => x.Date)
            .Must((dto, date) => NotBeInFuture(date, dto.ClientDate))
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

        RuleFor(x => x.SystolicBP)
            .InclusiveBetween(70m, 200m)
            .When(x => x.SystolicBP.HasValue)
            .WithMessage("Systolic BP must be between 70 and 200 mmHg");

        RuleFor(x => x.DiastolicBP)
            .InclusiveBetween(40m, 130m)
            .When(x => x.DiastolicBP.HasValue)
            .WithMessage("Diastolic BP must be between 40 and 130 mmHg");

        RuleFor(x => x.HeartRate)
            .InclusiveBetween(30, 220)
            .When(x => x.HeartRate.HasValue)
            .WithMessage("Heart rate must be between 30 and 220 bpm");

        RuleFor(x => x.BpReadingTime)
            .Must(BeValidReadingTime)
            .When(x => !string.IsNullOrWhiteSpace(x.BpReadingTime))
            .WithMessage("BP reading time must be Morning, Afternoon, or Evening");

        RuleFor(x => x)
            .Must(HaveValidBpRelationship)
            .WithMessage("Diastolic BP cannot be higher than systolic BP");
    }

    private static bool NotBeInFuture(DateOnly date, DateOnly? clientDate)
    {
        // Use client's date if provided (timezone-safe), otherwise fall back to server's date
        var today = clientDate ?? DateOnly.FromDateTime(DateTime.Today);
        return date <= today;
    }

    private static bool HaveAtMostOneDecimalPlace(decimal? weight)
    {
        if (!weight.HasValue) return true;
        var scaled = weight.Value * 10;
        return scaled == Math.Floor(scaled);
    }

    private static bool BeValidReadingTime(string? readingTime)
    {
        return readingTime is "Morning" or "Afternoon" or "Evening";
    }

    private static bool HaveValidBpRelationship(DailyLogDto dto)
    {
        if (!dto.SystolicBP.HasValue || !dto.DiastolicBP.HasValue)
        {
            return true;
        }

        return dto.DiastolicBP.Value <= dto.SystolicBP.Value;
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
