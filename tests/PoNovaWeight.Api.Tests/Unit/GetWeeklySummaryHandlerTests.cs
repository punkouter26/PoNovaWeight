using FluentAssertions;
using Moq;
using PoNovaWeight.Api.Features.WeeklySummary;
using PoNovaWeight.Api.Infrastructure.TableStorage;

namespace PoNovaWeight.Api.Tests.Unit;

public class GetWeeklySummaryHandlerTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly GetWeeklySummaryHandler _handler;

    public GetWeeklySummaryHandlerTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _handler = new GetWeeklySummaryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetWeeklySummary_ValidDate_ReturnsSevenDays()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15); // Wednesday
        var weekStart = new DateOnly(2025, 1, 12); // Sunday
        var weekEnd = new DateOnly(2025, 1, 18); // Saturday

        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", weekStart, weekEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyLogEntity>());

        var query = new GetWeeklySummaryQuery(date);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.WeekStart.Should().Be(weekStart);
        result.WeekEnd.Should().Be(weekEnd);
        result.Days.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetWeeklySummary_WithExistingLogs_MapsDataCorrectly()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15); // Wednesday
        var weekStart = new DateOnly(2025, 1, 12); // Sunday
        var weekEnd = new DateOnly(2025, 1, 18); // Saturday

        var existingEntities = new List<DailyLogEntity>
        {
            new()
            {
                PartitionKey = "dev-user",
                RowKey = "2025-01-13", // Monday
                Proteins = 10,
                Vegetables = 3,
                Fruits = 1,
                Starches = 1,
                Fats = 2,
                Dairy = 1,
                WaterSegments = 5
            },
            new()
            {
                PartitionKey = "dev-user",
                RowKey = "2025-01-14", // Tuesday
                Proteins = 12,
                Vegetables = 4,
                Fruits = 2,
                Starches = 2,
                Fats = 3,
                Dairy = 2,
                WaterSegments = 8
            }
        };

        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", weekStart, weekEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntities);

        var query = new GetWeeklySummaryQuery(date);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Days.Should().HaveCount(7);
        result.TotalProteins.Should().Be(22); // 10 + 12
        result.TotalVegetables.Should().Be(7); // 3 + 4
        result.TotalFruits.Should().Be(3); // 1 + 2
        result.TotalStarches.Should().Be(3); // 1 + 2
        result.TotalFats.Should().Be(5); // 2 + 3
        result.TotalDairy.Should().Be(3); // 1 + 2
    }

    [Fact]
    public async Task GetWeeklySummary_EmptyWeek_ReturnsZeroTotals()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        var weekStart = new DateOnly(2025, 1, 12);
        var weekEnd = new DateOnly(2025, 1, 18);

        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", weekStart, weekEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyLogEntity>());

        var query = new GetWeeklySummaryQuery(date);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalProteins.Should().Be(0);
        result.TotalVegetables.Should().Be(0);
        result.TotalFruits.Should().Be(0);
        result.TotalStarches.Should().Be(0);
        result.TotalFats.Should().Be(0);
        result.TotalDairy.Should().Be(0);
    }

    [Fact]
    public async Task GetWeeklySummary_DairyConversion_CalculatesProteinEquivalent()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        var weekStart = new DateOnly(2025, 1, 12);
        var weekEnd = new DateOnly(2025, 1, 18);

        var entities = new List<DailyLogEntity>
        {
            new() { PartitionKey = "dev-user", RowKey = "2025-01-13", Dairy = 3 },
            new() { PartitionKey = "dev-user", RowKey = "2025-01-14", Dairy = 2 }
        };

        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", weekStart, weekEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var query = new GetWeeklySummaryQuery(date);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalDairy.Should().Be(5);
        result.DairyAsProteinEquivalent.Should().Be(10); // 5 * 2
    }

    [Fact]
    public async Task GetWeeklySummary_AnyDayInWeek_ReturnsSameWeek()
    {
        // Consolidates week boundary tests
        var testDates = new[] { (2025, 1, 12), (2025, 1, 18), (2025, 1, 15) };
        var expectedWeekStart = new DateOnly(2025, 1, 12);
        var expectedWeekEnd = new DateOnly(2025, 1, 18);

        foreach (var (year, month, day) in testDates)
        {
            // Arrange
            var date = new DateOnly(year, month, day);

            _repositoryMock
                .Setup(r => r.GetRangeAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DailyLogEntity>());

            var query = new GetWeeklySummaryQuery(date);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.WeekStart.Should().Be(expectedWeekStart, $"date {date} should resolve to same week start");
            result.WeekEnd.Should().Be(expectedWeekEnd, $"date {date} should resolve to same week end");
        }
    }
}
