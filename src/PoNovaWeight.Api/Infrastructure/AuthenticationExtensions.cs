using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using Serilog;
using System.Security.Claims;

namespace PoNovaWeight.Api.Infrastructure;

/// <summary>
/// Extension methods for configuring authentication services.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures Google OAuth and Cookie authentication for the application.
    /// </summary>
    public static IServiceCollection AddNovaAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Google OAuth + Cookie Authentication
        // Reads from PoNovaWeight:Google:* (Key Vault) or Google:* (local/fallback)
        var googleClientId = configuration["PoNovaWeight:Google:ClientId"]
            ?? configuration["Google:ClientId"];
        var googleClientSecret = configuration["PoNovaWeight:Google:ClientSecret"]
            ?? configuration["Google:ClientSecret"];

        // Treat placeholders as missing credentials (common in IaC defaults)
        if (IsPlaceholder(googleClientId))
        {
            googleClientId = null;
        }

        if (IsPlaceholder(googleClientSecret))
        {
            googleClientSecret = null;
        }

        var hasGoogleCredentials = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);

        if (!hasGoogleCredentials && !environment.IsDevelopment())
        {
            throw new InvalidOperationException("Google OAuth credentials are required in production. Set Google:ClientId and Google:ClientSecret in configuration.");
        }

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = hasGoogleCredentials ? GoogleDefaults.AuthenticationScheme : CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = "nova-session";
            options.Cookie.HttpOnly = true;
            // In production, always use Secure cookies (all traffic is HTTPS via Azure front-end)
            // In development, use SameAsRequest to support both HTTP and HTTPS
            options.Cookie.SecurePolicy = environment.IsDevelopment() 
                ? CookieSecurePolicy.SameAsRequest 
                : CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax; // Lax required for OAuth redirects
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                // Return 401 for API calls instead of redirecting
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
        });

        // Add Google OAuth only when credentials are available
        if (hasGoogleCredentials)
        {
            authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = googleClientId!;
                options.ClientSecret = googleClientSecret!;
                options.CallbackPath = "/signin-google";
                options.SaveTokens = false;
                options.CorrelationCookie.SameSite = SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    // Ensure https redirects when behind Azure reverse proxies
                    var redirectUri = context.RedirectUri;
                    if (redirectUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    {
                        redirectUri = string.Concat("https://", redirectUri.AsSpan("http://".Length));
                    }

                    context.Response.Redirect(redirectUri);
                    return Task.CompletedTask;
                };
                options.Events.OnCreatingTicket = async context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
                    var name = context.Principal?.FindFirstValue(ClaimTypes.Name);
                    var picture = context.Principal?.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

                    if (!string.IsNullOrEmpty(email))
                    {
                        var userRepo = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                        var existingUser = await userRepo.GetAsync(email);

                        if (existingUser is not null)
                        {
                            // Update last login time
                            existingUser.LastLoginUtc = DateTimeOffset.UtcNow;
                            existingUser.DisplayName = name ?? existingUser.DisplayName;
                            existingUser.PictureUrl = picture ?? existingUser.PictureUrl;
                            await userRepo.UpsertAsync(existingUser);
                            logger.LogInformation("User signed in: {Email} (returning user)", email);
                        }
                        else
                        {
                            // Create new user
                            var newUser = UserEntity.Create(email, name ?? email, picture);
                            await userRepo.UpsertAsync(newUser);
                            logger.LogInformation("User signed in: {Email} (new user)", email);
                        }
                    }
                };
            });
        }
        else
        {
            Log.Warning("Google OAuth not configured - authentication will use dev login only");
        }

        services.AddAuthorization();

        return services;
    }

    private static bool IsPlaceholder(string? value)
        => string.IsNullOrWhiteSpace(value)
            || value.StartsWith("YOUR-", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "placeholder", StringComparison.OrdinalIgnoreCase);
}
