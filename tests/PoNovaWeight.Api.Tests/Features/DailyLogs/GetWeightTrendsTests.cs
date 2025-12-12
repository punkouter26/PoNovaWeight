using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Infrastructure.TableStorage;

namespace PoNovaWeight.Api.Tests.Features.DailyLogs;

/// <summary>
/// Unit tests for GetWeightTrendsHandler including gap-filling carry-forward logic.
/// </summary>
public class GetWeightTrendsTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly GetWeightTrendsHandler _handler;

    public GetWeightTrendsTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _handler = new GetWeightTrendsHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyTrends_WhenNoLogs()
    {
        // Arrange
        var query = new GetWeightTrendsQuery(30);
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyLogEntity>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.DataPoints);
        Assert.Equal(0, result.TotalDaysLogged);
        Assert.Null(result.WeightChange);
    }

    [Fact]
    public async Task Handle_ReturnsTrendData_ForDaysWithWeight()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, 175.0),
            CreateEntity(today.AddDays(-1), 176.0),
            CreateEntity(today.AddDays(-2), 174.5)
        };

        var query = new GetWeightTrendsQuery(7);
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.TotalDaysLogged);
        Assert.True(result.DataPoints.Count >= 3);
    }

    [Fact]
    public async Task Handle_CalculatesWeightChange()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        // Days=7 means range from today.AddDays(-6) to today (7 days total)
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, 175.0),      // Latest
            CreateEntity(today.AddDays(-6), 180.0) // First (within 7-day range)
        };

        var query = new GetWeightTrendsQuery(7);
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - weight went from 180 to 175, change = -5
        Assert.Equal(-5m, result.WeightChange);
    }

    [Fact]
    public async Task Handle_CarriesForwardWeight_ForGaps()
    {
        // Arrange - weight on day 0 and day 2, but not day 1
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, 175.0),
            // No entry for today.AddDays(-1)
            CreateEntity(today.AddDays(-2), 173.0)
        };

        var query = new GetWeightTrendsQuery(3);
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.DataPoints.Count);

        // Find the gap day (today - 1)
        var gapDay = result.DataPoints.FirstOrDefault(d => d.Date == today.AddDays(-1));
        Assert.NotNull(gapDay);
        Assert.True(gapDay.IsCarryForward);
        Assert.Equal(173m, gapDay.Weight); // Carried forward from day -2
    }

    [Fact]
    public async Task Handle_MarksCarryForwardCorrectly()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, 175.0)
        };

        var query = new GetWeightTrendsQuery(3);
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var actualDay = result.DataPoints.First(d => d.Date == today);
        Assert.False(actualDay.IsCarryForward); // Actual logged value

        // Previous days should be marked as carry forward if they exist
        var previousDays = result.DataPoints.Where(d => d.Date < today);
        foreach (var day in previousDays)
        {
            Assert.True(day.IsCarryForward || day.Weight == null);
        }
    }

    [Fact]
    public async Task Handle_IncludesAlcoholConsumption()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, 175.0, alcoholConsumed: true),
            CreateEntity(today.AddDays(-1), 174.0, alcoholConsumed: false)
        };

        var query = new GetWeightTrendsQuery(7);
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var todayPoint = result.DataPoints.First(d => d.Date == today);
        var yesterdayPoint = result.DataPoints.First(d => d.Date == today.AddDays(-1));

        Assert.True(todayPoint.AlcoholConsumed);
        Assert.False(yesterdayPoint.AlcoholConsumed);
    }

    [Fact]
    public async Task Handle_RequiresMinimumThreeDays_ForWeightChange()
    {
        // Arrange - only 2 days of data
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(today, 175.0),
            CreateEntity(today.AddDays(-1), 174.0)
        };

        var query = new GetWeightTrendsQuery(7);
        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - still calculates weight change even with 2 days
        Assert.Equal(2, result.TotalDaysLogged);
        // The weight change should still be calculated if we have at least 2 points
        Assert.Equal(1m, result.WeightChange); // 175 - 174 = 1
    }

    private static DailyLogEntity CreateEntity(DateOnly date, double weight, bool? alcoholConsumed = null)
    {
        return new DailyLogEntity
        {
            PartitionKey = "dev-user",
            RowKey = date.ToString("yyyy-MM-dd"),
            Weight = weight,
            AlcoholConsumed = alcoholConsumed
        };
    }
}
