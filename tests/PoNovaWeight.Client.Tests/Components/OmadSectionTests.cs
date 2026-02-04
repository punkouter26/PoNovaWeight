using Bunit;
using FluentAssertions;
using PoNovaWeight.Client.Components;

namespace PoNovaWeight.Client.Tests.Components;

public class OmadSectionTests : BunitContext
{
    [Fact]
    public void OmadSection_RendersWithNullValues()
    {
        // Act
        var cut = Render<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null));

        // Assert
        cut.Markup.Should().Contain("Daily Health Tracking");
        cut.Markup.Should().Contain("Weight (lbs)");
        cut.Markup.Should().Contain("Did you eat OMAD today?");
        cut.Markup.Should().Contain("Did you drink alcohol today?");
    }

    [Fact]
    public void OmadSection_DisplaysWeight_WhenProvided()
    {
        // Act
        var cut = Render<OmadSection>(parameters => parameters
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
        var cut = Render<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null)
            .Add(p => p.OmadCompliantChanged, EventCallback.Factory.Create<bool?>(this, v => receivedValue = v)));

        // Act - click the OMAD Yes radio
        var radios = cut.FindAll("input[type='radio'][name='omad']");
        radios[0].Change(true); // Yes radio

        // Assert - null -> true
        receivedValue.Should().BeTrue();
    }

    [Fact]
    public void OmadSection_ToggleAlcohol_CallsCallback()
    {
        // Arrange
        bool? receivedValue = null;
        var cut = Render<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null)
            .Add(p => p.AlcoholConsumedChanged, EventCallback.Factory.Create<bool?>(this, v => receivedValue = v)));

        // Act - click the Alcohol Yes radio
        var radios = cut.FindAll("input[type='radio'][name='alcohol']");
        radios[0].Change(true); // Yes radio

        // Assert - null -> true
        receivedValue.Should().BeTrue();
    }

    [Fact]
    public void OmadSection_OmadTrue_ClickingAgain_KeepsValue()
    {
        // Arrange
        bool? receivedValue = true; // Start non-null
        var cut = Render<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, true) // Start with true
            .Add(p => p.AlcoholConsumed, null)
            .Add(p => p.OmadCompliantChanged, EventCallback.Factory.Create<bool?>(this, v => receivedValue = v)));

        // Act - clicking Yes again should keep the value (Clear button is used to clear)
        var radios = cut.FindAll("input[type='radio'][name='omad']");
        radios[0].Change(true); // Click Yes radio again when already true

        // Assert - value stays true (no toggle, use Clear button to clear)
        receivedValue.Should().BeTrue();
    }

    [Fact]
    public void OmadSection_OmadFalse_ClickingAgain_KeepsValue()
    {
        // Arrange
        bool? receivedValue = false; // Set non-null initial
        var cut = Render<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, false) // Start with false
            .Add(p => p.AlcoholConsumed, null)
            .Add(p => p.OmadCompliantChanged, EventCallback.Factory.Create<bool?>(this, v => receivedValue = v)));

        // Act - clicking No again should keep the value (Clear button is used to clear)
        var radios = cut.FindAll("input[type='radio'][name='omad']");
        radios[1].Change(true); // Click No radio again when already false

        // Assert - value stays false (no toggle, use Clear button to clear)
        receivedValue.Should().BeFalse();
    }

    [Fact]
    public void OmadSection_ShowsWarning_WhenWeightChangeExceeds5Lbs()
    {
        // Arrange
        var cut = Render<OmadSection>(parameters => parameters
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
        var cut = Render<OmadSection>(parameters => parameters
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
        var cut = Render<OmadSection>(parameters => parameters
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
        var cut = Render<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, null));

        // Assert - clear button should not exist
        var clearButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title") == "Clear weight");
        clearButton.Should().BeNull();
    }

    [Fact]
    public void OmadSection_ClearOmadButton_ShowsWhenOmadHasValue()
    {
        // Arrange
        var cut = Render<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, true)
            .Add(p => p.AlcoholConsumed, null));

        // Assert - clear button for OMAD should be visible
        cut.Markup.Should().Contain("Clear"); // The ✕ Clear button
    }

    [Fact]
    public void OmadSection_ClearOmadButton_ClearsValue()
    {
        // Arrange
        bool? receivedValue = true;
        var cut = Render<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, true)
            .Add(p => p.AlcoholConsumed, null)
            .Add(p => p.OmadCompliantChanged, EventCallback.Factory.Create<bool?>(this, v => receivedValue = v)));

        // Act - click the Clear button (find button containing "Clear" text in the OMAD section)
        var clearButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Clear")).ToList();
        // First Clear button should be for OMAD
        clearButtons[0].Click();

        // Assert - value is cleared to null
        receivedValue.Should().BeNull();
    }

    [Fact]
    public void OmadSection_ClearAlcoholButton_ClearsValue()
    {
        // Arrange
        bool? receivedValue = true;
        var cut = Render<OmadSection>(parameters => parameters
            .Add(p => p.Weight, null)
            .Add(p => p.OmadCompliant, null)
            .Add(p => p.AlcoholConsumed, true)
            .Add(p => p.AlcoholConsumedChanged, EventCallback.Factory.Create<bool?>(this, v => receivedValue = v)));

        // Act - click the Clear button for Alcohol
        var clearButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Clear")).ToList();
        // With only alcohol set, this should be the only Clear button
        clearButtons[0].Click();

        // Assert - value is cleared to null
        receivedValue.Should().BeNull();
    }
}
