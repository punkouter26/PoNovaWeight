using FluentValidation.TestHelper;
using PoNovaWeight.Shared.DTOs;
using PoNovaWeight.Shared.Validation;

namespace PoNovaWeight.Api.Tests.Features.DailyLogs;

/// <summary>
/// Tests for weight threshold validation (5 lb confirmation check).
/// </summary>
public class WeightThresholdTests
{
    private readonly DailyLogDtoValidator _validator;

    public WeightThresholdTests()
    {
        _validator = new DailyLogDtoValidator();
    }

    [Theory]
    [InlineData(50)]   // Minimum valid weight
    [InlineData(150)]  // Average weight
    [InlineData(500)]  // Maximum valid weight
    public void Validate_WeightInRange_ShouldPass(decimal weight)
    {
        // Arrange
        var dto = CreateValidDto(weight);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Weight);
    }

    [Theory]
    [InlineData(49.9)]   // Just below minimum
    [InlineData(500.1)]  // Just above maximum
    [InlineData(0)]      // Zero
    [InlineData(1000)]   // Way too high
    public void Validate_WeightOutOfRange_ShouldFail(decimal weight)
    {
        // Arrange
        var dto = CreateValidDto(weight);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Weight);
    }

    [Fact]
    public void Validate_WeightNull_ShouldPass()
    {
        // Arrange
        var dto = new DailyLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
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

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Weight);
    }

    [Theory]
    [InlineData(175.5)]  // One decimal place - valid
    [InlineData(180.0)]  // Zero decimals - valid
    [InlineData(165)]    // No decimal - valid
    public void Validate_WeightOneDecimalPlace_ShouldPass(decimal weight)
    {
        // Arrange
        var dto = CreateValidDto(weight);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Weight);
    }

    [Theory]
    [InlineData(175.55)]   // Two decimal places
    [InlineData(180.123)]  // Three decimal places
    public void Validate_WeightMoreThanOneDecimalPlace_ShouldFail(decimal weight)
    {
        // Arrange
        var dto = CreateValidDto(weight);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Weight)
            .WithErrorMessage("Weight can have at most 1 decimal place");
    }

    [Fact]
    public void Validate_FutureDate_ShouldFail()
    {
        // Arrange
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var dto = new DailyLogDto
        {
            Date = tomorrow,
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0,
            Weight = 175m,
            OmadCompliant = true,
            AlcoholConsumed = false
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Date)
            .WithErrorMessage("Cannot log entries for future dates");
    }

    [Fact]
    public void Validate_TodayDate_ShouldPass()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = new DailyLogDto
        {
            Date = today,
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0,
            Weight = 175m,
            OmadCompliant = true,
            AlcoholConsumed = false
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Validate_PastDate_ShouldPass()
    {
        // Arrange
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var dto = new DailyLogDto
        {
            Date = yesterday,
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0,
            Weight = 175m,
            OmadCompliant = true,
            AlcoholConsumed = false
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Date);
    }

    private static DailyLogDto CreateValidDto(decimal weight)
    {
        return new DailyLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 0,
            Weight = weight,
            OmadCompliant = null,
            AlcoholConsumed = null
        };
    }
}
