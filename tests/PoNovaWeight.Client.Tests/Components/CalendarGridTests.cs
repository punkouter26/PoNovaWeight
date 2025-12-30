using Bunit;
using FluentAssertions;
using PoNovaWeight.Client.Components;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Client.Tests.Components;

public class CalendarGridTests : BunitContext
{
    [Fact]
    public void CalendarGrid_RendersMonthHeader()
    {
        // Arrange & Act
        var cut = Render<CalendarGrid>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 12)
            .Add(p => p.Days, new List<DailyLogSummary>()));

        // Assert
        cut.Markup.Should().Contain("December 2025");
    }

    [Fact]
    public void CalendarGrid_RendersDayOfWeekHeaders()
    {
        // Arrange & Act
        var cut = Render<CalendarGrid>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 1)
            .Add(p => p.Days, new List<DailyLogSummary>()));

        // Assert - should have S M T W T F S headers
        cut.Markup.Should().Contain(">S<");
        cut.Markup.Should().Contain(">M<");
        cut.Markup.Should().Contain(">T<");
        cut.Markup.Should().Contain(">W<");
        cut.Markup.Should().Contain(">F<");
    }

    [Fact]
    public void CalendarGrid_RendersDaysOfMonth()
    {
        // Arrange & Act
        var cut = Render<CalendarGrid>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 1)
            .Add(p => p.Days, new List<DailyLogSummary>()));

        // Assert - January 2025 has 31 days
        cut.Markup.Should().Contain(">1<");
        cut.Markup.Should().Contain(">15<");
        cut.Markup.Should().Contain(">31<");
    }

    [Fact]
    public void CalendarGrid_ShowsGreen_ForOmadCompliantDays()
    {
        // Arrange
        var days = new List<DailyLogSummary>
        {
            new() { Date = new DateOnly(2025, 1, 1), OmadCompliant = true, AlcoholConsumed = false, Weight = 175m }
        };

        // Act
        var cut = Render<CalendarGrid>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 1)
            .Add(p => p.Days, days));

        // Assert
        cut.Markup.Should().Contain("bg-green-500");
    }

    [Fact]
    public void CalendarGrid_ShowsRed_ForNonOmadCompliantDays()
    {
        // Arrange
        var days = new List<DailyLogSummary>
        {
            new() { Date = new DateOnly(2025, 1, 1), OmadCompliant = false, AlcoholConsumed = false, Weight = 175m }
        };

        // Act
        var cut = Render<CalendarGrid>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 1)
            .Add(p => p.Days, days));

        // Assert
        cut.Markup.Should().Contain("bg-red-500");
    }

    [Fact]
    public void CalendarGrid_ShowsGray_ForDaysWithNoData()
    {
        // Arrange - no days provided, all should be gray
        var cut = Render<CalendarGrid>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 1)
            .Add(p => p.Days, new List<DailyLogSummary>()));

        // Assert
        cut.Markup.Should().Contain("bg-gray-200");
    }

    [Fact]
    public void CalendarGrid_DisplaysWeight_WhenAvailable()
    {
        // Arrange
        var days = new List<DailyLogSummary>
        {
            new() { Date = new DateOnly(2025, 1, 1), OmadCompliant = true, AlcoholConsumed = false, Weight = 175m }
        };

        // Act
        var cut = Render<CalendarGrid>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 1)
            .Add(p => p.Days, days));

        // Assert
        cut.Markup.Should().Contain("175lb");
    }

    [Fact]
    public void CalendarGrid_NavigatesToPreviousMonth_OnClick()
    {
        // Arrange
        (int Year, int Month) receivedMonth = default;
        var cut = Render<CalendarGrid>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 6)
            .Add(p => p.Days, new List<DailyLogSummary>())
            .Add(p => p.OnMonthChanged, EventCallback.Factory.Create<(int, int)>(this, m => receivedMonth = m)));

        // Act
        var prevButton = cut.FindAll("button").First();
        prevButton.Click();

        // Assert
        receivedMonth.Year.Should().Be(2025);
        receivedMonth.Month.Should().Be(5);
    }

    [Fact]
    public void CalendarGrid_RendersLegend()
    {
        // Arrange & Act
        var cut = Render<CalendarGrid>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 1)
            .Add(p => p.Days, new List<DailyLogSummary>()));

        // Assert
        cut.Markup.Should().Contain("OMAD ✓");
        cut.Markup.Should().Contain("Not OMAD");
        cut.Markup.Should().Contain("No data");
    }

    [Fact]
    public void CalendarGrid_CallsDaySelected_OnDayClick()
    {
        // Arrange
        DateOnly? selectedDate = null;
        var days = new List<DailyLogSummary>
        {
            new() { Date = new DateOnly(2024, 1, 15), OmadCompliant = true }
        };

        var cut = Render<CalendarGrid>(parameters => parameters
            .Add(p => p.Year, 2024)
            .Add(p => p.Month, 1)
            .Add(p => p.Days, days)
            .Add(p => p.OnDaySelected, EventCallback.Factory.Create<DateOnly>(this, d => selectedDate = d)));

        // Act - Find and click the day 15 button
        var dayButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("15")).FirstOrDefault();
        dayButtons?.Click();

        // Assert
        selectedDate.Should().Be(new DateOnly(2024, 1, 15));
    }
}
