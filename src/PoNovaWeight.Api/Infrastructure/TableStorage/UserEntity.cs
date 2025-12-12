using Azure;
using Azure.Data.Tables;

namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Azure Table Storage entity for user profiles.
/// </summary>
public class UserEntity : ITableEntity
{
    /// <summary>
    /// User's email address, normalized to lowercase.
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Fixed value: "profile" (single row per user).
    /// </summary>
    public string RowKey { get; set; } = "profile";

    /// <summary>
    /// Azure-managed timestamp for concurrency.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Azure-managed ETag for optimistic concurrency.
    /// </summary>
    public ETag ETag { get; set; }

    /// <summary>
    /// User's display name from Google.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Google profile picture URL (optional).
    /// </summary>
    public string? PictureUrl { get; set; }

    /// <summary>
    /// Timestamp of first sign-in.
    /// </summary>
    public DateTimeOffset FirstLoginUtc { get; set; }

    /// <summary>
    /// Timestamp of most recent sign-in.
    /// </summary>
    public DateTimeOffset LastLoginUtc { get; set; }

    /// <summary>
    /// Normalizes email to lowercase for consistent storage.
    /// </summary>
    public static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Creates a new user entity for first-time sign-in.
    /// </summary>
    public static UserEntity Create(string email, string displayName, string? pictureUrl = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new UserEntity
        {
            PartitionKey = NormalizeEmail(email),
            RowKey = "profile",
            DisplayName = displayName,
            PictureUrl = pictureUrl,
            FirstLoginUtc = now,
            LastLoginUtc = now
        };
    }
}
