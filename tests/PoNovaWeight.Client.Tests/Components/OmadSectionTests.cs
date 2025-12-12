using Bunit;
using FluentAssertions;
using PoNovaWeight.Client.Components;

namespace PoNovaWeight.Client.Tests.Components;

public class OmadSectionTests : TestContext
{
    [Fact]
    public void OmadSection_RendersWithNullValues()
    {
        // Act
        var cut = RenderComponent<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null));

        // Assert
        cut.Markup.Should().Contain("Daily Health Tracking");
        cut.Markup.Should().Contain("Weight (lbs)");
        cut.Markup.Should().Contain("OMAD Compliant");
        cut.Markup.Should().Contain("Alcohol Consumed");
    }

    [Fact]
    public void OmadSection_DisplaysWeight_WhenProvided()
    {
        // Act
        var cut = RenderComponent<OmadSection>(parameters => parameters
            .Add(p => p.Weight, 175.5m)
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null));

        // Assert
        var input = cut.Find("input[type='number']");
        input.GetAttribute("value").Should().Be("175.5");
    }

    [Fact]
    public void OmadSection_ToggleOmad_CallsCallback()
    {
        // Arrange
        bool? receivedValue = null;
        var cut = RenderComponent<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null)
            .Add(p => p.OmadCompliantChanged, EventCallback.Factory.Create<bool?>(this, v => receivedValue = v)));

        // Act - click the OMAD toggle button
        var buttons = cut.FindAll("button[aria-pressed]");
        buttons[0].Click(); // OMAD is first toggle

        // Assert - null -> true
        receivedValue.Should().BeTrue();
    }

    [Fact]
    public void OmadSection_ToggleAlcohol_CallsCallback()
    {
        // Arrange
        bool? receivedValue = null;
        var cut = RenderComponent<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null)
            .Add(p => p.AlcoholConsumedChanged, EventCallback.Factory.Create<bool?>(this, v => receivedValue = v)));

        // Act - click the Alcohol toggle button
        var buttons = cut.FindAll("button[aria-pressed]");
        buttons[1].Click(); // Alcohol is second toggle

        // Assert - null -> true
        receivedValue.Should().BeTrue();
    }

    [Fact]
    public void OmadSection_OmadTrue_TogglesTo_False()
    {
        // Arrange
        bool? receivedValue = null;
        var cut = RenderComponent<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, true) // Start with true
            .Add(p => p.AlcoholConsumed, null)
            .Add(p => p.OmadCompliantChanged, EventCallback.Factory.Create<bool?>(this, v => receivedValue = v)));

        // Act
        var buttons = cut.FindAll("button[aria-pressed]");
        buttons[0].Click();

        // Assert - true -> false
        receivedValue.Should().BeFalse();
    }

    [Fact]
    public void OmadSection_OmadFalse_TogglesTo_Null()
    {
        // Arrange
        bool? receivedValue = true; // Set non-null initial to verify it becomes null
        var cut = RenderComponent<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, false) // Start with false
            .Add(p => p.AlcoholConsumed, null)
            .Add(p => p.OmadCompliantChanged, EventCallback.Factory.Create<bool?>(this, v => receivedValue = v)));

        // Act
        var buttons = cut.FindAll("button[aria-pressed]");
        buttons[0].Click();

        // Assert - false -> null
        receivedValue.Should().BeNull();
    }

    [Fact]
    public void OmadSection_ShowsWarning_WhenWeightChangeExceeds5Lbs()
    {
        // Arrange
        var cut = RenderComponent<OmadSection>(parameters => parameters
            .Add(p => p.Weight, 180m)
            .Add(p => p.PreviousDayWeight, 170m) // 10 lb difference
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null));

        // Assert
        cut.Markup.Should().Contain("Weight changed by 10");
        cut.Markup.Should().Contain("lbs from yesterday");
    }

    [Fact]
    public void OmadSection_NoWarning_WhenWeightChangeWithin5Lbs()
    {
        // Arrange
        var cut = RenderComponent<OmadSection>(parameters => parameters
            .Add(p => p.Weight, 175m)
            .Add(p => p.PreviousDayWeight, 173m) // 2 lb difference
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null));

        // Assert
        cut.Markup.Should().NotContain("Weight changed by");
    }

    [Fact]
    public void OmadSection_ClearWeight_ShowsButton_WhenWeightExists()
    {
        // Arrange
        var cut = RenderComponent<OmadSection>(parameters => parameters
            .Add(p => p.Weight, 175m)
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null));

        // Assert - clear button should exist (button without aria-pressed)
        var clearButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Clear weight");
        clearButton.Should().NotBeNull();
    }

    [Fact]
    public void OmadSection_ClearWeight_NotShown_WhenNoWeight()
    {
        // Arrange
        var cut = RenderComponent<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null));

        // Assert - clear button should not exist
        var clearButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Clear weight");
        clearButton.Should().BeNull();
    }
}
