# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Nova Food Journal - A PWA for tracking daily nutritional intake using the Nova Physician Wellness Center unit-based system. Single-user MVP architecture with AI-powered meal scanning.

**Tech Stack**: Blazor WASM + ASP.NET Core Minimal API + Azure Table Storage + Azure OpenAI GPT-4o + .NET 10.0 + .NET Aspire 9.0

## Build and Development Commands

```bash
# Development with Aspire (includes Azurite emulator + observability dashboard)
dotnet run --project src/PoNovaWeight.AppHost

# Run API standalone
cd src/PoNovaWeight.Api && dotnet run

# Tailwind CSS watch mode (separate terminal)
cd src/PoNovaWeight.Client && npm run watch

# Build all projects
dotnet build

# Run all tests
dotnet test

# Run tests with PowerShell script
./scripts/run-tests.ps1 -Type All -Verbose
./scripts/run-tests.ps1 -Type Unit
./scripts/run-tests.ps1 -Filter "DailyLog"

# Docker build
docker build -f src/PoNovaWeight.Api/Dockerfile -t ponovaweight-api:latest .
```

## Architecture

### Project Structure

- `src/PoNovaWeight.Api/` - ASP.NET Core Minimal API with feature-based CQRS organization
- `src/PoNovaWeight.Client/` - Blazor WASM PWA frontend with Tailwind CSS
- `src/PoNovaWeight.Shared/` - DTOs and FluentValidation validators
- `src/PoNovaWeight.AppHost/` - .NET Aspire orchestration for local development
- `src/PoNovaWeight.ServiceDefaults/` - OpenTelemetry, health checks, resilience defaults
- `tests/` - xUnit + Moq (API), bUnit (Client), Playwright (E2E)
- `infra/` - Azure Bicep IaC

### API Feature Organization (CQRS Pattern)

Each feature in `src/PoNovaWeight.Api/Features/` follows this structure:
- `Endpoints.cs` - Static class with `MapGroup()` for Minimal API routes
- `*Query.cs` / `*Command.cs` - MediatR request records
- `*Handler.cs` - MediatR handlers with business logic
- Validators live in `PoNovaWeight.Shared/Validators/`

Features: Auth (Google OAuth + Cookie), DailyLogs (CRUD), MealScan (AI analysis), WeeklySummary, Health

### Key Data Flows

1. **Daily Log**: Blazor Component → ApiClient → Minimal API → MediatR Handler → Repository → Azure Table Storage
2. **Meal Scan**: Camera → Compress to 800px → API → Azure OpenAI Vision → User confirms → Upsert log
3. **Auth**: Login → VerifyPasscode → HttpOnly Cookie → SessionService validates

### Repository Pattern

`IDailyLogRepository` in `Infrastructure/TableStorage/` abstracts Azure Table Storage. Uses `UseDevelopmentStorage=true` for local Azurite.

## Code Conventions

- **TreatWarningsAsErrors**: Enabled globally - all builds must be warning-free
- **File-scoped namespaces**: Required per .editorconfig
- **FluentValidation**: All request DTOs must have corresponding validators
- **XML Doc Comments**: Required on public types/methods
- **Primary Constructors**: Preferred for DI in handlers/services

## Testing

- **Unit Tests**: xUnit + Moq + FluentAssertions in `tests/PoNovaWeight.Api.Tests/`
- **Component Tests**: bUnit in `tests/PoNovaWeight.Client.Tests/`
- **E2E Tests**: Playwright in `tests/PoNovaWeight.E2E/`
- **Test User**: Use `"dev-user"` as UserID in test scenarios

## Configuration

Local development uses `appsettings.Development.json` with:
- `UseDevelopmentStorage=true` for Azurite
- Google OAuth credentials
- Azure OpenAI endpoint/key
- `DefaultPassCode` for auth testing

Production uses environment variables configured via Azure Container Apps.

## Package Management

Centralized package versions in `Directory.Packages.props`. When adding packages, add version to this file and reference without version in project file.
