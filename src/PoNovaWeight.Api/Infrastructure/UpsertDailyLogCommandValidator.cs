using FluentValidation;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Shared.Validation;

namespace PoNovaWeight.Api.Infrastructure;

/// <summary>
/// Validator for UpsertDailyLogCommand that delegates to DailyLogDtoValidator.
/// </summary>
public sealed class UpsertDailyLogCommandValidator : AbstractValidator<UpsertDailyLogCommand>
{
    public UpsertDailyLogCommandValidator()
    {
        RuleFor(x => x.DailyLog)
            .NotNull()
            .SetValidator(new DailyLogDtoValidator());
    }
}
