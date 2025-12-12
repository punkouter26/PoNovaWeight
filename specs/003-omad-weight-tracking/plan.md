# Implementation Plan: OMAD Weight Tracking

**Branch**: `003-omad-weight-tracking` | **Date**: December 12, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-omad-weight-tracking/spec.md`

## Summary

Add OMAD (One Meal A Day) tracking functionality to PoNovaWeight, including weight logging, alcohol consumption tracking, OMAD compliance tracking, visual calendar view, streak calculation, and analytics (weight trends + alcohol correlation). This consolidates functionality from the PoOmad project into the existing daily logging page alongside nutrients and water tracking.

## Technical Context

**Language/Version**: C# 14 / .NET 10  
**Primary Dependencies**: MediatR, FluentValidation, Azure.Data.Tables, Blazor WASM  
**Storage**: Azure Table Storage (via Azurite for local dev)  
**Testing**: xUnit, bUnit, Playwright  
**Target Platform**: Web (mobile-first responsive, portrait mode)  
**Project Type**: Web application (API + Client + Shared)  
**Performance Goals**: 10-second daily logging (SC-001), 2-second calendar load (SC-002)  
**Constraints**: Online-only (no offline support), pounds only (no kg conversion)  
**Scale/Scope**: Single user per session, ~365 entries/year maximum

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliant | Notes |
|-----------|-----------|-------|
| .NET 10 | ✅ | Existing project already uses .NET 10 |
| Central Package Management | ✅ | Directory.Packages.props exists at root |
| Nullable Reference Types | ✅ | Enabled in all .csproj files |
| Vertical Slice Architecture | ✅ | Will add DailyLogs/OMAD slice in Features/ |
| Minimal APIs | ✅ | New endpoints use Minimal APIs |
| Shared DTOs only | ✅ | DailyLogDto extended in Shared project |
| Repository Pattern | ✅ | IDailyLogRepository pattern already established |
| TDD Workflow | ✅ | Unit tests for handlers, bUnit for components |
| 80% Coverage | ✅ | Will maintain coverage threshold |
| Azure Table Storage | ✅ | Consistent with existing infrastructure |
| Serilog Logging | ✅ | Already configured in Program.cs |
| RFC 7807 Problem Details | ✅ | ExceptionHandler already implements |

**Pre-Phase 0 Gate**: ✅ PASSED - No violations

## Project Structure

### Documentation (this feature)

```text
specs/003-omad-weight-tracking/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI specs)
│   └── omad-api.yaml
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── PoNovaWeight.Api/
│   ├── Features/
│   │   └── DailyLogs/           # Extend existing slice
│   │       ├── Endpoints.cs     # Add OMAD endpoints
│   │       ├── GetDailyLog.cs   # Extend to include OMAD fields
│   │       ├── UpsertDailyLog.cs # Extend to include OMAD fields
│   │       ├── DeleteDailyLog.cs # NEW: Delete handler
│   │       ├── GetMonthlyLogs.cs # NEW: Calendar data
│   │       ├── CalculateStreak.cs # NEW: Streak calculation
│   │       ├── GetWeightTrends.cs # NEW: Analytics
│   │       └── GetAlcoholCorrelation.cs # NEW: Analytics
│   └── Infrastructure/
│       └── TableStorage/
│           ├── DailyLogEntity.cs # Extend with OMAD fields
│           ├── DailyLogRepository.cs # Add delete + range queries
│           └── IDailyLogRepository.cs # Add delete method
├── PoNovaWeight.Client/
│   ├── Pages/
│   │   ├── DayDetail.razor      # Extend with OMAD section
│   │   └── Calendar.razor       # NEW: Monthly calendar view
│   ├── Components/
│   │   ├── OmadSection.razor    # NEW: OMAD/weight/alcohol input
│   │   ├── StreakDisplay.razor  # NEW: Streak counter
│   │   ├── CalendarGrid.razor   # NEW: Monthly grid
│   │   ├── WeightTrendChart.razor # NEW: Trend visualization
│   │   └── AlcoholInsights.razor # NEW: Correlation display
│   └── Services/
│       └── ApiClient.cs         # Extend with OMAD methods
└── PoNovaWeight.Shared/
    ├── DTOs/
    │   ├── DailyLogDto.cs       # Extend with OMAD fields
    │   ├── MonthlyLogsDto.cs    # NEW: Calendar response
    │   ├── StreakDto.cs         # NEW: Streak response
    │   ├── WeightTrendsDto.cs   # NEW: Trends response
    │   └── AlcoholCorrelationDto.cs # NEW: Correlation response
    └── Validation/
        └── DailyLogDtoValidator.cs # Extend with OMAD validation

tests/
├── PoNovaWeight.Api.Tests/
│   ├── Features/
│   │   └── DailyLogs/
│   │       ├── CalculateStreakTests.cs # NEW
│   │       ├── GetMonthlyLogsTests.cs # NEW
│   │       ├── GetWeightTrendsTests.cs # NEW
│   │       ├── GetAlcoholCorrelationTests.cs # NEW
│   │       └── DeleteDailyLogTests.cs # NEW
│   └── Integration/
│       └── OmadEndpointsTests.cs # NEW: Integration tests
└── PoNovaWeight.Client.Tests/
    └── Components/
        ├── OmadSectionTests.cs # NEW
        ├── StreakDisplayTests.cs # NEW
        └── CalendarGridTests.cs # NEW
```

**Structure Decision**: Extend existing Vertical Slice Architecture. OMAD fields are added to the existing DailyLogs feature slice rather than creating a new slice, as they are logically part of the same daily entry entity.

## Complexity Tracking

> No constitution violations - table not required.
