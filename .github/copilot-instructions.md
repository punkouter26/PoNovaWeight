# PoNovaWeight - Copilot Instructions

## Engineering Standards

### Naming & Organization
- **Unified Identity**: `Po{SolutionName}` prefix for namespaces, Azure RGs, Aspire resources (e.g., `PoNovaWeight.Api`, `rg-PoNovaWeight-prod`)
- **Zero-Waste Codebase**: Delete unused files, dead code, and obsolete assets aggressively
- **GoF/SOLID Patterns**: Apply design patterns with explanatory comments

### Safety & Quality
- `Directory.Build.props` enforces `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<Nullable>enable</Nullable>`
- Central Package Management via `Directory.Packages.props` with transitive pinning - **never add versions to `.csproj`**
- Use `.copilotignore` to exclude `bin/`, `obj/`, `node_modules/` from AI focus

### Secrets & Configuration
- **Local**: `dotnet user-secrets` for Google OAuth, OpenAI keys
- **Cloud**: Azure Key Vault via Managed Identity (subscription: `Punkouter26`)
- **Shared Resources**: Common secrets in `PoShared` resource group

## Architecture Overview

**Blazor WebAssembly PWA** + **ASP.NET Core Minimal API** + **.NET Aspire** orchestration:
- **Azure Table Storage** over SQL (key-value access, ~$1/month) - see [ADR 001](../docs/adr/001-table-storage-over-sql.md)
- **Vertical slices** with MediatR (CQRS pattern)
- **OpenTelemetry** via Aspire ServiceDefaults → Application Insights

## Project Structure

| Project | Purpose |
|---------|---------|
| `PoNovaWeight.AppHost` | Aspire orchestrator - starts Azurite, API, and dashboard |
| `PoNovaWeight.Api` | Minimal API (HTTP:5000, HTTPS:5001) serving REST + Blazor WASM |
| `PoNovaWeight.Client` | Blazor WebAssembly frontend (served by API) |
| `PoNovaWeight.Shared` | DTOs, contracts, FluentValidation validators |
| `PoNovaWeight.ServiceDefaults` | OpenTelemetry, health checks, HTTP resilience |

## Developer Workflow

### Running the App
```bash
# Preferred: Run with Aspire (starts Azurite + API + Dashboard)
dotnet run --project src/PoNovaWeight.AppHost

# Dashboard: https://localhost:15888
# Client: http://localhost:5000 (HTTP) or https://localhost:5001 (HTTPS)
```

### API Debugging
Use `.http` files in `src/PoNovaWeight.Api/http/` for endpoint testing (VS Code REST Client or Rider).

### Testing
```bash
# Unit and integration tests (mock repositories, no Azurite needed)
dotnet test

# E2E tests (requires API running)
cd tests/e2e && npm test

# E2E headed mode (for debugging)
npm run test:headed
```

## Key Patterns

### Feature Organization (Vertical Slices)
Each feature in `src/PoNovaWeight.Api/Features/` contains:
- `Endpoints.cs` - Minimal API route definitions
- Handler files (e.g., `GetDailyLog.cs`) - MediatR request + handler in same file

Example: [DailyLogs/GetDailyLog.cs](../src/PoNovaWeight.Api/Features/DailyLogs/GetDailyLog.cs)
```csharp
public record GetDailyLogQuery(DateOnly Date, string UserId = "dev-user") : IRequest<DailyLogDto?>;
public class GetDailyLogHandler(IDailyLogRepository repository) : IRequestHandler<GetDailyLogQuery, DailyLogDto?>
```

### Table Storage Keys
- **PartitionKey**: User email (from `HttpContext.GetUserId()`)
- **RowKey**: Date formatted as `yyyy-MM-dd` for lexicographic sorting

### Shared DTOs with Validation
DTOs in `PoNovaWeight.Shared/DTOs/` use **records** with `required` properties. Validators in `Shared/Validation/` use FluentValidation:
```csharp
RuleFor(x => x.Weight).InclusiveBetween(50m, 500m).When(x => x.Weight.HasValue);
```

### User Context
Use `HttpContext.GetUserId()` extension (returns email claim or throws `UnauthorizedAccessException`).

### Exception Handling
`GlobalExceptionHandler` converts exceptions to RFC 7807 ProblemDetails:
- `UnauthorizedAccessException` → 401
- `ArgumentException` / `InvalidOperationException` → 400

## Testing Conventions

### Testing Strategy
| Layer | Tool | Purpose |
|-------|------|---------|
| **Unit (C#)** | xUnit + Moq | Pure logic, handlers, domain rules |
| **Integration (C#)** | `CustomWebApplicationFactory` | API endpoints with mocked repos |
| **Integration (Docker)** | Testcontainers + Azurite | Real Table Storage operations |
| **E2E (TS)** | Playwright (Chromium + Mobile) | Critical user paths, headless in CI |

### Test Consolidation (≤20 per tier)
Use `[Theory]` with `[InlineData]` to consolidate related tests:
```csharp
[Theory]
[InlineData(UnitCategory.Proteins, 5, 1, 6, "increment")]
[InlineData(UnitCategory.Vegetables, 3, -1, 2, "decrement")]
public async Task IncrementUnit_Scenarios(UnitCategory cat, int initial, int delta, int expected, string scenario) { }
```

### Unit Tests
Mock repository interfaces, test handlers directly:
```csharp
var mockRepo = new Mock<IDailyLogRepository>();
var handler = new GetDailyLogHandler(mockRepo.Object);
```

### Integration Tests (Mocked)
`CustomWebApplicationFactory` replaces repositories with mocks - no Azurite required.

### Integration Tests (Docker/Azurite)
Real Table Storage testing with Testcontainers:
```csharp
[Collection("Azurite")]
[Trait("Category", "Docker")]
public class AzuriteRepositoryTests(AzuriteFixture fixture) { }
```
Run with: `dotnet test --filter "Category=Docker"`

### Auth Bypass for Tests
- **Production**: Google OAuth via `AddGoogle()`
- **Development**: Use `/api/auth/dev-login` endpoint to bypass OAuth (creates cookie session)
- **Integration Tests**: `CustomWebApplicationFactory` with `TestAuthHandler` injects test auth config

### Dev Login Endpoint
In Development environment, use `POST /api/auth/dev-login?email=dev-user@local` to authenticate without Google OAuth:
```http
POST https://localhost:5001/api/auth/dev-login?email=dev-user@local
```

## External Integrations

- **Google OAuth**: Required credentials in user secrets or `appsettings.Development.json`
- **Azure OpenAI**: Meal scanning AI via `IMealAnalysisService`
- **Azurite**: Local storage emulator, automatically started by Aspire
- **Health Endpoints**: `/health` and `/api/health` verify API and storage connectivity
- **Swagger UI**: Available at `/swagger` in Development mode
- **OpenAPI Spec**: Available at `/openapi/v1.json`
