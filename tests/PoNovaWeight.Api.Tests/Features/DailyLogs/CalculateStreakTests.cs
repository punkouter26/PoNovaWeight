using Microsoft.Extensions.Time.Testing;
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
    private readonly DateOnly _fixedToday = new(2026, 2, 4);

    public CalculateStreakTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(_fixedToday.ToDateTime(TimeOnly.MinValue)));
        _handler = new CalculateStreakHandler(_repositoryMock.Object, timeProvider);
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
        var today = _fixedToday;
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
        var today = _fixedToday;
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
    public async Task Handle_UnloggedDaysBreakStreak()
    {
        // Arrange - unlogged day (null OmadCompliant) BREAKS streak (consecutive days required)
        var today = _fixedToday;
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, omadCompliant: true),
            CreateEntity(today.AddDays(-1), omadCompliant: null), // Unlogged - breaks streak
            CreateEntity(today.AddDays(-2), omadCompliant: true)
        };

        var query = new CalculateStreakQuery();
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - streak is only 1 (today), unlogged day breaks it
        Assert.Equal(1, result.CurrentStreak);
        Assert.Equal(today, result.StreakStartDate);
    }

    [Fact]
    public async Task Handle_ReturnsZero_WhenMostRecentDayIsNonCompliant()
    {
        // Arrange
        var today = _fixedToday;
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
    public async Task Handle_ReturnsZero_WhenTodayIsUnlogged()
    {
        // Arrange - most recent days are unlogged, streak must start from today
        var today = _fixedToday;
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, omadCompliant: null), // Unlogged today - no streak can start
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

        // Assert - no active streak since today is not compliant (streak requires consecutive days from today)
        Assert.Equal(0, result.CurrentStreak);
        Assert.Null(result.StreakStartDate);
    }

    [Fact]
    public async Task Handle_GapsInDates_BreakStreak()
    {
        // Arrange - there's a gap in dates (no entry for day -1)
        var today = _fixedToday;
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, omadCompliant: true),
            // No entry for today.AddDays(-1) - gap breaks streak
            CreateEntity(today.AddDays(-2), omadCompliant: true)
        };

        var query = new CalculateStreakQuery();
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - streak is only 1 (today) because missing day breaks consecutive requirement
        Assert.Equal(1, result.CurrentStreak);
        Assert.Equal(today, result.StreakStartDate);
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
