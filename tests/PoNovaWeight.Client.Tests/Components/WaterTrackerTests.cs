using Bunit;
using PoNovaWeight.Client.Components;

namespace PoNovaWeight.Client.Tests.Components;

/// <summary>
/// bUnit tests for WaterTracker component.
/// </summary>
public class WaterTrackerTests : BunitContext
{
    [Fact]
    public void WaterTracker_Renders_EightSegments()
    {
        // Act
        var cut = Render<WaterTracker>(parameters => parameters
            .Add(p => p.FilledSegments, 0)
            .Add(p => p.OnSegmentChanged, _ => { }));

        // Assert
        var segments = cut.FindAll("[data-water-segment]");
        Assert.Equal(8, segments.Count);
    }

    [Fact]
    public void WaterTracker_ShowsCorrectFilledCount()
    {
        // Consolidates filled segment display tests
        var fillLevels = new[] { 0, 3, 5, 8 };

        foreach (var filled in fillLevels)
        {
            // Act
            var cut = Render<WaterTracker>(parameters => parameters
                .Add(p => p.FilledSegments, filled)
                .Add(p => p.OnSegmentChanged, _ => { }));

            // Assert - check the counter text
            var markup = cut.Markup;
            Assert.Contains($"{filled}/8", markup);
        }
    }

    [Fact]
    public void WaterTracker_ClickBehavior_HandlesAllScenarios()
    {
        // Test 1: Click on segment triggers callback with correct value
        int? clickedValue = null;
        var cut1 = Render<WaterTracker>(parameters => parameters
            .Add(p => p.FilledSegments, 0)
            .Add(p => p.OnSegmentChanged, segment => clickedValue = segment));
        var segments1 = cut1.FindAll("[data-water-segment]");
        segments1[2].Click(); // Segment 3
        Assert.Equal(3, clickedValue);

        // Test 2: Click on filled segment sets to that index
        int? toggleValue = null;
        var cut2 = Render<WaterTracker>(parameters => parameters
            .Add(p => p.FilledSegments, 5)
            .Add(p => p.OnSegmentChanged, segment => toggleValue = segment));
        var segments2 = cut2.FindAll("[data-water-segment]");
        segments2[2].Click(); // Segment 3 when 5 filled -> sets to 2
        Assert.Equal(2, toggleValue);

        // Test 3: Click first segment fills one
        int? firstClick = null;
        var cut3 = Render<WaterTracker>(parameters => parameters
            .Add(p => p.FilledSegments, 0)
            .Add(p => p.OnSegmentChanged, segment => firstClick = segment));
        cut3.FindAll("[data-water-segment]")[0].Click();
        Assert.Equal(1, firstClick);

        // Test 4: Click last segment fills all
        int? lastClick = null;
        var cut4 = Render<WaterTracker>(parameters => parameters
            .Add(p => p.FilledSegments, 0)
            .Add(p => p.OnSegmentChanged, segment => lastClick = segment));
        cut4.FindAll("[data-water-segment]")[7].Click();
        Assert.Equal(8, lastClick);
    }

    [Fact]
    public void WaterTracker_CheckmarkDisplay_BasedOnCompletionStatus()
    {
        // At 8 segments - shows checkmark (SVG icon)
        var cutComplete = Render<WaterTracker>(parameters => parameters
            .Add(p => p.FilledSegments, 8)
            .Add(p => p.OnSegmentChanged, _ => { }));
        Assert.Contains("M4.5 12.75l6 6 9-13.5", cutComplete.Markup);

        // Below 8 segments - no checkmark
        var cutIncomplete = Render<WaterTracker>(parameters => parameters
            .Add(p => p.FilledSegments, 7)
            .Add(p => p.OnSegmentChanged, _ => { }));
        Assert.DoesNotContain("M4.5 12.75l6 6 9-13.5", cutIncomplete.Markup);
    }

    [Fact]
    public void WaterTracker_BasicRendering_ShowsLabelAndSegments()
    {
        // Act
        var cut = Render<WaterTracker>(parameters => parameters
            .Add(p => p.FilledSegments, 0)
            .Add(p => p.OnSegmentChanged, _ => { }));

        // Assert - renders 8 segments and water label
        var segments = cut.FindAll("[data-water-segment]");
        Assert.Equal(8, segments.Count);
        Assert.Contains("Water", cut.Markup);
    }
}
