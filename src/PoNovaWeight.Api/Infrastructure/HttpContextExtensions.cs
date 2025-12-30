using System.Security.Claims;

namespace PoNovaWeight.Api.Infrastructure;

/// <summary>
/// Extension methods for HttpContext to reduce boilerplate in endpoints.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the authenticated user's email from claims.
    /// Throws UnauthorizedAccessException if not authenticated.
    /// </summary>
    public static string GetUserId(this HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.Email)
            ?? throw new UnauthorizedAccessException("User is not authenticated");
}
