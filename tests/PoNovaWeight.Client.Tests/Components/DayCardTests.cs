using Bunit;
using FluentAssertions;
using PoNovaWeight.Client.Components;
using PoNovaWeight.Shared.Contracts;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Client.Tests.Components;

public class DayCardTests : TestContext
{
    [Fact]
    public void DayCard_ProgressBar_ShowsCorrectColorByStatus()
    {
        // Test 1: Over target - should show fruit color (red/warning)
        var overTargetDay = new DailyLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Proteins = 20, // Over target of 15
            Vegetables = 3,
            Fruits = 1,
            Starches = 1,
            Fats = 2,
            Dairy = 1,
            WaterSegments = 4
        };
        var cutOver = RenderComponent<DayCard>(parameters => parameters
            .Add(p => p.Day, overTargetDay)
            .Add(p => p.ShowWater, true));
        // DayCard uses inline Tailwind colors - over target shows bg-healthy-fruit
        cutOver.Markup.Should().Contain("bg-healthy-fruit");

        // Test 2: Under target - should show category-specific color
        var underTargetDay = new DailyLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Proteins = 5, // Under target of 15
            Vegetables = 2,
            Fruits = 1,
            Starches = 1,
            Fats = 2,
            Dairy = 1,
            WaterSegments = 0
        };
        var cutUnder = RenderComponent<DayCard>(parameters => parameters
            .Add(p => p.Day, underTargetDay)
            .Add(p => p.ShowWater, true));
        // Under target shows category color (e.g., bg-healthy-earth for proteins)
        cutUnder.Markup.Should().Contain("bg-healthy-earth");

        // Test 3: At target - should show primary color
        var atTargetDay = new DailyLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Proteins = 15, // At target
            Vegetables = 5,
            Fruits = 2,
            Starches = 2,
            Fats = 4,
            Dairy = 0,
            WaterSegments = 0
        };
        var cutAt = RenderComponent<DayCard>(parameters => parameters
            .Add(p => p.Day, atTargetDay)
            .Add(p => p.ShowWater, true));
        // At/met target shows primary color
        cutAt.Markup.Should().Contain("bg-healthy-primary");
    }

    [Fact]
    public void DayCard_TodayBadge_ShowsCorrectlyBasedOnDate()
    {
        // Today should show badge
        var today = DateOnly.FromDateTime(DateTime.Today);
        var todayDay = DailyLogDto.Empty(today);
        var cutToday = RenderComponent<DayCard>(parameters => parameters
            .Add(p => p.Day, todayDay)
            .Add(p => p.ShowWater, false));
        cutToday.Markup.Should().Contain("Today");
        cutToday.Markup.Should().Contain("day-card-today");

        // Yesterday should not show badge
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var yesterdayDay = DailyLogDto.Empty(yesterday);
        var cutYesterday = RenderComponent<DayCard>(parameters => parameters
            .Add(p => p.Day, yesterdayDay)
            .Add(p => p.ShowWater, false));
        cutYesterday.Markup.Should().NotContain(">Today<");
        cutYesterday.Markup.Should().NotContain("day-card-today");
    }

    [Fact]
    public void DayCard_ShowsAllCategories()
    {
        // Arrange
        var day = DailyLogDto.Empty(DateOnly.FromDateTime(DateTime.Today));

        // Act
        var cut = RenderComponent<DayCard>(parameters => parameters
            .Add(p => p.Day, day)
            .Add(p => p.ShowWater, false));

        // Assert - Check for all category short names
        cut.Markup.Should().Contain("Protein");
        cut.Markup.Should().Contain("Veggies");
        cut.Markup.Should().Contain("Fruit");
        cut.Markup.Should().Contain("Starch");
        cut.Markup.Should().Contain("Fat");
        cut.Markup.Should().Contain("Dairy");
    }

    [Fact]
    public void DayCard_WaterDisplay_ShowsWaterSegments()
    {
        // Test: Water display shows current/target format
        var dayWithWater = new DailyLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = 5
        };
        var cut = RenderComponent<DayCard>(parameters => parameters
            .Add(p => p.Day, dayWithWater)
            .Add(p => p.ShowWater, true));
        
        // Water is always shown in compact DayCard with water emoji and count
        cut.Markup.Should().Contain("💧");
        cut.Markup.Should().Contain("5/8");
    }

    [Fact]
    public void DayCard_LinksToCorrectDayDetailPage()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        var day = DailyLogDto.Empty(date);

        // Act
        var cut = RenderComponent<DayCard>(parameters => parameters
            .Add(p => p.Day, day)
            .Add(p => p.ShowWater, false));

        // Assert
        cut.Find("a").GetAttribute("href").Should().Be("/day/2025-01-15");
    }
}
