using Moq;
using PoNovaWeight.Api.Features.Settings;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Features.Settings;

/// <summary>
/// Unit tests for UpsertUserSettingsHandler.
/// Verifies user settings creation and updates.
/// </summary>
public class UpsertUserSettingsHandlerTests
{
    private readonly Mock<IUserSettingsRepository> _repositoryMock;
    private readonly UpsertUserSettingsHandler _handler;

    public UpsertUserSettingsHandlerTests()
    {
        _repositoryMock = new Mock<IUserSettingsRepository>();
        _handler = new UpsertUserSettingsHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_NewSettings_CreatesEntityAndReturnsSettings()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            TargetSystolic = 125,
            TargetDiastolic = 82,
            TargetHeartRate = 68
        };
        var command = new UpsertUserSettingsCommand(settings, "new-user");
        
        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(125, result.TargetSystolic);
        Assert.Equal(82, result.TargetDiastolic);
        Assert.Equal(68, result.TargetHeartRate);
        
        _repositoryMock.Verify(
            r => r.UpsertAsync(
                It.Is<UserSettingsEntity>(e => 
                    e.RowKey == "new-user" &&
                    e.PartitionKey == "settings" &&
                    e.TargetSystolic == 125 &&
                    e.TargetDiastolic == 82 &&
                    e.TargetHeartRate == 68),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateExistingSettings_UpsertsCalled()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            TargetSystolic = 115,
            TargetDiastolic = 75,
            TargetHeartRate = 65
        };
        var command = new UpsertUserSettingsCommand(settings, "existing-user");
        
        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(settings.TargetSystolic, result.TargetSystolic);
        Assert.Equal(settings.TargetDiastolic, result.TargetDiastolic);
        Assert.Equal(settings.TargetHeartRate, result.TargetHeartRate);
        
        _repositoryMock.Verify(
            r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DefaultUserId_UsesDevUser()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            TargetSystolic = 120,
            TargetDiastolic = 80,
            TargetHeartRate = 70
        };
        var command = new UpsertUserSettingsCommand(settings); // Uses default "dev-user"
        
        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _repositoryMock.Verify(
            r => r.UpsertAsync(
                It.Is<UserSettingsEntity>(e => e.RowKey == "dev-user"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PartialSettings_StoresAllFields()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            TargetSystolic = 130,
            TargetDiastolic = null, // Partial settings
            TargetHeartRate = 72
        };
        var command = new UpsertUserSettingsCommand(settings, "partial-user");
        
        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(130, result.TargetSystolic);
        Assert.Null(result.TargetDiastolic);
        Assert.Equal(72, result.TargetHeartRate);
        
        _repositoryMock.Verify(
            r => r.UpsertAsync(
                It.Is<UserSettingsEntity>(e => 
                    e.TargetSystolic == 130 &&
                    e.TargetDiastolic == null &&
                    e.TargetHeartRate == 72),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CancellationToken_PassedToRepository()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            TargetSystolic = 120,
            TargetDiastolic = 80,
            TargetHeartRate = 70
        };
        var command = new UpsertUserSettingsCommand(settings, "token-test-user");
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.NotNull(result);
        _repositoryMock.Verify(
            r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NullValues_CreatesEntityWithNulls()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            TargetSystolic = null,
            TargetDiastolic = null,
            TargetHeartRate = null
        };
        var command = new UpsertUserSettingsCommand(settings, "null-user");
        
        _repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.TargetSystolic);
        Assert.Null(result.TargetDiastolic);
        Assert.Null(result.TargetHeartRate);
        
        _repositoryMock.Verify(
            r => r.UpsertAsync(
                It.Is<UserSettingsEntity>(e => 
                    e.TargetSystolic == null &&
                    e.TargetDiastolic == null &&
                    e.TargetHeartRate == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
