using FluentAssertions;
using MediatR;
using Moq;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using Xunit;

namespace PoNovaWeight.Api.Tests.Features.DailyLogs;

public class DeleteDailyLogTests
{
    private readonly Mock<IDailyLogRepository> _repositoryMock;
    private readonly DeleteDailyLogHandler _handler;

    public DeleteDailyLogTests()
    {
        _repositoryMock = new Mock<IDailyLogRepository>();
        _handler = new DeleteDailyLogHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsTrue_WhenDeleteSucceeds()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        _repositoryMock.Setup(r => r.DeleteAsync(
                "dev-user",
                date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(new DeleteDailyLogCommand(date), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenEntityNotFound()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        _repositoryMock.Setup(r => r.DeleteAsync(
                "dev-user",
                date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(new DeleteDailyLogCommand(date), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CallsRepository_WithCorrectParameters()
    {
        // Arrange
        var date = new DateOnly(2024, 6, 15);
        _repositoryMock.Setup(r => r.DeleteAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(new DeleteDailyLogCommand(date, "test-user"), CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(
            "test-user",
            date,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UsesDefaultUserId_WhenNotProvided()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        _repositoryMock.Setup(r => r.DeleteAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(new DeleteDailyLogCommand(date), CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(
            "dev-user",
            date,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
