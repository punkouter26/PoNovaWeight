using Bunit;
using FluentAssertions;
using PoNovaWeight.Client.Components;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Client.Tests.Components;

public class StreakDisplayTests : BunitContext
{
    [Fact]
    public void StreakDisplay_ShowsZeroStreak_WithMotivation()
    {
        // Arrange
        var streak = new StreakDto { CurrentStreak = 0, StreakStartDate = null };

        // Act
        var cut = Render<StreakDisplay>(parameters => parameters
            .Add(p => p.Streak, streak));

        // Assert
        cut.Markup.Should().Contain("0");
        cut.Markup.Should().Contain("Start your streak today!");
    }

    [Fact]
    public void StreakDisplay_ShowsStreakCount()
    {
        // Arrange
        var streak = new StreakDto
        {
            CurrentStreak = 5,
            StreakStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-4))
        };

        // Act
        var cut = Render<StreakDisplay>(parameters => parameters
            .Add(p => p.Streak, streak));

        // Assert
        cut.Markup.Should().Contain("5");
        cut.Markup.Should().Contain("Day OMAD Streak");
    }

    [Fact]
    public void StreakDisplay_ShowsStartDate()
    {
        // Arrange
        var startDate = new DateOnly(2025, 12, 1);
        var streak = new StreakDto
        {
            CurrentStreak = 10,
            StreakStartDate = startDate
        };

        // Act
        var cut = Render<StreakDisplay>(parameters => parameters
            .Add(p => p.Streak, streak));

        // Assert
        cut.Markup.Should().Contain("Dec 1");
        cut.Markup.Should().Contain("Started");
    }

    [Fact]
    public void StreakDisplay_ShowsUpToSevenCheckmarks()
    {
        // Arrange
        var streak = new StreakDto
        {
            CurrentStreak = 7,
            StreakStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-6))
        };

        // Act
        var cut = Render<StreakDisplay>(parameters => parameters
            .Add(p => p.Streak, streak));

        // Assert - should have 7 checkmark circles (the rounded-full divs containing ✓)
        var checkmarks = cut.FindAll("div.rounded-full");
        checkmarks.Should().HaveCount(7);
    }

    [Fact]
    public void StreakDisplay_ShowsPlusMore_WhenStreakExceedsSeven()
    {
        // Arrange
        var streak = new StreakDto
        {
            CurrentStreak = 15,
            StreakStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-14))
        };

        // Act
        var cut = Render<StreakDisplay>(parameters => parameters
            .Add(p => p.Streak, streak));

        // Assert
        cut.Markup.Should().Contain("+8 more");
    }

    [Fact]
    public void StreakDisplay_ShowsFireEmoji()
    {
        // Arrange
        var streak = new StreakDto { CurrentStreak = 3, StreakStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)) };

        // Act
        var cut = Render<StreakDisplay>(parameters => parameters
            .Add(p => p.Streak, streak));

        // Assert
        cut.Markup.Should().Contain("🔥");
    }

    [Fact]
    public void StreakDisplay_HandlesNullStreak()
    {
        // Act
        var cut = Render<StreakDisplay>(parameters => parameters
            .Add(p => p.Streak, null));

        // Assert - should not throw and render something
        cut.Markup.Should().NotBeEmpty();
    }
}
