using FluentAssertions;
using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Features.WeeklySummary;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Api.Tests.TestAuth;

namespace PoNovaWeight.Api.Tests.Unit;

/// <summary>
/// Consolidated unit tests for DailyLog and WeeklySummary handlers.
/// Uses Theory/InlineData to reduce test count while maintaining coverage.
/// </summary>
public class DailyLogAndWeeklySummaryConsolidatedTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly GetDailyLogHandler _dailyLogHandler;
    private readonly GetWeeklySummaryHandler _weeklySummaryHandler;

    public DailyLogAndWeeklySummaryConsolidatedTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _dailyLogHandler = new GetDailyLogHandler(_repositoryMock.Object);
        
        var cache = new FakeMemoryCache();
        
        _weeklySummaryHandler = new GetWeeklySummaryHandler(_repositoryMock.Object, cache);
    }

    #region GetDailyLog Tests

    [Theory]
    [InlineData("2025-01-15", true, 10, 3, 5, "existing entry with data")]
    [InlineData("2025-02-01", false, 0, 0, 0, "non-existent entry returns null")]
    public async Task GetDailyLog_VariousScenarios_ReturnsCorrectResult(
        string dateStr, bool exists, int proteins, int vegetables, int water, string scenario)
    {
        // Arrange
        var date = DateOnly.Parse(dateStr);
        var entity = exists ? new DailyLogEntity
        {
            PartitionKey = "dev-user",
            RowKey = dateStr,
            Proteins = proteins,
            Vegetables = vegetables,
            WaterSegments = water
        } : null;

        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _dailyLogHandler.Handle(new GetDailyLogQuery(date), CancellationToken.None);

        // Assert
        if (exists)
        {
            result.Should().NotBeNull(because: scenario);
            result!.Proteins.Should().Be(proteins);
            result.Vegetables.Should().Be(vegetables);
            result.WaterSegments.Should().Be(water);
        }
        else
        {
            result.Should().BeNull(because: scenario);
        }
    }

    [Theory]
    [InlineData("custom-user@test.com")]
    [InlineData("another-user@example.org")]
    public async Task GetDailyLog_CustomUserId_QueriesCorrectPartition(string userId)
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        _repositoryMock
            .Setup(r => r.GetAsync(userId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyLogEntity?)null);

        // Act
        await _dailyLogHandler.Handle(new GetDailyLogQuery(date, userId), CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.GetAsync(userId, date, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetWeeklySummary Tests

    [Theory]
    [InlineData("2025-01-12", "2025-01-12", "2025-01-18", "Sunday (week start)")]
    [InlineData("2025-01-15", "2025-01-12", "2025-01-18", "Wednesday (mid-week)")]
    [InlineData("2025-01-18", "2025-01-12", "2025-01-18", "Saturday (week end)")]
    public async Task GetWeeklySummary_AnyDayInWeek_ResolvesToCorrectWeekBounds(
        string inputDate, string expectedStart, string expectedEnd, string scenario)
    {
        // Arrange
        var date = DateOnly.Parse(inputDate);
        var weekStart = DateOnly.Parse(expectedStart);
        var weekEnd = DateOnly.Parse(expectedEnd);

        _repositoryMock
            .Setup(r => r.GetRangeAsync(It.IsAny<string>(), weekStart, weekEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyLogEntity>());

        // Act
        var result = await _weeklySummaryHandler.Handle(new GetWeeklySummaryQuery(date), CancellationToken.None);

        // Assert
        result.WeekStart.Should().Be(weekStart, because: scenario);
        result.WeekEnd.Should().Be(weekEnd, because: scenario);
        result.Days.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetWeeklySummary_WithData_CalculatesTotalsCorrectly()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        var weekStart = new DateOnly(2025, 1, 12);
        var weekEnd = new DateOnly(2025, 1, 18);

        var entities = new List<DailyLogEntity>
        {
            new() { PartitionKey = "dev-user", RowKey = "2025-01-13", Proteins = 10, Vegetables = 3, Dairy = 2 },
            new() { PartitionKey = "dev-user", RowKey = "2025-01-14", Proteins = 12, Vegetables = 4, Dairy = 3 }
        };

        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", weekStart, weekEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _weeklySummaryHandler.Handle(new GetWeeklySummaryQuery(date), CancellationToken.None);

        // Assert
        result.TotalProteins.Should().Be(22); // 10 + 12
        result.TotalVegetables.Should().Be(7); // 3 + 4
        result.TotalDairy.Should().Be(5); // 2 + 3
        result.DairyAsProteinEquivalent.Should().Be(10); // 5 * 2
    }

    [Fact]
    public async Task GetWeeklySummary_EmptyWeek_ReturnsZeroTotals()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        _repositoryMock
            .Setup(r => r.GetRangeAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyLogEntity>());

        // Act
        var result = await _weeklySummaryHandler.Handle(new GetWeeklySummaryQuery(date), CancellationToken.None);

        // Assert
        result.TotalProteins.Should().Be(0);
        result.TotalVegetables.Should().Be(0);
        result.TotalFruits.Should().Be(0);
        result.TotalStarches.Should().Be(0);
        result.TotalFats.Should().Be(0);
        result.TotalDairy.Should().Be(0);
        result.DairyAsProteinEquivalent.Should().Be(0);
    }

    #endregion
}
