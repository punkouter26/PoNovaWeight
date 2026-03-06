using FluentValidation;
using PoNovaWeight.Shared.Contracts;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Shared.Validation;

/// <summary>
/// Validates MealScanRequestDto ensuring image data is properly formatted and within size limits.
/// </summary>
public sealed class MealScanRequestDtoValidator : AbstractValidator<MealScanRequestDto>
{
    private const int MinImageBytes = 100;
    private const int MaxImageBytes = 10 * 1024 * 1024; // 10MB

    public MealScanRequestDtoValidator()
    {
        RuleFor(x => x.ImageBase64)
            .NotEmpty()
            .WithMessage("Image data is required");

        RuleFor(x => x.ImageBase64)
            .Must(IsValidBase64)
            .WithMessage("Invalid image format. Please provide a valid base64-encoded image");

        RuleFor(x => x.ImageBase64)
            .Must(IsValidImageSize)
            .WithMessage("Image must be between 100 bytes and 10MB in size");

        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Cannot scan meals for future dates");
    }

    private static bool IsValidBase64(string base64String)
    {
        try
        {
            Convert.FromBase64String(base64String);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsValidImageSize(string base64String)
    {
        try
        {
            var imageBytes = Convert.FromBase64String(base64String);
            return imageBytes.Length >= MinImageBytes && imageBytes.Length <= MaxImageBytes;
        }
        catch
        {
            return false;
        }
    }
}
