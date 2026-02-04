using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.Auth;

/// <summary>
/// Extension methods for mapping authentication endpoints.
/// </summary>
public static class Endpoints
{
    /// <summary>
    /// Maps authentication endpoints including dev-login for Development environment.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapGet("/login", Login)
            .WithName("Login")
            .WithDescription("Initiates Google OAuth sign-in flow.")
            .ExcludeFromDescription(); // Challenge redirect, not a typical API response

        group.MapGet("/logout", (Delegate)Logout)
            .WithName("Logout")
            .WithDescription("Signs out the current user and clears the session.")
            .ExcludeFromDescription(); // Redirect response

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithDescription("Returns the current user's authentication status and profile.")
            .Produces<AuthStatus>(StatusCodes.Status200OK);

        // Development-only endpoint for bypassing OAuth
        // This enables testing without real Google credentials
        var env = app.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
        {
            group.MapPost("/dev-login", DevLogin)
                .WithName("DevLogin")
                .WithDescription("[DEV ONLY] Bypasses OAuth to sign in with a test user.")
                .Produces<AuthStatus>(StatusCodes.Status200OK);
        }

        return app;
    }

    /// <summary>
    /// Development-only login endpoint that bypasses OAuth.
    /// Creates a cookie-based session for the specified email without Google OAuth.
    /// </summary>
    private static async Task<IResult> DevLogin(
        HttpContext context,
        IUserRepository userRepository,
        ILoggerFactory loggerFactory,
        string? email = "dev-user@local")
    {
        var logger = loggerFactory.CreateLogger("Auth");
        
        // Ensure user exists in repository
        var userEmail = email ?? "dev-user@local";
        var existingUser = await userRepository.GetAsync(userEmail);
        
        if (existingUser is null)
        {
            var newUser = UserEntity.Create(userEmail, "Dev User", null);
            await userRepository.UpsertAsync(newUser);
            logger.LogInformation("[DEV] Created test user: {Email}", userEmail);
        }
        else
        {
            existingUser.LastLoginUtc = DateTimeOffset.UtcNow;
            await userRepository.UpsertAsync(existingUser);
        }

        // Create claims and sign in
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, userEmail),
            new(ClaimTypes.Name, "Dev User")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        
        logger.LogInformation("[DEV] User signed in via dev-login: {Email}", userEmail);

        return Results.Ok(AuthStatus.Authenticated(new UserInfo
        {
            Email = userEmail,
            DisplayName = "Dev User",
            PictureUrl = null
        }));
    }

    private static async Task<IResult> Login(HttpContext context, IAuthenticationSchemeProvider schemeProvider, string? returnUrl = "/")
    {
        // Check if Google OAuth is configured
        var googleScheme = await schemeProvider.GetSchemeAsync(GoogleDefaults.AuthenticationScheme);
        if (googleScheme is null)
        {
            // Google OAuth not configured - in development, redirect to dev-login info
            var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
            {
                return Results.Problem(
                    title: "Google OAuth Not Configured",
                    detail: "Use POST /api/auth/dev-login?email=your@email.com to sign in during development.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            return Results.Problem(
                title: "Authentication Unavailable",
                detail: "Google OAuth is not configured.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl ?? "/"
        };

        return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> Logout(HttpContext context, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Auth");
        var email = context.User.FindFirstValue(ClaimTypes.Email);
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!string.IsNullOrEmpty(email))
        {
            logger.LogInformation("User signed out: {Email}", email);
        }

        return Results.Redirect("/login");
    }

    private static IResult GetCurrentUser(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(AuthStatus.Unauthenticated());
        }

        var email = context.User.FindFirstValue(ClaimTypes.Email);
        var name = context.User.FindFirstValue(ClaimTypes.Name);
        var picture = context.User.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

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
}
