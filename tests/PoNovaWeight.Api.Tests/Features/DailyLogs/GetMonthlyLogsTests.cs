using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Features.DailyLogs;

/// <summary>
/// Unit tests for GetMonthlyLogsHandler.
/// </summary>
public class GetMonthlyLogsTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly GetMonthlyLogsHandler _handler;

    public GetMonthlyLogsTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _handler = new GetMonthlyLogsHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsMonthlyLogs_ForSpecifiedMonth()
    {
        // Arrange
        var query = new GetMonthlyLogsQuery(2025, 12);
        var startDate = new DateOnly(2025, 12, 1);
        var endDate = new DateOnly(2025, 12, 31);

        var entities = new List<DailyLogEntity>
        {
            new()
            {
                PartitionKey = "dev-user",
                RowKey = "2025-12-01",
                OmadCompliant = true,
                AlcoholConsumed = false,
                Weight = 175.5
            },
            new()
            {
                PartitionKey = "dev-user",
                RowKey = "2025-12-05",
                OmadCompliant = false,
                AlcoholConsumed = true,
                Weight = 176.0
            }
        };

        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2025, result.Year);
        Assert.Equal(12, result.Month);
        Assert.Equal(2, result.Days.Count);

        var firstDay = result.Days.First(d => d.Date == new DateOnly(2025, 12, 1));
        Assert.True(firstDay.OmadCompliant);
        Assert.False(firstDay.AlcoholConsumed);
        Assert.Equal(175.5m, firstDay.Weight);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoEntriesExist()
    {
        // Arrange
        var query = new GetMonthlyLogsQuery(2025, 1);
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 1, 31);

        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyLogEntity>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2025, result.Year);
        Assert.Equal(1, result.Month);
        Assert.Empty(result.Days);
    }

    [Fact]
    public async Task Handle_HandlesNullOmadFields_Gracefully()
    {
        // Arrange
        var query = new GetMonthlyLogsQuery(2025, 6);
        var startDate = new DateOnly(2025, 6, 1);
        var endDate = new DateOnly(2025, 6, 30);

        var entities = new List<DailyLogEntity>
        {
            new()
            {
                PartitionKey = "dev-user",
                RowKey = "2025-06-15",
                // All OMAD fields are null
                OmadCompliant = null,
                AlcoholConsumed = null,
                Weight = null
            }
        };

        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Days);
        var day = result.Days[0];
        Assert.Null(day.OmadCompliant);
        Assert.Null(day.AlcoholConsumed);
        Assert.Null(day.Weight);
    }

    [Fact]
    public async Task Handle_CalculatesCorrectDateRange_ForFebruary()
    {
        // Arrange - 2024 is a leap year
        var query = new GetMonthlyLogsQuery(2024, 2);
        DateOnly capturedStart = default;
        DateOnly capturedEnd = default;

        _repositoryMock
            .Setup(r => r.GetRangeAsync("dev-user", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateOnly, DateOnly, CancellationToken>((_, start, end, _) =>
            {
                capturedStart = start;
                capturedEnd = end;
            })
            .ReturnsAsync(new List<DailyLogEntity>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(new DateOnly(2024, 2, 1), capturedStart);
        Assert.Equal(new DateOnly(2024, 2, 29), capturedEnd); // Leap year
    }
}
