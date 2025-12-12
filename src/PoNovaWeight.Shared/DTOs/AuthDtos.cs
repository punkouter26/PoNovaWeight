namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// Represents user information exposed to the client.
/// </summary>
public record UserInfo
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// User's display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Profile picture URL (optional).
    /// </summary>
    public string? PictureUrl { get; init; }
}

/// <summary>
/// Response DTO for authentication status check.
/// </summary>
public record AuthStatus
{
    /// <summary>
    /// Whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>
    /// User information if authenticated, null otherwise.
    /// </summary>
    public UserInfo? User { get; init; }

    /// <summary>
    /// Creates an authenticated status with user info.
    /// </summary>
    public static AuthStatus Authenticated(UserInfo user) => new()
    {
        IsAuthenticated = true,
        User = user
    };

    /// <summary>
    /// Creates an unauthenticated status.
    /// </summary>
    public static AuthStatus Unauthenticated() => new()
    {
        IsAuthenticated = false,
        User = null
    };
}
