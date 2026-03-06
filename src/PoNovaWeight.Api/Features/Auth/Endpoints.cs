using System.Security.Claims;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.Auth;

/// <summary>
/// Extension methods for mapping authentication endpoints.
/// </summary>
public static class Endpoints
{
    /// <summary>
    /// Maps authentication endpoints for JWT-based auth.
    /// With client-side OIDC, the client handles login/logout flows directly with Google.
    /// The API only needs to validate tokens and provide user info.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, IWebHostEnvironment env)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .RequireRateLimiting("api");

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithDescription("Returns the current user's authentication status and profile from the JWT token.")
            .Produces<AuthStatus>(StatusCodes.Status200OK)
            .CacheOutput(x => x.NoCache());

        // Sync endpoint - ensures user exists in database after client-side login
        group.MapPost("/sync", SyncUser)
            .WithName("SyncUser")
            .WithDescription("Syncs the authenticated user to the database. Called after client-side login.")
            .RequireAuthorization()
            .Produces<AuthStatus>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    /// <summary>
    /// Gets the current user's authentication status from the JWT token.
    /// Does not require authentication - returns unauthenticated status if no valid token.
    /// </summary>
    private static IResult GetCurrentUser(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(AuthStatus.Unauthenticated());
        }

        var email = context.User.FindFirstValue(ClaimTypes.Email) 
            ?? context.User.FindFirstValue("email");
        var name = context.User.FindFirstValue(ClaimTypes.Name) 
            ?? context.User.FindFirstValue("name");
        var picture = context.User.FindFirstValue("picture");

        if (string.IsNullOrEmpty(email))
        {
            return Results.Ok(AuthStatus.Unauthenticated());
        }

        var userInfo = new UserInfo
        {
            Email = email,
            DisplayName = name ?? email,
            PictureUrl = picture
        };

        return Results.Ok(AuthStatus.Authenticated(userInfo));
    }

    /// <summary>
    /// Syncs the authenticated user to the database.
    /// This is called by the client after a successful login to ensure the user exists.
    /// </summary>
    private static async Task<IResult> SyncUser(
        HttpContext context,
        IUserRepository userRepository,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Auth");
        
        var email = context.User.FindFirstValue(ClaimTypes.Email) 
            ?? context.User.FindFirstValue("email");
        var name = context.User.FindFirstValue(ClaimTypes.Name) 
            ?? context.User.FindFirstValue("name");
        var picture = context.User.FindFirstValue("picture");

        if (string.IsNullOrEmpty(email))
        {
            return Results.Unauthorized();
        }

        var existingUser = await userRepository.GetAsync(email);
        
        if (existingUser is null)
        {
            var newUser = UserEntity.Create(email, name ?? email, picture);
            await userRepository.UpsertAsync(newUser);
            logger.LogInformation("User synced (new): {Email}", email);
        }
        else
        {
            existingUser.LastLoginUtc = DateTimeOffset.UtcNow;
            existingUser.DisplayName = name ?? existingUser.DisplayName;
            existingUser.PictureUrl = picture ?? existingUser.PictureUrl;
            await userRepository.UpsertAsync(existingUser);
            logger.LogDebug("User synced (existing): {Email}", email);
        }

        return Results.Ok(AuthStatus.Authenticated(new UserInfo
        {
            Email = email,
            DisplayName = name ?? email,
            PictureUrl = picture
        }));
    }
}
