using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PoNovaWeight.Api.Features.Auth;
using PoNovaWeight.Api.Infrastructure.TableStorage;

namespace PoNovaWeight.Api.Tests.Unit.Auth;

public class TestUserDataSeederTests
{
    private readonly Mock<IDailyLogRepository> _dailyLogRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<TestUserDataSeeder>> _loggerMock;
    private readonly FakeTimeProvider _timeProvider;

    public TestUserDataSeederTests()
    {
        _dailyLogRepositoryMock = new Mock<IDailyLogRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<TestUserDataSeeder>>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task EnsureSeededAsync_WhenNoExistingData_CreatesThreeYearsOfRealisticEntries()
    {
        // Arrange
        var captured = new List<DailyLogEntity>();
        var seeder = new TestUserDataSeeder(
            _dailyLogRepositoryMock.Object,
            _userRepositoryMock.Object,
            _timeProvider,
            _loggerMock.Object);

        _dailyLogRepositoryMock
            .Setup(r => r.GetRangeAsync(
                TestUserDataSeeder.TestUserEmail,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyLogEntity>());

        _dailyLogRepositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DailyLogEntity, CancellationToken>((entity, _) => captured.Add(entity))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(r => r.GetAsync(TestUserDataSeeder.TestUserEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        _userRepositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await seeder.EnsureSeededAsync(CancellationToken.None);

        // Assert
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var startDate = today.AddYears(-3).AddDays(1);
        var totalDays = today.DayNumber - startDate.DayNumber + 1;

        var expectedFull = (int)Math.Round(totalDays * 0.95, MidpointRounding.AwayFromZero);
        var expectedPartial = (int)Math.Round(totalDays * 0.03, MidpointRounding.AwayFromZero);
        var expectedMissing = totalDays - expectedFull - expectedPartial;

        Assert.True(result.Seeded);
        Assert.Equal(totalDays, result.TotalDays);
        Assert.Equal(expectedFull, result.FullDays);
        Assert.Equal(expectedPartial, result.PartialDays);
        Assert.Equal(expectedMissing, result.MissingDays);
        Assert.Equal(expectedFull + expectedPartial, result.CreatedEntries);

        Assert.Equal(result.CreatedEntries, captured.Count);
        Assert.All(captured, e => Assert.Equal(TestUserDataSeeder.TestUserEmail, e.PartitionKey));
        Assert.All(captured, e => Assert.True(DateOnly.ParseExact(e.RowKey, "yyyy-MM-dd") >= startDate));
        Assert.All(captured, e => Assert.True(DateOnly.ParseExact(e.RowKey, "yyyy-MM-dd") <= today));
        Assert.Equal(captured.Count, captured.Select(e => e.RowKey).Distinct().Count());

        Assert.Contains(captured, e => e.Weight.HasValue);
        Assert.Contains(captured, e => !e.Weight.HasValue);
        Assert.Contains(captured, e => e.SystolicBP.HasValue && e.DiastolicBP.HasValue);
        Assert.Contains(captured, e => !e.SystolicBP.HasValue && !e.DiastolicBP.HasValue);

        _userRepositoryMock.Verify(
            r => r.UpsertAsync(It.Is<UserEntity>(u => u.PartitionKey == TestUserDataSeeder.TestUserEmail), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureSeededAsync_WhenDataAlreadyExists_SkipsGeneration()
    {
        // Arrange
        var seeder = new TestUserDataSeeder(
            _dailyLogRepositoryMock.Object,
            _userRepositoryMock.Object,
            _timeProvider,
            _loggerMock.Object);

        _dailyLogRepositoryMock
            .Setup(r => r.GetRangeAsync(
                TestUserDataSeeder.TestUserEmail,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new DailyLogEntity
                {
                    PartitionKey = TestUserDataSeeder.TestUserEmail,
                    RowKey = "2026-03-04",
                    Weight = 210
                }
            ]);

        // Act
        var result = await seeder.EnsureSeededAsync(CancellationToken.None);

        // Assert
        Assert.False(result.Seeded);
        Assert.Equal(0, result.CreatedEntries);

        _dailyLogRepositoryMock.Verify(
            r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _userRepositoryMock.Verify(
            r => r.UpsertAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
