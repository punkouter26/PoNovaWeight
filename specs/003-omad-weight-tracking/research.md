# Research: OMAD Weight Tracking

**Feature**: 003-omad-weight-tracking  
**Date**: December 12, 2025  
**Purpose**: Resolve technical unknowns and establish best practices before implementation

## Research Summary

All technical decisions have been resolved. The implementation approach leverages existing patterns from both the current PoNovaWeight codebase and the reference PoOmad project.

---

## Decision 1: Extending DailyLogEntity vs New Entity

**Decision**: Extend existing `DailyLogEntity` with OMAD fields

**Rationale**:
- The daily log is conceptually a single record per user per day
- Existing infrastructure (repository, endpoints) can be extended rather than duplicated
- Azure Table Storage supports adding new properties without schema migration
- Maintains single-table design for atomic operations

**Alternatives Considered**:
- **Separate OmadEntry entity**: Rejected because it creates data synchronization issues and complicates the calendar view query
- **Separate Table**: Rejected because it requires cross-table queries and violates the single-responsibility of daily tracking

**Implementation Notes**:
- Add nullable properties for backward compatibility: `Weight`, `OmadCompliant`, `AlcoholConsumed`
- Existing entries without OMAD data will have null values (graceful degradation)

---

## Decision 2: Streak Calculation Approach

**Decision**: Calculate streak on-demand from stored logs (no persisted streak counter)

**Rationale**:
- Streaks must account for the "unlogged days don't break streak" rule
- Persisted counters require complex synchronization on edits/deletes
- Query performance is acceptable (~365 entries/year maximum)
- Reference implementation in PoOmad uses this approach successfully

**Alternatives Considered**:
- **Persisted streak field on UserEntity**: Rejected because edits/deletes require recalculation
- **Materialized view**: Rejected as over-engineering for single-user scale

**Implementation Notes**:
- Query all user logs, sort by date descending
- Count consecutive `OmadCompliant = true` entries
- Stop on first `OmadCompliant = false` (non-compliance breaks streak)
- Skip entries where `OmadCompliant` is null (unlogged)

---

## Decision 3: Weight Trend Gap-Filling Strategy

**Decision**: Carry-forward from last known weight

**Rationale**:
- Matches PoOmad implementation (proven approach)
- Simple and intuitive for users
- No statistical interpolation complexity
- Clear visual distinction with `IsCarryForward` flag

**Alternatives Considered**:
- **Linear interpolation**: Rejected as potentially misleading (implies gradual change)
- **Leave gaps empty**: Rejected as it breaks chart continuity
- **Average of neighbors**: Rejected as computationally complex and less intuitive

**Implementation Notes**:
- Set `IsCarryForward = true` for gap-filled points
- Display carried-forward values with distinct styling (dashed line or lighter color)

---

## Decision 4: Calendar Data Query Strategy

**Decision**: Single query for month range with client-side rendering

**Rationale**:
- Azure Table Storage partition key is user ID, row key is date
- Range query `RowKey ge '2025-12-01' and RowKey le '2025-12-31'` is efficient
- Returns ~31 rows maximum per query (minimal payload)
- Client renders grid from sparse data

**Alternatives Considered**:
- **Pre-computed calendar entities**: Rejected as over-engineering
- **Day-by-day API calls**: Rejected due to excessive network overhead

**Implementation Notes**:
- `GetMonthlyLogs` endpoint accepts year/month parameters
- Returns list of `DailyLogDto` for days with entries
- Client fills in gray indicators for missing days

---

## Decision 5: 5 lb Weight Change Confirmation

**Decision**: Client-side confirmation dialog with optional server bypass flag

**Rationale**:
- Primary purpose is preventing accidental typos
- Client-side dialog provides immediate feedback
- Server accepts `confirmWeightChange: true` flag to bypass validation
- Matches PoOmad implementation

**Alternatives Considered**:
- **Server-side only validation**: Rejected as poor UX (requires round-trip)
- **Hard limit (reject >5 lb change)**: Rejected as legitimate large changes occur

**Implementation Notes**:
- Client fetches previous day's weight on load
- If delta > 5 lbs, show confirmation modal before submit
- Include `ConfirmWeightChange` flag in `UpsertDailyLog` request

---

## Decision 6: Alcohol Correlation Statistics

**Decision**: Simple average comparison (no Pearson correlation coefficient)

**Rationale**:
- MVP scope - correlation coefficient requires statistical expertise to interpret
- Average weight comparison is immediately understandable
- Matches simplified analytics appropriate for personal tracking

**Alternatives Considered**:
- **Full Pearson correlation**: Considered for future enhancement
- **Moving average comparison**: Rejected as overly complex for MVP

**Implementation Notes**:
- Calculate: average weight on alcohol days vs non-alcohol days
- Require minimum 7 days with weight data
- Display difference as "X lbs higher/lower on alcohol days"

---

## Best Practices Applied

### Azure Table Storage
- Use user ID as partition key for efficient single-user queries
- Use date string (yyyy-MM-dd) as row key for range queries
- Leverage upsert (InsertOrReplace) for idempotent operations

### Blazor WASM
- Use `OnAfterRenderAsync` for initial data load
- Debounce rapid toggle inputs to prevent API spam
- Show optimistic UI updates with rollback on error

### MediatR CQRS
- Separate commands (mutations) from queries (reads)
- Use request/response records for immutability
- Leverage pipeline behaviors for cross-cutting concerns (validation, logging)

### FluentValidation
- Extend existing `DailyLogDtoValidator` with OMAD rules
- Weight range: 50-500 lbs
- Date validation: not in future

---

## Dependencies Confirmed

| Package | Current Version | Notes |
|---------|-----------------|-------|
| MediatR | 12.4.1 | Already installed |
| FluentValidation | 11.11.0 | Already installed |
| Azure.Data.Tables | 12.9.1 | Already installed |
| Microsoft.AspNetCore.Components.WebAssembly | 10.0.0 | Already installed |

No new package dependencies required.
