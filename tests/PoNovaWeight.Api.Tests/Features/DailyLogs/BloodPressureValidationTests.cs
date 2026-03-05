using FluentAssertions;
using FluentValidation.TestHelper;
using PoNovaWeight.Shared.DTOs;
using PoNovaWeight.Shared.Validation;
using Xunit;

namespace PoNovaWeight.Api.Tests.Features.DailyLogs;

/// <summary>
/// Tests for blood pressure validation rules.
/// </summary>
public class BloodPressureValidationTests
{
    private readonly DailyLogDtoValidator _validator;

    public BloodPressureValidationTests()
    {
        _validator = new DailyLogDtoValidator();
    }

    [Theory]
    [InlineData(70)]    // Minimum valid systolic
    [InlineData(120)]   // Normal systolic
    [InlineData(140)]   // Hypertension threshold
    [InlineData(200)]   // Maximum valid systolic
    public void Validate_ValidSystolicBP(decimal systolic)
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = DailyLogDto.Empty(today) with { SystolicBP = systolic, DiastolicBP = 80 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SystolicBP);
    }

    [Theory]
    [InlineData(69)]    // Just below minimum
    [InlineData(201)]   // Just above maximum
    public void Validate_InvalidSystolicBP(decimal systolic)
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = DailyLogDto.Empty(today) with { SystolicBP = systolic, DiastolicBP = 80 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SystolicBP);
    }

    [Theory]
    [InlineData(40)]    // Minimum valid diastolic
    [InlineData(80)]    // Normal diastolic
    [InlineData(90)]    // Hypertension threshold
    [InlineData(130)]   // Maximum valid diastolic
    public void Validate_ValidDiastolicBP(decimal diastolic)
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = DailyLogDto.Empty(today) with { SystolicBP = 120, DiastolicBP = diastolic };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DiastolicBP);
    }

    [Theory]
    [InlineData(39)]    // Just below minimum
    [InlineData(131)]   // Just above maximum
    public void Validate_InvalidDiastolicBP(decimal diastolic)
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = DailyLogDto.Empty(today) with { SystolicBP = 120, DiastolicBP = diastolic };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiastolicBP);
    }

    [Fact]
    public void Validate_DiastolicHigherThanSystolic_Fails()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = DailyLogDto.Empty(today) with { SystolicBP = 100, DiastolicBP = 120 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        // When diastolic > systolic, the validator raises an error on the object itself
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NullBPValues_Passes()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = DailyLogDto.Empty(today) with { SystolicBP = null, DiastolicBP = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SystolicBP);
        result.ShouldNotHaveValidationErrorFor(x => x.DiastolicBP);
    }

    [Theory]
    [InlineData("Morning")]
    [InlineData("Afternoon")]
    [InlineData("Evening")]
    public void Validate_ValidReadingTime(string readingTime)
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = DailyLogDto.Empty(today) with { BpReadingTime = readingTime };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BpReadingTime);
    }

    [Theory]
    [InlineData("Night")]
    [InlineData("InvalidTime")]
    public void Validate_InvalidReadingTime(string readingTime)
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = DailyLogDto.Empty(today) with { BpReadingTime = readingTime };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BpReadingTime);
    }
}
