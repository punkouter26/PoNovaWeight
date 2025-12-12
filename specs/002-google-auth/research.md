# Research: Google OAuth Authentication

**Feature**: 002-google-auth  
**Date**: 2025-12-11  
**Purpose**: Resolve all technical unknowns before Phase 1 design

## Research Tasks

### 1. ASP.NET Core Google Authentication Provider

**Question**: What is the simplest way to implement Google OAuth in ASP.NET Core 10?

**Finding**: ASP.NET Core provides a built-in `Microsoft.AspNetCore.Authentication.Google` package that handles the entire OAuth 2.0 / OpenID Connect flow with minimal configuration.

**Decision**: Use `Microsoft.AspNetCore.Authentication.Google` NuGet package

**Rationale**:
- Official Microsoft package, well-maintained and secure
- Handles OAuth flow, token validation, and claims extraction automatically
- Integrates directly with ASP.NET Core's authentication middleware
- Requires only Client ID and Client Secret from Google Cloud Console

**Alternatives Considered**:
- Manual OAuth implementation: Rejected — requires handling tokens, state, CSRF manually
- IdentityServer/Duende: Rejected — overkill for single-provider auth
- Azure AD B2C: Rejected — adds Azure dependency and complexity for simple Google auth

**Configuration Pattern**:
```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "nova-session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.LoginPath = "/login";
})
.AddGoogle(options =>
{
    options.ClientId = configuration["Google:ClientId"]!;
    options.ClientSecret = configuration["Google:ClientSecret"]!;
});
```

---

### 2. Google Cloud Console OAuth Setup

**Question**: What steps are required to create Google OAuth credentials?

**Finding**: Google Cloud Console requires creating an OAuth 2.0 Client ID with authorized redirect URIs.

**Decision**: Document CLI-based setup in quickstart.md

**Rationale**: User requested "use CLI as much as possible"

**CLI Commands** (gcloud):
```bash
# Create project (if needed)
gcloud projects create ponovaweight-oauth --name="PoNovaWeight OAuth"

# Enable OAuth API
gcloud services enable oauth2.googleapis.com --project=ponovaweight-oauth

# Create OAuth consent screen (requires manual console step for branding)
# Note: OAuth consent screen MUST be configured in Console UI first

# Create OAuth client
gcloud alpha iap oauth-clients create projects/ponovaweight-oauth/brands/ponovaweight-oauth \
  --display_name="PoNovaWeight Web App"
```

**Note**: OAuth consent screen branding and authorized domains require Console UI. CLI can create the client after consent screen is configured.

**Authorized Redirect URIs**:
- Development: `https://localhost:5001/signin-google`
- Production: `https://ponovaweight-app.azurewebsites.net/signin-google`

---

### 3. User Profile Persistence to Azure Table Storage

**Question**: How should user profiles be stored in Azure Table Storage?

**Finding**: Following the existing `DailyLogEntity` pattern, create a `UserEntity` with email as PartitionKey (per clarification decision).

**Decision**: Create `Users` table with email as PartitionKey, fixed RowKey of "profile"

**Rationale**:
- Email is the primary identifier (per clarification)
- Single row per user simplifies queries
- Matches existing repository pattern in codebase
- PartitionKey on email enables efficient single-user lookups

**Table Schema**:
| Column | Type | Description |
|--------|------|-------------|
| PartitionKey | string | User's email address (normalized to lowercase) |
| RowKey | string | Fixed value: "profile" |
| DisplayName | string | Google display name |
| PictureUrl | string? | Google profile picture URL |
| FirstLoginUtc | DateTimeOffset | When user first signed in |
| LastLoginUtc | DateTimeOffset | Most recent sign-in |
| Timestamp | DateTimeOffset | Azure-managed |
| ETag | ETag | Azure-managed for concurrency |

---

### 4. Session Management Strategy

**Question**: Should we use ASP.NET Core's cookie authentication or keep custom session tokens?

**Finding**: ASP.NET Core's cookie authentication provides built-in session management with encryption, sliding expiration, and automatic claims handling.

**Decision**: Use ASP.NET Core cookie authentication (replace custom `nova-session` logic)

**Rationale**:
- Built-in encryption and signing of cookie contents
- Automatic sliding expiration support
- Claims are automatically populated from Google token
- Reduces custom code and security surface area

**Alternatives Considered**:
- Custom JWT tokens: Rejected — adds complexity, not needed for cookie-based SPA
- Keep custom session table: Rejected — duplicates cookie functionality

---

### 5. Blazor WASM Authentication Integration

**Question**: How does Blazor WASM handle authentication with a server-side OAuth flow?

**Finding**: Since the API hosts the Blazor WASM app, the cookie is shared automatically. Blazor can check auth status via an API endpoint.

**Decision**: Use cookie-based auth with `/api/auth/me` endpoint for current user info

**Rationale**:
- Cookies automatically sent with all requests to same origin
- No need for token storage in browser (more secure)
- Simple `/api/auth/me` endpoint returns current user or 401
- `AuthenticationStateProvider` in Blazor polls this endpoint

**Flow**:
1. User clicks "Sign in with Google" → navigates to `/api/auth/login`
2. API redirects to Google OAuth
3. Google redirects back to `/signin-google` (handled by middleware)
4. Middleware sets cookie and redirects to `/`
5. Blazor loads and calls `/api/auth/me` to get user info
6. `AuthenticationStateProvider` updates UI based on response

---

### 6. Removing Passcode Authentication

**Question**: What code needs to be removed/modified?

**Finding**: The following files contain passcode-specific code:

**Files to Remove**:
- `VerifyPasscode.cs` — Entire handler and command

**Files to Modify**:
- `Endpoints.cs` — Remove `/verify` endpoint, add Google OAuth endpoints
- `AuthDtos.cs` — Remove `PasscodeRequest`, add `UserInfo`
- `AuthMiddleware.cs` — Simplify to use ASP.NET Core's `User.Identity.IsAuthenticated`
- `appsettings.json` — Remove `Auth:Passcode`, add `Google:ClientId/ClientSecret`

**Decision**: Clean removal with Google OAuth replacement in same PR

---

### 7. Security Considerations

**Question**: What security measures are needed beyond default settings?

**Finding**: ASP.NET Core's Google authentication is secure by default. Additional measures:

**Decisions**:
1. **HTTPS Only**: Cookie with `SecurePolicy.Always`
2. **SameSite Strict**: Prevents CSRF in modern browsers
3. **HttpOnly**: Cookie not accessible via JavaScript
4. **Normalize Email**: Lowercase before storage (Google emails are case-insensitive)
5. **No sensitive data in client**: Only email and display name exposed to Blazor

---

## Summary

All research tasks complete. No NEEDS CLARIFICATION items remain.

| Research Area | Decision |
|---------------|----------|
| Auth Provider | `Microsoft.AspNetCore.Authentication.Google` |
| Session Management | ASP.NET Core cookie authentication |
| User Storage | Azure Table Storage, email as PartitionKey |
| Blazor Integration | Cookie-based with `/api/auth/me` endpoint |
| Passcode Auth | Full removal |
| Security | HTTPS, HttpOnly, Secure, SameSite=Strict |
