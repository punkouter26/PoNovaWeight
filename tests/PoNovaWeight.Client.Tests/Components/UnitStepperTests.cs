using Bunit;
using FluentAssertions;
using PoNovaWeight.Client.Components;
using PoNovaWeight.Shared.Contracts;

namespace PoNovaWeight.Client.Tests.Components;

public class UnitStepperTests : TestContext
{
    [Fact]
    public void UnitStepper_TapPlus_IncrementsValue()
    {
        // Arrange
        int receivedDelta = 0;
        var cut = RenderComponent<UnitStepper>(parameters => parameters
            .Add(p => p.Category, UnitCategory.Proteins)
            .Add(p => p.Current, 5)
            .Add(p => p.OnValueChanged, EventCallback.Factory.Create<int>(this, delta => receivedDelta = delta)));

        // Act - click the plus button
        var plusButton = cut.FindAll("button").Last();
        plusButton.Click();

        // Assert
        receivedDelta.Should().Be(1);
    }

    [Fact]
    public void UnitStepper_TapMinus_DecrementsValue()
    {
        // Arrange
        int receivedDelta = 0;
        var cut = RenderComponent<UnitStepper>(parameters => parameters
            .Add(p => p.Category, UnitCategory.Proteins)
            .Add(p => p.Current, 5)
            .Add(p => p.OnValueChanged, EventCallback.Factory.Create<int>(this, delta => receivedDelta = delta)));

        // Act - click the minus button
        var minusButton = cut.FindAll("button").First();
        minusButton.Click();

        // Assert
        receivedDelta.Should().Be(-1);
    }

    [Fact]
    public void UnitStepper_MinusAtZero_ButtonIsDisabled()
    {
        // Arrange
        var cut = RenderComponent<UnitStepper>(parameters => parameters
            .Add(p => p.Category, UnitCategory.Proteins)
            .Add(p => p.Current, 0)
            .Add(p => p.OnValueChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert - minus button should be disabled
        var minusButton = cut.FindAll("button").First();
        minusButton.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void UnitStepper_DisplaysCurrentValue()
    {
        // Arrange
        var cut = RenderComponent<UnitStepper>(parameters => parameters
            .Add(p => p.Category, UnitCategory.Proteins)
            .Add(p => p.Current, 10)
            .Add(p => p.OnValueChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert
        cut.Markup.Should().Contain("10");
    }

    [Fact]
    public void UnitStepper_DisplaysCategoryName()
    {
        // Arrange
        var cut = RenderComponent<UnitStepper>(parameters => parameters
            .Add(p => p.Category, UnitCategory.Vegetables)
            .Add(p => p.Current, 3)
            .Add(p => p.OnValueChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert
        cut.Markup.Should().Contain("Vegetables");
    }

    [Fact]
    public void UnitStepper_OverTarget_ShowsWarning()
    {
        // Arrange - Proteins target is 15, set to 16 to exceed
        var cut = RenderComponent<UnitStepper>(parameters => parameters
            .Add(p => p.Category, UnitCategory.Proteins)
            .Add(p => p.Current, 16)
            .Add(p => p.OnValueChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert - should show "Over" badge and danger progress bar
        cut.Markup.Should().Contain(">Over<");
        cut.Markup.Should().Contain("progress-fill-danger");
    }

    [Fact]
    public void UnitStepper_UnderTarget_NoWarning()
    {
        // Arrange - Proteins target is 15, set to 10
        var cut = RenderComponent<UnitStepper>(parameters => parameters
            .Add(p => p.Category, UnitCategory.Proteins)
            .Add(p => p.Current, 10)
            .Add(p => p.OnValueChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert - should not show "Over" badge
        cut.Markup.Should().NotContain(">Over<");
    }

    [Fact]
    public void UnitStepper_ShowsCorrectTarget()
    {
        // Consolidates all category target tests
        var testCases = new (UnitCategory category, int expectedTarget)[]
        {
            (UnitCategory.Proteins, 15),
            (UnitCategory.Vegetables, 5),
            (UnitCategory.Fruits, 2),
            (UnitCategory.Starches, 2),
            (UnitCategory.Fats, 4),
            (UnitCategory.Dairy, 3)
        };

        foreach (var (category, expectedTarget) in testCases)
        {
            // Arrange
            var cut = RenderComponent<UnitStepper>(parameters => parameters
                .Add(p => p.Category, category)
                .Add(p => p.Current, 0)
                .Add(p => p.OnValueChanged, EventCallback.Factory.Create<int>(this, _ => { })));

            // Assert - should display "0 / {target}"
            cut.Markup.Should().Contain($"0 / {expectedTarget}", $"{category} should have target {expectedTarget}");
        }
    }
}
