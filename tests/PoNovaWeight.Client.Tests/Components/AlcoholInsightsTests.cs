using Bunit;
using FluentAssertions;
using PoNovaWeight.Client.Components;
using PoNovaWeight.Shared.DTOs;
using Xunit;

namespace PoNovaWeight.Client.Tests.Components;

public class AlcoholInsightsTests : BunitContext
{
    [Fact]
    public void Render_ShowsNoDataMessage_WhenNoData()
    {
        // Arrange
        var correlation = new AlcoholCorrelationDto
        {
            HasSufficientData = false,
            DaysWithAlcohol = 0,
            DaysWithoutAlcohol = 0
        };

        // Act
        var cut = Render<AlcoholInsights>(parameters => parameters
            .Add(p => p.Correlation, correlation));

        // Assert
        cut.Markup.Should().Contain("Need more data for correlation");
    }

    [Fact]
    public void Render_ShowsNoAlcoholDaysMessage_WhenNoAlcoholDays()
    {
        // Arrange
        var correlation = new AlcoholCorrelationDto
        {
            HasSufficientData = false,
            DaysWithAlcohol = 0,
            DaysWithoutAlcohol = 5
        };

        // Act
        var cut = Render<AlcoholInsights>(parameters => parameters
            .Add(p => p.Correlation, correlation));

        // Assert
        cut.Markup.Should().Contain("Need more data for correlation");
    }

    [Fact]
    public void Render_ShowsNoSoberDaysMessage_WhenNoNonAlcoholDays()
    {
        // Arrange
        var correlation = new AlcoholCorrelationDto
        {
            HasSufficientData = false,
            DaysWithAlcohol = 5,
            DaysWithoutAlcohol = 0
        };

        // Act
        var cut = Render<AlcoholInsights>(parameters => parameters
            .Add(p => p.Correlation, correlation));

        // Assert
        cut.Markup.Should().Contain("Need more data for correlation");
    }

    [Fact]
    public void Render_ShowsAverages_WhenSufficientData()
    {
        // Arrange
        var correlation = new AlcoholCorrelationDto
        {
            HasSufficientData = true,
            DaysWithAlcohol = 10,
            DaysWithoutAlcohol = 20,
            AverageWeightWithAlcohol = 183.5m,
            AverageWeightWithoutAlcohol = 179.2m,
            WeightDifference = 4.3m
        };

        // Act
        var cut = Render<AlcoholInsights>(parameters => parameters
            .Add(p => p.Correlation, correlation));

        // Assert
        cut.Markup.Should().Contain("183.5 lbs");
        cut.Markup.Should().Contain("179.2 lbs");
        cut.Markup.Should().Contain("10 days");
        cut.Markup.Should().Contain("20 days");
    }

    [Fact]
    public void Render_ShowsPositiveDifference_InRed()
    {
        // Arrange
        var correlation = new AlcoholCorrelationDto
        {
            HasSufficientData = true,
            DaysWithAlcohol = 5,
            DaysWithoutAlcohol = 10,
            AverageWeightWithAlcohol = 185m,
            AverageWeightWithoutAlcohol = 180m,
            WeightDifference = 5m
        };

        // Act
        var cut = Render<AlcoholInsights>(parameters => parameters
            .Add(p => p.Correlation, correlation));

        // Assert
        cut.Markup.Should().Contain("+5.0 lbs");
        cut.Markup.Should().Contain("text-red-600");
        cut.Markup.Should().Contain("heavier with");
    }

    [Fact]
    public void Render_ShowsNegativeDifference_InGreen()
    {
        // Arrange
        var correlation = new AlcoholCorrelationDto
        {
            HasSufficientData = true,
            DaysWithAlcohol = 5,
            DaysWithoutAlcohol = 10,
            AverageWeightWithAlcohol = 175m,
            AverageWeightWithoutAlcohol = 180m,
            WeightDifference = -5m
        };

        // Act
        var cut = Render<AlcoholInsights>(parameters => parameters
            .Add(p => p.Correlation, correlation));

        // Assert
        cut.Markup.Should().Contain("-5.0 lbs");
        cut.Markup.Should().Contain("text-green-600");
        cut.Markup.Should().Contain("lighter with");
    }

    [Fact]
    public void Render_ShowsInsightMessage_WhenDifferenceIsSignificant()
    {
        // Arrange
        var correlation = new AlcoholCorrelationDto
        {
            HasSufficientData = true,
            DaysWithAlcohol = 5,
            DaysWithoutAlcohol = 10,
            AverageWeightWithAlcohol = 185m,
            AverageWeightWithoutAlcohol = 180m,
            WeightDifference = 5m
        };

        // Act
        var cut = Render<AlcoholInsights>(parameters => parameters
            .Add(p => p.Correlation, correlation));

        // Assert
        cut.Markup.Should().Contain("alcohol days correlate with higher weight");
    }

    [Fact]
    public void Render_ShowsTitle_WithBeerEmoji()
    {
        // Arrange
        var correlation = new AlcoholCorrelationDto
        {
            HasSufficientData = false,
            DaysWithAlcohol = 0,
            DaysWithoutAlcohol = 0
        };

        // Act
        var cut = Render<AlcoholInsights>(parameters => parameters
            .Add(p => p.Correlation, correlation));

        // Assert
        cut.Markup.Should().Contain("Alcohol Impact");
        cut.Markup.Should().Contain("🍺");
    }
}
