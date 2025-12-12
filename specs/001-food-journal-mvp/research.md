# Research: PoNovaWeight Food Journal MVP

**Date**: 2025-12-10  
**Branch**: `001-food-journal-mvp`

## Technology Decisions

### 1. .NET Version: 10 vs 8

**Decision**: .NET 10

**Rationale**: Constitution mandates .NET 10 for all projects. User's initial technical spec referenced .NET 8, but Constitution supersedes.

**Alternatives Considered**:
- .NET 8 LTS: Stable, user's preference, but violates Constitution I.Foundation
- .NET 9: Available, but Constitution specifically requires .NET 10

---

### 2. Storage: Azure Table Storage vs SQL

**Decision**: Azure Table Storage (NoSQL)

**Rationale**:
- Perfect fit for key-value access pattern (PartitionKey=UserId, RowKey=Date)
- Range queries for weekly data retrieval in single request
- Low cost at scale (~$0.045/GB/month)
- Azurite provides local emulator for development
- No schema migrations needed for MVP

**Alternatives Considered**:
- Azure SQL Database: Higher cost, schema overhead, unnecessary relational features
- Cosmos DB: Overkill for simple key-value pattern, higher cost
- SQLite: No cloud sync, would require additional sync layer

**Partition Strategy**:
- `PartitionKey`: `"dev-user"` (single user MVP, extensible later)
- `RowKey`: `yyyy-MM-dd` format for range queries

---

### 3. AI Integration: Azure OpenAI vs Other Providers

**Decision**: Azure OpenAI Service (GPT-4o model)

**Rationale**:
- GPT-4o has superior vision capabilities for food recognition
- Azure service = single cloud provider, simplified auth
- JSON Mode guarantees machine-parsable output
- Enterprise compliance (data stays in Azure)

**Alternatives Considered**:
- OpenAI Direct API: Requires separate API key management
- Google Gemini: Less proven for food portion estimation
- On-device ML: Insufficient accuracy for portion-to-unit conversion

**Prompt Engineering**:
- System message enforces JSON Mode
- Output schema: `{ "proteins": int, "vegetables": int, "fruits": int, "starches": int, "fats": int, "dairy": int }`
- Include Nova wellness rules in system prompt for accurate unit mapping

---

### 4. Frontend Framework: Blazor WASM vs Other SPA

**Decision**: Blazor WebAssembly (Hosted)

**Rationale**:
- Single language (C#) across full stack
- Constitution mandates Blazor with standard controls
- Hosted model eliminates CORS complexity
- PWA support built-in with service worker template
- Strong typing with shared DTOs

**Alternatives Considered**:
- React/Vue + API: Two languages, separate deployments
- Blazor Server: Requires persistent SignalR connection, poor offline
- MAUI Hybrid: Overkill for web-first requirement

---

### 5. CSS Framework: Tailwind vs Component Libraries

**Decision**: Tailwind CSS (utility-first)

**Rationale**:
- Lightweight, mobile-first by design
- No component library bloat
- Constitution allows Radzen only "as needed"—Tailwind sufficient for this UI
- CLI watch process for development

**Alternatives Considered**:
- Bootstrap: Heavier, opinionated components
- Material Design: Heavy library, enterprise aesthetic
- Radzen.Blazor: Full component library, more than needed for simple steppers/cards

---

### 6. Authentication: Passcode vs Full Identity

**Decision**: Simple passcode (app secret)

**Rationale**:
- MVP with single/few users doesn't need identity provider
- Protects AI endpoint from unauthorized access
- Stored in appsettings (dev) / Key Vault (prod)
- Session state via browser storage (clears on close per clarification)

**Alternatives Considered**:
- Azure AD B2C: Complex setup, overkill for MVP
- ASP.NET Identity: Database overhead, unnecessary features
- No auth: Exposes AI costs to public

---

### 7. Image Processing: Client-Side vs Server-Side

**Decision**: Client-side compression (browser APIs)

**Rationale**:
- Reduces upload payload significantly (max 1024px width)
- Lowers Azure OpenAI token costs
- Faster perceived response time
- Browser Canvas API readily available in Blazor via JS interop

**Alternatives Considered**:
- Server-side resize: Larger uploads, wasted bandwidth
- No compression: Excessive token usage, slower analysis

---

### 8. Testing Strategy: Coverage Within 50-Test Limit

**Decision**: Prioritized test allocation

**Rationale**: Constitution limits solution to ≤50 tests while requiring 80% coverage.

**Allocation**:
| Layer | Tests | Focus |
|-------|-------|-------|
| Unit (xUnit) | ~15 | MediatR handlers, domain logic |
| Integration (xUnit) | ~10 | Repository + endpoints |
| Component (bUnit) | ~10 | Blazor components |
| E2E (Playwright) | ~10 | Critical user flows |
| **Total** | ~45 | Under 50 limit |

**High-Value Tests**:
1. GetWeekLogs handler (date range logic)
2. UpsertDailyLog handler (persistence)
3. MealAnalysis service (AI integration mock)
4. UnitStepper component (increment/decrement)
5. Dashboard E2E (weekly view load)
6. Logging E2E (stepper interaction)

---

## Best Practices Applied

### Blazor WASM PWA
- Service worker caches static assets + API responses for offline
- Manifest.json with proper icons, theme color, display: standalone
- Detect online/offline state for AI feature availability

### Azure Table Storage
- Use `TableClient` from `Azure.Data.Tables` SDK
- Implement retry policies for transient failures
- Batch operations for weekly data updates (if needed)

### Vertical Slice Architecture
- Each feature folder contains: endpoint, handler, validators
- MediatR for CQRS pattern within slices
- No cross-slice dependencies

### Error Handling
- Global exception handler middleware
- ProblemDetails factory for consistent RFC 7807 responses
- Structured logging with correlation IDs

---

## Unresolved Items

None. All technical decisions documented with rationale.
