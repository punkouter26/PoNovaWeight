using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using Serilog;
using System.Security.Claims;
using System.Text;

namespace PoNovaWeight.Api.Infrastructure;

/// <summary>
/// Extension methods for configuring authentication services.
/// </summary>
public static class AuthenticationExtensions
{
    // Dev test key - must match the one in Endpoints.cs for dev-login
    private const string DevTestKey = "dev-test-key-that-is-long-enough-for-hmac-sha256";

    /// <summary>
    /// Configures JWT Bearer authentication for validating Google ID tokens from the client.
    /// The Blazor WASM client handles the OAuth flow and sends the ID token to the API.
    /// In Development mode, also accepts dev test tokens for E2E testing.
    /// </summary>
    public static IServiceCollection AddNovaAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Get Google Client ID for token validation
        var googleClientId = configuration["PoNovaWeight:Google:ClientId"]
            ?? configuration["Google:ClientId"];

        // Treat placeholders as missing credentials
        if (IsPlaceholder(googleClientId))
        {
            googleClientId = null;
        }

        if (string.IsNullOrEmpty(googleClientId) && !environment.IsDevelopment())
        {
            throw new InvalidOperationException("Google Client ID is required in production. Set Google:ClientId in configuration.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // In development, we accept both Google tokens and dev test tokens
            if (environment.IsDevelopment())
            {
                ConfigureDevAuthentication(options, googleClientId);
            }
            else
            {
                ConfigureProductionAuthentication(options, googleClientId!);
            }
        });

        services.AddAuthorization();

        return services;
    }

    private static void ConfigureDevAuthentication(JwtBearerOptions options, string? googleClientId)
    {
        // In dev mode, we handle Google, Microsoft, and dev test tokens
        options.RequireHttpsMetadata = false;
        
        // Don't set Authority — we use multiple issuers and resolve keys dynamically
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            IssuerValidator = ValidateMultiProviderIssuer,
            ValidateAudience = false, // Dev tokens may have different audiences
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "name",
            RoleClaimType = "role",
            // Provide the symmetric key for dev tokens
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DevTestKey)),
            // Also try signature validation with the key
            TryAllIssuerSigningKeys = true
        };

        // Add multiple token validation via events - try Google and Microsoft OIDC configs
        ConfigureMultiProviderEvents(options, googleClientId);
    }

    private static void ConfigureProductionAuthentication(JwtBearerOptions options, string googleClientId)
    {
        // Don't set Authority — we use multiple issuers and resolve keys via events
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            IssuerValidator = ValidateMultiProviderIssuer,
            ValidateAudience = false, // Multiple audiences (Google + Microsoft client IDs)
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        ConfigureMultiProviderEvents(options, googleClientId);
    }

    /// <summary>
    /// Configures JWT events with multi-provider token resolution.
    /// Downloads signing keys from Google and Microsoft OIDC endpoints dynamically.
    /// </summary>
    private static void ConfigureMultiProviderEvents(JwtBearerOptions options, string? googleClientId)
    {
        // Cache OIDC signing key configs
        var googleConfigManager = new Microsoft.IdentityModel.Protocols.ConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>(
            "https://accounts.google.com/.well-known/openid-configuration",
            new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfigurationRetriever());
        var microsoftConfigManager = new Microsoft.IdentityModel.Protocols.ConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>(
            "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
            new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfigurationRetriever());

        options.TokenValidationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
        {
            var keys = new List<SecurityKey>();
            try
            {
                var googleConfig = googleConfigManager.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
                keys.AddRange(googleConfig.SigningKeys);
            }
            catch { /* Google keys unavailable */ }
            try
            {
                var msConfig = microsoftConfigManager.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
                keys.AddRange(msConfig.SigningKeys);
            }
            catch { /* Microsoft keys unavailable */ }
            // Add dev key if present in parameters
            if (parameters.IssuerSigningKey is not null)
            {
                keys.Add(parameters.IssuerSigningKey);
            }
            // Always include the dev symmetric key for dev-login tokens
            keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DevTestKey)));
            return keys;
        };

        ConfigureJwtEvents(options);
    }

    private static void ConfigureJwtEvents(JwtBearerOptions options)
    {
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");
                
                var email = context.Principal?.FindFirstValue(ClaimTypes.Email)
                    ?? context.Principal?.FindFirstValue("email")
                    ?? context.Principal?.FindFirstValue("preferred_username");
                var name = context.Principal?.FindFirstValue(ClaimTypes.Name)
                    ?? context.Principal?.FindFirstValue("name")
                    ?? context.Principal?.FindFirstValue("preferred_username");
                var picture = context.Principal?.FindFirstValue("picture");

                if (!string.IsNullOrEmpty(email))
                {
                    // Upsert user on each token validation
                    var userRepo = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                    var existingUser = await userRepo.GetAsync(email);

                    if (existingUser is not null)
                    {
                        existingUser.LastLoginUtc = DateTimeOffset.UtcNow;
                        existingUser.DisplayName = name ?? existingUser.DisplayName;
                        existingUser.PictureUrl = picture ?? existingUser.PictureUrl;
                        await userRepo.UpsertAsync(existingUser);
                        logger.LogDebug("Token validated for user: {Email}", email);
                    }
                    else
                    {
                        var newUser = UserEntity.Create(email, name ?? email, picture);
                        await userRepo.UpsertAsync(newUser);
                        logger.LogInformation("New user created from token: {Email}", email);
                    }
                }
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");
                
                logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                logger.LogWarning("Exception type: {Type}", context.Exception.GetType().FullName);
                if (context.Exception.InnerException is not null)
                {
                    logger.LogWarning("Inner exception: {Inner}", context.Exception.InnerException.Message);
                }

                // Log token details for debugging
                var authHeader = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
                if (authHeader is not null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader["Bearer ".Length..];
                    // Decode JWT header and payload (without verification) for debugging
                    try
                    {
                        var parts = token.Split('.');
                        if (parts.Length >= 2)
                        {
                            var header = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(parts[0])));
                            var payload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(parts[1])));
                            logger.LogWarning("Token header: {Header}", header);
                            logger.LogWarning("Token payload: {Payload}", payload);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Could not decode token: {Error}", ex.Message);
                    }
                }

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Return 401 with helpful message
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\":\"Unauthorized\",\"message\":\"Valid Google or Microsoft ID token required\"}");
            }
        };
    }

    /// <summary>
    /// Custom issuer validator that accepts Google, any Microsoft tenant, and dev issuers.
    /// Microsoft tokens use the user's actual tenant ID in the issuer claim, so we can't
    /// use a static list — we validate the pattern instead.
    /// </summary>
    private static string ValidateMultiProviderIssuer(
        string issuer,
        SecurityToken securityToken,
        TokenValidationParameters validationParameters)
    {
        // Google issuers
        if (issuer is "https://accounts.google.com" or "accounts.google.com")
            return issuer;

        // Microsoft issuers: https://login.microsoftonline.com/{tenant-id}/v2.0
        if (issuer.StartsWith("https://login.microsoftonline.com/", StringComparison.OrdinalIgnoreCase) &&
            issuer.EndsWith("/v2.0", StringComparison.OrdinalIgnoreCase))
            return issuer;

        // Dev issuer
        if (issuer == "dev-issuer")
            return issuer;

        throw new SecurityTokenInvalidIssuerException($"Issuer '{issuer}' is not valid.") { InvalidIssuer = issuer };
    }

    private static bool IsPlaceholder(string? value)
        => string.IsNullOrWhiteSpace(value)
            || value.StartsWith("YOUR-", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "placeholder", StringComparison.OrdinalIgnoreCase);

    private static string PadBase64(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return base64;
    }
}
