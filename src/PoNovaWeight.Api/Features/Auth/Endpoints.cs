using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.Auth;

/// <summary>
/// Extension methods for mapping authentication endpoints.
/// </summary>
public static class Endpoints
{
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

        return app;
    }

    private static IResult Login(HttpContext context, string? returnUrl = "/")
    {
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
