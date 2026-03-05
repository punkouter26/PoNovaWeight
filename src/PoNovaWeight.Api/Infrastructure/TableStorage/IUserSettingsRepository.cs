namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Repository for user settings operations.
/// </summary>
public interface IUserSettingsRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<UserSettingsEntity?> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task UpsertAsync(UserSettingsEntity entity, CancellationToken cancellationToken = default);
}
