# Implementation Plan: PoNovaWeight Food Journal MVP

**Branch**: `001-food-journal-mvp` | **Date**: 2025-12-10 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-food-journal-mvp/spec.md`

## Summary

PoNovaWeight is a mobile-first Progressive Web App (PWA) that digitizes the Nova Physician Wellness Center's paper-based food journaling system. The app tracks proprietary "Units" (Proteins, Vegetables, Fruits, Starches/Carbs, Fats, Dairy) rather than calories, with AI-powered meal photo analysis. Built as a Blazor WASM hosted application with Azure Table Storage persistence and Azure OpenAI integration.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (per Constitution; user spec mentioned .NET 8—upgraded)  
**Primary Dependencies**: Blazor WASM, Minimal APIs, MediatR, FluentValidation, Serilog, Azure.Data.Tables, Azure.AI.OpenAI  
**Storage**: Azure Table Storage (NoSQL) with Azurite for local development  
**Testing**: xUnit (unit/integration), bUnit (component), Playwright (E2E)  
**Target Platform**: PWA targeting mobile browsers (Chromium), portrait-first responsive design  
**Project Type**: Web application (Blazor WASM hosted by API)  
**Performance Goals**: Dashboard load <3s on 4G, AI response <10s for 95% requests, stepper updates <100ms  
**Constraints**: $5/month Azure budget, offline-capable for cached data, single shared passcode auth  
**Scale/Scope**: Single user (MVP), ~50 daily logs/month, <10 AI requests/day expected

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Foundation

| Requirement | Status | Notes |
|-------------|--------|-------|
| Solution named `PoNovaWeight.sln` | ✅ PASS | Base identifier for Azure resources |
| .NET 10 target | ✅ PASS | global.json locks to 10.0.xxx SDK |
| Directory.Packages.props | ✅ PASS | Centralized NuGet management |
| Nullable enabled | ✅ PASS | All .csproj files |

### II. Architecture

| Requirement | Status | Notes |
|-------------|--------|-------|
| Vertical Slice Architecture | ✅ PASS | Features in `/src/PoNovaWeight.Api/Features/` |
| SOLID + GoF patterns | ✅ PASS | Documented in README.md |
| Minimal APIs | ✅ PASS | All endpoints |
| API hosts Blazor WASM | ✅ PASS | Hosted model |
| `/src`, `/tests`, `/docs`, `/infra`, `/scripts` | ✅ PASS | Standard structure |
| `...Api`, `...Client`, `...Shared` separation | ✅ PASS | Three projects |
| Shared = DTOs/contracts only | ✅ PASS | No business logic |

### III. Implementation

| Requirement | Status | Notes |
|-------------|--------|-------|
| Swagger/OpenAPI enabled | ✅ PASS | All endpoints documented |
| `.http` files maintained | ✅ PASS | Manual verification |
| Health check at `api/health` | ✅ PASS | Validates Table Storage + OpenAI |
| RFC 7807 Problem Details | ✅ PASS | All error responses |
| Structured ILogger.LogError | ✅ PASS | All catch blocks |
| Standard Blazor controls primary | ✅ PASS | Radzen only if needed |
| Mobile-first responsive | ✅ PASS | Portrait-first PWA |
| F5 debug launch | ✅ PASS | launch.json committed |
| Keys in appsettings (dev) / Key Vault (prod) | ✅ PASS | Conditional config |
| Azurite for local storage | ✅ PASS | Development + integration tests |

### IV. Quality & Testing

| Requirement | Status | Notes |
|-------------|--------|-------|
| Build warnings resolved | ✅ PASS | Pre-push requirement |
| `dotnet format` style | ✅ PASS | Consistency |
| TDD for business logic | ✅ PASS | MediatR handlers |
| UI/E2E tests contemporaneous | ✅ PASS | Written with features |
| `MethodName_StateUnderTest_ExpectedBehavior` | ✅ PASS | Test naming |
| 80% coverage threshold | ✅ PASS | New business logic |
| ≤50 tests total | ✅ PASS | Solution limit |
| xUnit for unit/integration | ✅ PASS | Backend tests |
| bUnit for components | ✅ PASS | Blazor components |
| Playwright E2E | ✅ PASS | Chromium mobile/desktop |
| Accessibility + visual regression | ✅ PASS | Integrated |

### V. Operations & Azure

| Requirement | Status | Notes |
|-------------|--------|-------|
| Bicep in `/infra` | ✅ PASS | IaC |
| Azure Developer CLI (`azd`) | ✅ PASS | Deployment |
| OIDC (Federated Credentials) | ✅ PASS | GitHub Actions |
| App Insights + Log Analytics + App Service + Storage | ✅ PASS | Minimum services |
| $5 budget with 80% alert | ✅ PASS | punkouter26@gmail.com |
| Serilog structured logging | ✅ PASS | Console (dev) / App Insights (prod) |
| OpenTelemetry + Meter | ✅ PASS | Custom metrics |
| Snapshot Debugger enabled | ✅ PASS | Production |
| Profiler enabled | ✅ PASS | Production |
| KQL library in `docs/kql/` | ✅ PASS | Monitoring queries |

**Pre-Phase 0 Gate Result**: ✅ **PASS** (0 violations)

## Project Structure

### Documentation (this feature)

```text
specs/001-food-journal-mvp/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI specs)
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
PoNovaWeight.sln
global.json
Directory.Packages.props
Directory.Build.props

src/
├── PoNovaWeight.Api/
│   ├── Features/
│   │   ├── DailyLogs/
│   │   │   ├── GetWeekLogs.cs          # GET /api/logs/week/{date}
│   │   │   ├── UpsertDailyLog.cs       # POST /api/logs
│   │   │   └── Endpoints.cs            # Minimal API route mapping
│   │   ├── MealScan/
│   │   │   ├── AnalyzeMealPhoto.cs     # POST /api/scan
│   │   │   └── Endpoints.cs
│   │   ├── Auth/
│   │   │   ├── ValidatePasscode.cs     # POST /api/auth/validate
│   │   │   └── Endpoints.cs
│   │   └── Health/
│   │       └── Endpoints.cs            # GET /api/health
│   ├── Infrastructure/
│   │   ├── TableStorage/
│   │   │   └── DailyLogRepository.cs
│   │   └── OpenAI/
│   │       └── MealAnalysisService.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
│
├── PoNovaWeight.Client/
│   ├── Pages/
│   │   ├── Dashboard.razor             # Weekly view (7 day cards)
│   │   ├── DayDetail.razor             # Unit steppers + water tracker
│   │   ├── MealScanConfirm.razor       # AI suggestion review
│   │   └── Login.razor                 # Passcode entry
│   ├── Components/
│   │   ├── DayCard.razor               # Progress bars per category
│   │   ├── UnitStepper.razor           # +/- buttons
│   │   ├── WaterTracker.razor          # 8-segment tracker
│   │   └── WeeklySummary.razor         # Aggregated totals
│   ├── Services/
│   │   ├── ApiClient.cs                # HTTP calls to API
│   │   └── SessionService.cs           # Passcode state management
│   ├── wwwroot/
│   │   ├── manifest.json               # PWA manifest
│   │   ├── service-worker.js           # Offline caching
│   │   └── css/
│   │       └── app.css                 # Tailwind output
│   ├── Program.cs
│   └── _Imports.razor
│
└── PoNovaWeight.Shared/
    ├── DTOs/
    │   ├── DailyLogDto.cs
    │   ├── WeeklySummaryDto.cs
    │   ├── MealScanRequestDto.cs
    │   └── MealScanResultDto.cs
    ├── Contracts/
    │   └── UnitCategory.cs             # Enum + target definitions
    └── Validation/
        └── DailyLogValidator.cs        # FluentValidation rules

tests/
├── PoNovaWeight.Api.Tests/
│   ├── Unit/
│   │   ├── GetWeekLogsHandlerTests.cs
│   │   ├── UpsertDailyLogHandlerTests.cs
│   │   └── MealAnalysisServiceTests.cs
│   └── Integration/
│       ├── DailyLogRepositoryTests.cs  # Against Azurite
│       └── EndpointTests.cs            # WebApplicationFactory
│
├── PoNovaWeight.Client.Tests/
│   └── Components/
│       ├── DayCardTests.cs             # bUnit
│       ├── UnitStepperTests.cs
│       └── WaterTrackerTests.cs
│
└── PoNovaWeight.E2E/
    ├── playwright.config.ts
    └── tests/
        ├── dashboard.spec.ts           # Weekly view E2E
        ├── logging.spec.ts             # Unit entry E2E
        └── meal-scan.spec.ts           # AI flow E2E

docs/
├── README.md                           # App description + run instructions
├── architecture.md                     # Mermaid diagrams
├── coverage/                           # Generated coverage reports
├── kql/
│   ├── user-activity.kql
│   ├── ai-usage.kql
│   └── error-analysis.kql
└── adr/
    └── 001-table-storage-over-sql.md

infra/
├── main.bicep
├── modules/
│   ├── app-service.bicep
│   ├── storage.bicep
│   ├── app-insights.bicep
│   └── budget.bicep
└── azure.yaml                          # azd configuration

scripts/
├── setup-azurite.ps1
├── run-tests.ps1
└── generate-coverage.ps1
```

**Structure Decision**: Blazor WASM Hosted model with three projects per Constitution II.Architecture. The Api project hosts the Client and references Shared. Vertical Slice Architecture organizes API features by domain concern (DailyLogs, MealScan, Auth, Health).

## Complexity Tracking

> No Constitution violations identified. No complexity justifications required.

---

## Phase 1 Artifacts Generated

| Artifact | Path | Purpose |
|----------|------|---------|
| Data Model | `data-model.md` | Azure Table Storage schema, C# entities, DTOs, validation rules |
| API Contracts | `contracts/openapi.yaml` | OpenAPI 3.0 specification for all endpoints |
| Quickstart | `quickstart.md` | Developer setup and local run instructions |
| Research | `research.md` | 8 technology decisions with rationale |

## Post-Design Constitution Re-Check

*Re-evaluated after Phase 1 design completion.*

### Critical Validations

| Check | Status | Evidence |
|-------|--------|----------|
| .NET 10 throughout | ✅ PASS | data-model.md uses C# 13 features (required, init-only) |
| Vertical Slice preserved | ✅ PASS | API structure in project tree follows Features/ pattern |
| ≤50 tests feasible | ✅ PASS | research.md allocates ~45 tests across unit/bUnit/E2E |
| $5 budget respected | ✅ PASS | Azure Table Storage + OpenAI pay-per-use within limits |
| Shared = DTOs only | ✅ PASS | data-model.md DTOs are behavior-free records |
| OpenAPI documented | ✅ PASS | contracts/openapi.yaml covers all 7 endpoints |
| Mobile-first design | ✅ PASS | PWA manifest + portrait-first in quickstart |

**Post-Design Gate Result**: ✅ **PASS** (0 violations)

---

## Planning Phase Complete

**Status**: Phase 0 + Phase 1 complete. Ready for `/speckit.tasks` to generate Phase 2 implementation tasks.

**Next Command**: Run `/speckit.tasks` to generate `tasks.md` with granular implementation work items.
