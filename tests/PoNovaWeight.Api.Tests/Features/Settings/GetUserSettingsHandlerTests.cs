using Moq;
using PoNovaWeight.Api.Features.Settings;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Features.Settings;

/// <summary>
/// Unit tests for GetUserSettingsHandler.
/// Verifies retrieval of user settings with default fallback.
/// </summary>
public class GetUserSettingsHandlerTests
{
    private readonly Mock<IUserSettingsRepository> _repositoryMock;
    private readonly GetUserSettingsHandler _handler;

    public GetUserSettingsHandlerTests()
    {
        _repositoryMock = new Mock<IUserSettingsRepository>();
        _handler = new GetUserSettingsHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_NoExistingSettings_ReturnsDefaults()
    {
        // Arrange
        var query = new GetUserSettingsQuery("new-user");
        
        _repositoryMock
            .Setup(r => r.GetAsync("new-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSettingsEntity?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(120, result.TargetSystolic);
        Assert.Equal(80, result.TargetDiastolic);
        Assert.Equal(70, result.TargetHeartRate);
        
        _repositoryMock.Verify(
            r => r.GetAsync("new-user", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingSettings_ReturnsCustomValues()
    {
        // Arrange
        var query = new GetUserSettingsQuery("test-user");
        var existingEntity = UserSettingsEntity.Create("test-user");
        existingEntity.TargetSystolic = 115;
        existingEntity.TargetDiastolic = 75;
        existingEntity.TargetHeartRate = 65;
        
        _repositoryMock
            .Setup(r => r.GetAsync("test-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(115, result.TargetSystolic);
        Assert.Equal(75, result.TargetDiastolic);
        Assert.Equal(65, result.TargetHeartRate);
        
        _repositoryMock.Verify(
            r => r.GetAsync("test-user", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DefaultUserId_UsesDevUser()
    {
        // Arrange
        var query = new GetUserSettingsQuery(); // Uses default "dev-user"
        
        _repositoryMock
            .Setup(r => r.GetAsync("dev-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSettingsEntity?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(UserSettingsDto.Default.TargetSystolic, result.TargetSystolic);
        
        _repositoryMock.Verify(
            r => r.GetAsync("dev-user", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PartialSettings_ReturnsAllFieldsFromEntity()
    {
        // Arrange
        var query = new GetUserSettingsQuery("partial-user");
        var entity = UserSettingsEntity.Create("partial-user");
        entity.TargetSystolic = 130;
        entity.TargetDiastolic = null; // Partial settings
        entity.TargetHeartRate = 72;
        
        _repositoryMock
            .Setup(r => r.GetAsync("partial-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(130, result.TargetSystolic);
        Assert.Null(result.TargetDiastolic);
        Assert.Equal(72, result.TargetHeartRate);
    }

    [Fact]
    public async Task Handle_CancellationToken_PassedToRepository()
    {
        // Arrange
        var query = new GetUserSettingsQuery("token-test-user");
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        _repositoryMock
            .Setup(r => r.GetAsync("token-test-user", cancellationToken))
            .ReturnsAsync((UserSettingsEntity?)null);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        Assert.NotNull(result);
        _repositoryMock.Verify(
            r => r.GetAsync("token-test-user", cancellationToken),
            Times.Once);
    }
}
