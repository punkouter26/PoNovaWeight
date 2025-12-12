using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Infrastructure.TableStorage;

namespace PoNovaWeight.Api.Tests.Features.DailyLogs;

/// <summary>
/// Unit tests for CalculateStreakHandler.
/// Tests both basic streak counting and the unlogged days rule.
/// </summary>
public class CalculateStreakTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly CalculateStreakHandler _handler;

    public CalculateStreakTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _handler = new CalculateStreakHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsZero_WhenNoLogs()
    {
        // Arrange
        var query = new CalculateStreakQuery();
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyLogEntity>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.CurrentStreak);
        Assert.Null(result.StreakStartDate);
    }

    [Fact]
    public async Task Handle_CountsConsecutiveOmadDays()
    {
        // Arrange - 3 consecutive OMAD-compliant days
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, omadCompliant: true),
            CreateEntity(today.AddDays(-1), omadCompliant: true),
            CreateEntity(today.AddDays(-2), omadCompliant: true)
        };

        var query = new CalculateStreakQuery();
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.CurrentStreak);
        Assert.Equal(today.AddDays(-2), result.StreakStartDate);
    }

    [Fact]
    public async Task Handle_BreaksStreak_OnNonCompliantDay()
    {
        // Arrange - streak broken by non-compliant day
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, omadCompliant: true),
            CreateEntity(today.AddDays(-1), omadCompliant: false), // Breaks streak
            CreateEntity(today.AddDays(-2), omadCompliant: true)
        };

        var query = new CalculateStreakQuery();
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.CurrentStreak); // Only today counts
        Assert.Equal(today, result.StreakStartDate);
    }

    [Fact]
    public async Task Handle_UnloggedDaysDoNotBreakStreak()
    {
        // Arrange - unlogged day (null OmadCompliant) should NOT break streak
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, omadCompliant: true),
            CreateEntity(today.AddDays(-1), omadCompliant: null), // Unlogged - should be skipped
            CreateEntity(today.AddDays(-2), omadCompliant: true)
        };

        var query = new CalculateStreakQuery();
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - streak should be 2 (today + 2 days ago), skipping unlogged day
        Assert.Equal(2, result.CurrentStreak);
        Assert.Equal(today.AddDays(-2), result.StreakStartDate);
    }

    [Fact]
    public async Task Handle_ReturnsZero_WhenMostRecentDayIsNonCompliant()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, omadCompliant: false),
            CreateEntity(today.AddDays(-1), omadCompliant: true),
            CreateEntity(today.AddDays(-2), omadCompliant: true)
        };

        var query = new CalculateStreakQuery();
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.CurrentStreak);
        Assert.Null(result.StreakStartDate);
    }

    [Fact]
    public async Task Handle_CountsFromMostRecentCompliantDay_SkippingNullsAtEnd()
    {
        // Arrange - most recent days are unlogged, but have OMAD compliant days before
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, omadCompliant: null), // Unlogged today
            CreateEntity(today.AddDays(-1), omadCompliant: null), // Unlogged yesterday
            CreateEntity(today.AddDays(-2), omadCompliant: true),
            CreateEntity(today.AddDays(-3), omadCompliant: true)
        };

        var query = new CalculateStreakQuery();
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - streak is 2 from the compliant days
        Assert.Equal(2, result.CurrentStreak);
        Assert.Equal(today.AddDays(-3), result.StreakStartDate);
    }

    [Fact]
    public async Task Handle_HandlesGapsInDates_TreatingMissingDaysAsUnlogged()
    {
        // Arrange - there's a gap in dates (no entry for day -1)
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, omadCompliant: true),
            // No entry for today.AddDays(-1) - should be treated as unlogged
            CreateEntity(today.AddDays(-2), omadCompliant: true)
        };

        var query = new CalculateStreakQuery();
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - streak continues through the gap (missing day = unlogged = doesn't break)
        Assert.Equal(2, result.CurrentStreak);
    }

    private static DailyLogEntity CreateEntity(DateOnly date, bool? omadCompliant)
    {
        return new DailyLogEntity
        {
            PartitionKey = "dev-user",
            RowKey = date.ToString("yyyy-MM-dd"),
            OmadCompliant = omadCompliant
        };
    }
}
