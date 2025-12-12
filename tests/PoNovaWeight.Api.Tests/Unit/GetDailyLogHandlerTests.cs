using FluentAssertions;
using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Unit;

public class GetDailyLogHandlerTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly GetDailyLogHandler _handler;

    public GetDailyLogHandlerTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _handler = new GetDailyLogHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetDailyLog_ReturnsCorrectData_OrNullForNonExistent()
    {
        // Test 1: Valid date returns daily log
        var date = new DateOnly(2025, 1, 15);
        var entity = new DailyLogEntity
        {
            PartitionKey = "dev-user",
            RowKey = "2025-01-15",
            Proteins = 10,
            Vegetables = 3,
            Fruits = 1,
            Starches = 1,
            Fats = 2,
            Dairy = 1,
            WaterSegments = 5
        };

        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await _handler.Handle(new GetDailyLogQuery(date), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Date.Should().Be(date);
        result.Proteins.Should().Be(10);
        result.Vegetables.Should().Be(3);
        result.WaterSegments.Should().Be(5);

        // Test 2: Non-existent date returns null
        var emptyDate = new DateOnly(2025, 2, 1);
        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", emptyDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyLogEntity?)null);

        var nullResult = await _handler.Handle(new GetDailyLogQuery(emptyDate), CancellationToken.None);
        nullResult.Should().BeNull();
    }

    [Fact]
    public async Task GetDailyLog_CustomUserId_UsesCorrectUserId()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 15);
        var customUserId = "custom-user";

        _repositoryMock
            .Setup(r => r.GetAsync(customUserId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyLogEntity?)null);

        var query = new GetDailyLogQuery(date, customUserId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.GetAsync(customUserId, date, It.IsAny<CancellationToken>()), Times.Once);
    }
}
