using Bunit;
using FluentAssertions;
using PoNovaWeight.Client.Components;
using Xunit;

namespace PoNovaWeight.Client.Tests.Components;

public class WeightTrendChartTests : BunitContext
{
    [Fact]
    public void Render_ShowsNoDataMessage_WhenEmpty()
    {
        // Arrange & Act
        var cut = Render<WeightTrendChart>(parameters => parameters
            .Add(p => p.DataPoints, new List<WeightTrendChart.TrendDataPoint>())
            .Add(p => p.TotalDaysLogged, 0)
            .Add(p => p.WeightChange, null));

        // Assert
        cut.Markup.Should().Contain("No weight data yet");
        cut.Markup.Should().Contain("Log your first weight");
    }

    [Fact]
    public void Render_ShowsTitle_WithDaysLoggedCount()
    {
        // Arrange
        var dataPoints = new List<WeightTrendChart.TrendDataPoint>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.Today), Weight = 180m, IsCarryForward = false }
        };

        // Act
        var cut = Render<WeightTrendChart>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.TotalDaysLogged, 5)
            .Add(p => p.WeightChange, -2.5m));

        // Assert
        cut.Markup.Should().Contain("Weight Trends");
        cut.Markup.Should().Contain("5 days logged");
    }

    [Fact]
    public void Render_ShowsPositiveWeightChange_InRed()
    {
        // Arrange
        var dataPoints = new List<WeightTrendChart.TrendDataPoint>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.Today), Weight = 180m, IsCarryForward = false }
        };

        // Act
        var cut = Render<WeightTrendChart>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.TotalDaysLogged, 3)
            .Add(p => p.WeightChange, 3.5m));

        // Assert
        cut.Markup.Should().Contain("+3.5 lbs");
        cut.Markup.Should().Contain("text-healthy-fruit");
    }

    [Fact]
    public void Render_ShowsNegativeWeightChange_InGreen()
    {
        // Arrange
        var dataPoints = new List<WeightTrendChart.TrendDataPoint>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.Today), Weight = 180m, IsCarryForward = false }
        };

        // Act
        var cut = Render<WeightTrendChart>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.TotalDaysLogged, 10)
            .Add(p => p.WeightChange, -5.0m));

        // Assert
        cut.Markup.Should().Contain("-5.0 lbs");
        cut.Markup.Should().Contain("text-healthy-primary");
    }

    [Fact]
    public void Render_ShowsNoChange_WhenZero()
    {
        // Arrange
        var dataPoints = new List<WeightTrendChart.TrendDataPoint>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.Today), Weight = 180m, IsCarryForward = false }
        };

        // Act
        var cut = Render<WeightTrendChart>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.TotalDaysLogged, 7)
            .Add(p => p.WeightChange, 0m));

        // Assert
        cut.Markup.Should().Contain("0.0 lbs");
        cut.Markup.Should().Contain("text-gray-600");
    }

    [Fact]
    public void Render_ShowsBlueBars_ForLoggedWeight()
    {
        // Arrange
        var dataPoints = new List<WeightTrendChart.TrendDataPoint>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), Weight = 178m, IsCarryForward = false },
            new() { Date = DateOnly.FromDateTime(DateTime.Today), Weight = 180m, IsCarryForward = false }
        };

        // Act
        var cut = Render<WeightTrendChart>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.TotalDaysLogged, 2)
            .Add(p => p.WeightChange, 2m));

        // Assert
        cut.Markup.Should().Contain("bg-healthy-primary");
    }

    [Fact]
    public void Render_ShowsLightBlueBars_ForCarryForward()
    {
        // Arrange
        var dataPoints = new List<WeightTrendChart.TrendDataPoint>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), Weight = 180m, IsCarryForward = false },
            new() { Date = DateOnly.FromDateTime(DateTime.Today), Weight = 180m, IsCarryForward = true }
        };

        // Act
        var cut = Render<WeightTrendChart>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.TotalDaysLogged, 1)
            .Add(p => p.WeightChange, 0m));

        // Assert
        cut.Markup.Should().Contain("bg-healthy-secondary");
    }

    [Fact]
    public void Render_ShowsAmberBars_ForAlcoholDays()
    {
        // Arrange
        var dataPoints = new List<WeightTrendChart.TrendDataPoint>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.Today), Weight = 182m, IsCarryForward = false, AlcoholConsumed = true }
        };

        // Act
        var cut = Render<WeightTrendChart>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.TotalDaysLogged, 1)
            .Add(p => p.WeightChange, null));

        // Assert
        cut.Markup.Should().Contain("bg-healthy-grain");
    }

    [Fact]
    public void Render_ShowsLegend()
    {
        // Arrange
        var dataPoints = new List<WeightTrendChart.TrendDataPoint>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.Today), Weight = 180m, IsCarryForward = false }
        };

        // Act
        var cut = Render<WeightTrendChart>(parameters => parameters
            .Add(p => p.DataPoints, dataPoints)
            .Add(p => p.TotalDaysLogged, 1)
            .Add(p => p.WeightChange, null));

        // Assert
        cut.Markup.Should().Contain("Logged");
        cut.Markup.Should().Contain("Carry-forward");
        cut.Markup.Should().Contain("Alcohol day");
    }
}
