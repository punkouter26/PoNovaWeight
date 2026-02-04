using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using Xunit;

namespace PoNovaWeight.Api.Tests.Features.DailyLogs;

public class GetAlcoholCorrelationTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly GetAlcoholCorrelationHandler _handler;
    private readonly DateOnly _fixedToday = new(2026, 2, 4);

    public GetAlcoholCorrelationTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(_fixedToday.ToDateTime(TimeOnly.MinValue)));
        _handler = new GetAlcoholCorrelationHandler(_repositoryMock.Object, timeProvider);
    }

    [Fact]
    public async Task Handle_ReturnsInsufficientData_WhenNoLogs()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyLogEntity>());

        // Act
        var result = await _handler.Handle(new GetAlcoholCorrelationQuery(), CancellationToken.None);

        // Assert
        result.HasSufficientData.Should().BeFalse();
        result.DaysWithAlcohol.Should().Be(0);
        result.DaysWithoutAlcohol.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsInsufficientData_WhenNoAlcoholDays()
    {
        // Arrange
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), weight: 180, alcohol: false),
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), weight: 179, alcohol: false),
            CreateEntity(DateOnly.FromDateTime(DateTime.Today), weight: 178, alcohol: false)
        };

        _repositoryMock.Setup(r => r.GetRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(new GetAlcoholCorrelationQuery(), CancellationToken.None);

        // Assert
        result.HasSufficientData.Should().BeFalse();
        result.DaysWithAlcohol.Should().Be(0);
        result.DaysWithoutAlcohol.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ReturnsInsufficientData_WhenNoNonAlcoholDays()
    {
        // Arrange
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), weight: 182, alcohol: true),
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), weight: 183, alcohol: true),
            CreateEntity(DateOnly.FromDateTime(DateTime.Today), weight: 184, alcohol: true)
        };

        _repositoryMock.Setup(r => r.GetRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(new GetAlcoholCorrelationQuery(), CancellationToken.None);

        // Assert
        result.HasSufficientData.Should().BeFalse();
        result.DaysWithAlcohol.Should().Be(3);
        result.DaysWithoutAlcohol.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CalculatesAverages_WhenBothTypesExist()
    {
        // Arrange
        var entities = new List<DailyLogEntity>
        {
            // Alcohol days: 182, 184 -> avg 183
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-4)), weight: 182, alcohol: true),
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), weight: 184, alcohol: true),
            // Non-alcohol days: 178, 180 -> avg 179
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-3)), weight: 178, alcohol: false),
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), weight: 180, alcohol: false)
        };

        _repositoryMock.Setup(r => r.GetRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(new GetAlcoholCorrelationQuery(), CancellationToken.None);

        // Assert
        result.HasSufficientData.Should().BeTrue();
        result.DaysWithAlcohol.Should().Be(2);
        result.DaysWithoutAlcohol.Should().Be(2);
        result.AverageWeightWithAlcohol.Should().Be(183m);
        result.AverageWeightWithoutAlcohol.Should().Be(179m);
        result.WeightDifference.Should().Be(4m); // 183 - 179
    }

    [Fact]
    public async Task Handle_IgnoresDays_WithNullWeight()
    {
        // Arrange
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-3)), weight: 182, alcohol: true),
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), weight: null, alcohol: true), // No weight
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), weight: 178, alcohol: false),
            CreateEntity(DateOnly.FromDateTime(DateTime.Today), weight: null, alcohol: false) // No weight
        };

        _repositoryMock.Setup(r => r.GetRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(new GetAlcoholCorrelationQuery(), CancellationToken.None);

        // Assert
        result.HasSufficientData.Should().BeTrue();
        result.DaysWithAlcohol.Should().Be(1);
        result.DaysWithoutAlcohol.Should().Be(1);
        result.AverageWeightWithAlcohol.Should().Be(182m);
        result.AverageWeightWithoutAlcohol.Should().Be(178m);
    }

    [Fact]
    public async Task Handle_IgnoresDays_WithNullAlcohol()
    {
        // Arrange
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), weight: 182, alcohol: true),
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), weight: 180, alcohol: null), // Null alcohol
            CreateEntity(DateOnly.FromDateTime(DateTime.Today), weight: 178, alcohol: false)
        };

        _repositoryMock.Setup(r => r.GetRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(new GetAlcoholCorrelationQuery(), CancellationToken.None);

        // Assert - day with null alcohol should be excluded
        result.DaysWithAlcohol.Should().Be(1);
        result.DaysWithoutAlcohol.Should().Be(1);
    }

    [Fact]
    public async Task Handle_CalculatesNegativeDifference_WhenAlcoholDaysLighter()
    {
        // Arrange
        var entities = new List<DailyLogEntity>
        {
            CreateEntity(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), weight: 175, alcohol: true),
            CreateEntity(DateOnly.FromDateTime(DateTime.Today), weight: 180, alcohol: false)
        };

        _repositoryMock.Setup(r => r.GetRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _handler.Handle(new GetAlcoholCorrelationQuery(), CancellationToken.None);

        // Assert
        result.WeightDifference.Should().Be(-5m); // 175 - 180
    }

    private static DailyLogEntity CreateEntity(DateOnly date, double? weight, bool? alcohol)
    {
        return new DailyLogEntity
        {
            PartitionKey = "dev-user",
            RowKey = date.ToString("yyyy-MM-dd"),
            Weight = weight,
            AlcoholConsumed = alcohol
        };
    }
}
