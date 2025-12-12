# Implementation Plan: Google OAuth Authentication

**Branch**: `002-google-auth` | **Date**: 2025-12-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-google-auth/spec.md`

## Summary

Replace the existing passcode-based authentication with Google OAuth 2.0 using ASP.NET Core's built-in authentication providers. User profile data (email, display name) will be persisted to Azure Table Storage on first sign-in. This approach is the simplest OAuth implementation for .NET, requiring minimal custom code while leveraging battle-tested Microsoft authentication middleware.

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: `Microsoft.AspNetCore.Authentication.Google`, `Azure.Data.Tables`  
**Storage**: Azure Table Storage (Azurite for local dev) — Users table with email as PartitionKey  
**Testing**: xUnit, bUnit, Moq, FluentAssertions, Playwright  
**Target Platform**: ASP.NET Core Web API hosting Blazor WASM  
**Project Type**: Web application (API + Client + Shared)  
**Performance Goals**: Auth round-trip < 5 seconds (mostly Google latency)  
**Constraints**: Cookie-based sessions, HttpOnly/Secure/SameSite=Strict  
**Scale/Scope**: Open registration, ~100s of users expected initially

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Check ✅ PASSED

| Principle | Compliance | Notes |
|-----------|------------|-------|
| **I. Foundation** | | |
| .NET 10, global.json locked | ✅ Pass | Already at 10.0.100 |
| Central package management | ✅ Pass | Directory.Packages.props exists |
| Nullable enabled | ✅ Pass | All projects have `<Nullable>enable</Nullable>` |
| **II. Architecture** | | |
| Vertical Slice in Features/ | ✅ Pass | Auth feature in `/src/PoNovaWeight.Api/Features/Auth/` |
| Minimal APIs | ✅ Pass | Existing auth uses Minimal API endpoints |
| Shared project DTOs only | ✅ Pass | AuthDtos.cs contains only DTOs |
| **III. Implementation** | | |
| OpenAPI/Swagger enabled | ✅ Pass | Already configured |
| Health check at api/health | ✅ Pass | Already exists |
| RFC 7807 Problem Details | ✅ Pass | GlobalExceptionHandler configured |
| Keys in appsettings (dev) | ✅ Pass | Will add Google credentials to appsettings |
| Azurite for local storage | ✅ Pass | Already configured |
| **IV. Quality & Testing** | | |
| TDD workflow | ✅ Plan | Will write tests first for handlers |
| 80% coverage threshold | ✅ Plan | Auth handlers will be unit tested |
| Integration tests for endpoints | ✅ Plan | Happy path tests for auth endpoints |
| **V. Operations & Azure** | | |
| Secrets in Key Vault (prod) | ✅ Plan | Google secrets → Key Vault for production |
| Serilog structured logging | ✅ Pass | Already configured |

### Post-Phase 1 Check ✅ PASSED

| Principle | Compliance | Notes |
|-----------|------------|-------|
| **II. Architecture** | | |
| Shared project DTOs only | ✅ Pass | UserInfo, AuthStatus are DTOs only |
| Repository pattern in TableStorage | ✅ Pass | UserRepository follows DailyLogRepository pattern |
| **III. Implementation** | | |
| Minimal APIs | ✅ Pass | `/api/auth/login`, `/api/auth/logout`, `/api/auth/me` |
| `.http` files maintained | ✅ Plan | Will update api.http with new endpoints |
| **IV. Quality & Testing** | | |
| Unit tests for handlers | ✅ Plan | GoogleAuthHandler tests defined |
| Integration tests for endpoints | ✅ Plan | Auth endpoint tests defined |
| bUnit tests for components | ✅ Plan | Login.razor tests defined |
| **V. Operations & Azure** | | |
| User data in Table Storage | ✅ Pass | Users table with email as PartitionKey |

**Gate Status**: ✅ PASS — No violations

## Project Structure

### Documentation (this feature)

```text
specs/002-google-auth/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output (Google Cloud setup)
├── contracts/           # Phase 1 output
│   └── openapi.yaml     # Auth endpoint contracts
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── PoNovaWeight.Api/
│   ├── Features/
│   │   └── Auth/
│   │       ├── Endpoints.cs           # Modified: Google OAuth endpoints
│   │       ├── GoogleAuthHandler.cs   # New: Sign-in callback handler
│   │       └── VerifyPasscode.cs      # Removed: Passcode auth
│   └── Infrastructure/
│       ├── AuthMiddleware.cs          # Modified: Cookie-based session validation
│       └── TableStorage/
│           ├── UserEntity.cs          # New: User profile entity
│           ├── IUserRepository.cs     # New: User repository interface
│           └── UserRepository.cs      # New: User repository implementation
├── PoNovaWeight.Client/
│   ├── Pages/
│   │   └── Login.razor                # New: Login page with Google button
│   └── Services/
│       └── AuthService.cs             # Modified: Google auth support
└── PoNovaWeight.Shared/
    └── DTOs/
        ├── AuthDtos.cs                # Modified: Add UserInfo DTO
        └── (PasscodeRequest removed)

tests/
├── PoNovaWeight.Api.Tests/
│   ├── Unit/
│   │   └── Auth/
│   │       └── GoogleAuthHandlerTests.cs  # New
│   └── Integration/
│       └── Auth/
│           └── AuthEndpointsTests.cs      # Modified
└── PoNovaWeight.Client.Tests/
    └── Components/
        └── LoginTests.cs                  # New
```

**Structure Decision**: Follows existing vertical slice architecture. Auth feature stays in `/Features/Auth/`. User persistence added to `/Infrastructure/TableStorage/` following the existing DailyLogRepository pattern.

## Complexity Tracking

> No violations — no entries needed

