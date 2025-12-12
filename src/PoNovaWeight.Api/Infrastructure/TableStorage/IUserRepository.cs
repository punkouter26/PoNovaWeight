namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Repository interface for user profile operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Ensures the Users table exists. Should be called at startup.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    /// <param name="email">User's email address (will be normalized).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User entity if found, null otherwise.</returns>
    Task<UserEntity?> GetAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a user entity.
    /// </summary>
    /// <param name="entity">User entity to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpsertAsync(UserEntity entity, CancellationToken cancellationToken = default);
}
