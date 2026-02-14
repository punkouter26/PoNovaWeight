# ADR 004: Implement DevAuth for Local Development

## Status
**Accepted** | Date: February 2026

## Context
We needed a way for developers to test the application locally without setting up Google OAuth credentials. The options considered were:
- Fake/Dev authentication provider
- Skip authentication in development
- Always require real OAuth

## Decision
We chose **DevAuth** (fake authentication) for the following reasons:

### 1. Developer Experience
- No need to create Google Cloud project for local dev
- Instant setup, clone and run
- Easy to test different user scenarios

### 2. Security
- Only enabled in Development environment
- Explicitly disabled in production
- Clear visual indicator (dev mode banner)

### 3. Consistency
- Same code paths as production auth
- Tests can use real auth flow
- No special test accounts needed

## Implementation

### DevAuthStateProvider
- Custom AuthenticationStateProvider for Blazor
- Provides fake user identity when enabled
- Switches to real auth in production

### DevTokenHandler
- HTTP handler that adds fake auth headers
- Used by integration tests
- Mimics real token validation

### Configuration
```json
{
  "DevAuth": {
    "Enabled": true,
    "UserId": "dev@example.com",
    "DisplayName": "Dev User"
  }
}
```

## Consequences

### Positive
- Faster onboarding for new developers
- No OAuth setup required for local dev
- Easy to test edge cases

### Negative
- Risk of accidentally enabling in production
- Must verify DevAuth is disabled in production checks
- Test coverage must include real auth flow

## Security Measures
- DevAuth disabled by default in appsettings.json
- Environment check in Program.cs
- Production deployment verification checklist
- Warning banner in UI when dev mode active
