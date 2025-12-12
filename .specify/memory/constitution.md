<!--
================================================================================
SYNC IMPACT REPORT
================================================================================
Version Change: 1.0.0 (no change - validation only)

Modified Principles: None

Added Sections: None

Removed Sections: None

Validation Performed (2025-12-11):
  - All 5 principle sections verified against user requirements
  - Foundation: Solution naming, .NET 10, package management, null safety ✅
  - Architecture: Vertical slice, SOLID, Minimal APIs, repository structure ✅
  - Implementation: API/Backend, Frontend, Development Environment ✅
  - Quality & Testing: Code hygiene, TDD, coverage, testing tiers ✅
  - Operations & Azure: Bicep, CI/CD, observability, cost management ✅

Templates Requiring Updates:
  - .specify/templates/plan-template.md: ✅ Aligned (Constitution Check section)
  - .specify/templates/spec-template.md: ✅ Aligned (generic structure)
  - .specify/templates/tasks-template.md: ✅ Aligned (generic structure)
  - .specify/templates/agent-file-template.md: ✅ Aligned (generic structure)
  - .specify/templates/checklist-template.md: ✅ Aligned (generic structure)

Follow-up TODOs: None
================================================================================
-->

# PoNovaWeight Constitution

## Core Principles

### I. Foundation

**Solution Naming**
- The `.sln` file name (e.g., `PoNovaWeight`) MUST be the base identifier
- This name MUST be used for all Azure services and resource groups (e.g., `PoNovaWeight-rg`, `PoNovaWeight-app`)
- The user-facing HTML `<title>` MUST use this name
- Bookmarks MUST display simply as `PoNovaWeight`

**.NET Version**
- All projects MUST target .NET 10
- The `global.json` file MUST be locked to a `10.0.xxx` SDK version
- Latest C# language features MUST be used

**Package Management**
- All NuGet packages MUST be managed centrally in a `Directory.Packages.props` file at the repository root

**Null Safety**
- Nullable Reference Types (`<Nullable>enable</Nullable>`) MUST be enabled in all `.csproj` files

---

### II. Architecture

**Code Organization**
- The API MUST use Vertical Slice Architecture
- All API logic (endpoints, CQRS handlers) MUST be co-located by feature in `/src/Po.[AppName].Api/Features/`

**Design Philosophy**
- SOLID principles and standard GoF design patterns MUST be applied
- Pattern usage MUST be documented in code comments or the root `README.md`

**API Design**
- Minimal APIs MUST be used for all new endpoints
- The API project MUST host the Blazor WASM project

**Repository Structure**
- The standard root folder structure MUST be: `/src`, `/tests`, `/docs`, `/infra`, and `/scripts`
- `/src` projects MUST follow separation of concerns: `...Api`, `...Client`, and `...Shared`
- The `...Shared` project MUST only contain DTOs, contracts, and shared validation logic (e.g., FluentValidation rules) referenced by both `...Api` and `...Client` projects. It MUST NOT contain any business logic or data access code
- `/docs` MUST contain: `README.md` (app description and run instructions), mermaid diagrams, KQL query library, and ADRs
- `/scripts` MUST contain helper scripts created by the coding LLM

---

### III. Implementation

**API & Backend**

- *API Documentation*: All API endpoints MUST have Swagger (OpenAPI) generation enabled. `.http` files MUST be maintained for manual verification
- *Health Checks*: A health check endpoint MUST exist at `api/health` that validates connectivity to all external dependencies
- *Error Handling*: All non-successful API responses (4xx, 5xx) MUST return an `IResult` that serializes to an RFC 7807 Problem Details JSON object. Structured `ILogger.LogError` MUST be used within all catch blocks

**Frontend (Blazor)**

- *UI Framework*: Standard Blazor WASM controls are the primary component library. Radzen.Blazor MAY only be used for complex requirements as needed
- *Responsive Design*: The UI MUST be mobile-first (portrait mode), responsive, fluid, and touch-friendly

**Development Environment**

- *Debug Launch*: The environment MUST support a one-step 'F5' debug launch for the API and browser. A `launch.json` with a `serverReadyAction` MUST be committed to the repository
- *Keys*: All keys MUST be stored in `appsettings.json` until the app is deployed to Azure. `Program.cs` MUST be configured to read from Azure Key Vault only when `ASPNETCORE_ENVIRONMENT` is `Production`. After deployment, both local and Azure code SHOULD refer to keys in Azure Key Vault, with the exception of local code using Azurite instead of Azure Storage
- *Local Storage*: Azurite MUST be used for local development and integration testing

---

### IV. Quality & Testing

**Code Hygiene**
- All build warnings/errors MUST be resolved before pushing changes to GitHub
- `dotnet format` MUST be run to ensure style consistency

**Dependency Hygiene**
- Updates to all packages via `Directory.Packages.props` MUST be checked and applied regularly

**Workflow**
- A TDD workflow (Red → Green → Refactor) MUST be applied for all business logic (e.g., MediatR handlers, domain services)
- For UI and E2E tests, tests MUST be written contemporaneously with the feature code

**Test Naming**
- Test methods MUST follow the `MethodName_StateUnderTest_ExpectedBehavior` convention

**Code Coverage (dotnet-coverage)**
- A minimum 80% line coverage threshold MUST be enforced for all new business logic
- No more than 50 tests total MUST exist in the entire solution
- A combined coverage report MUST be generated in `docs/coverage/`

**Unit Tests (xUnit)**
- MUST cover all backend business logic (e.g., MediatR handlers) with all external dependencies mocked

**Component Tests (bUnit)**
- MUST cover all new Blazor components (rendering, user interactions, state changes), mocking dependencies like `IHttpClientFactory`

**Integration Tests (xUnit)**
- A "happy path" test MUST be created for every new API endpoint, running against a test host and an in-memory database emulator
- Realistic test data SHOULD be generated

**E2E Tests (Playwright)**
- The API MUST be started before running E2E tests
- Tests MUST target Chromium (mobile and desktop views)
- *Full-Stack E2E (Default)*: MUST run the entire stack (frontend + API + test database) to validate a true user flow
- *Isolated E2E (By Exception)*: Network mocking SHOULD only be used for specific scenarios that are difficult to set up (e.g., simulating a 3rd-party payment provider failure)
- Automated accessibility and visual regression checks MUST be integrated

---

### V. Operations & Azure

**Provisioning**
- All Azure infrastructure MUST be provisioned using Bicep (from the `/infra` folder) and deployed via Azure Developer CLI (`azd`)

**CI/CD**
- The GitHub Actions workflow MUST use Federated Credentials (OIDC) for secure, secret-less connection to Azure
- The YAML file MUST build the code and deploy it to the resource group (e.g., `PoNovaWeight-rg`) as an App Service (e.g., `PoNovaWeight-app`)

**Required Services**
- Bicep scripts MUST provision, at minimum: Application Insights & Log Analytics, App Service, and Azure Storage—all in the same resource group

**Cost Management**
- A $5 monthly cost budget MUST be created for the application's resource group
- The budget MUST be configured with an Action Group to send an email alert to `punkouter26@gmail.com` when 80% of the threshold is met

**Logging**
- Serilog MUST be used for all structured logging
- Configuration MUST be driven by `appsettings.json` to write to the Debug Console (in Development) and Application Insights (in Production)

**Telemetry**
- Modern OpenTelemetry abstractions MUST be used for all custom telemetry
- `Meter` MUST be used to create custom metrics for business-critical values

**Production Diagnostics**
- The Application Insights Snapshot Debugger MUST be enabled on the App Service
- The Application Insights Profiler MUST be enabled on the App Service

**KQL Library**
- The `docs/kql/` folder MUST be populated with essential queries for monitoring app-specific parameters, users, and actions performed

---

## Governance

**Authority**
- This Constitution supersedes all other development practices and ad-hoc decisions
- All PRs and code reviews MUST verify compliance with these principles

**Amendment Process**
1. Propose changes with rationale in a dedicated PR
2. Document the change impact on existing code
3. Provide a migration plan for any breaking changes
4. Update dependent artifacts (templates, documentation) in the same PR

**Versioning Policy**
- MAJOR: Backward-incompatible governance/principle removals or redefinitions
- MINOR: New principle/section added or materially expanded guidance
- PATCH: Clarifications, wording, typo fixes, non-semantic refinements

**Compliance Review**
- Constitution compliance MUST be checked before Phase 0 research and after Phase 1 design (per plan-template.md)
- Violations MUST be justified in the Complexity Tracking section or remediated

**Version**: 1.0.0 | **Ratified**: 2025-12-10 | **Last Amended**: 2025-12-11
