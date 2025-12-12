# Tasks: OMAD Weight Tracking

**Input**: Design documents from `/specs/003-omad-weight-tracking/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Tests are included for business logic (handlers) as required by constitution (TDD workflow).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Project initialization and shared infrastructure

- [X] T001 Extend DailyLogEntity with OMAD fields in src/PoNovaWeight.Api/Infrastructure/TableStorage/DailyLogEntity.cs
- [X] T002 [P] Extend DailyLogDto with OMAD fields in src/PoNovaWeight.Shared/DTOs/DailyLogDto.cs
- [X] T003 [P] Create MonthlyLogsDto in src/PoNovaWeight.Shared/DTOs/MonthlyLogsDto.cs
- [X] T004 [P] Create StreakDto in src/PoNovaWeight.Shared/DTOs/StreakDto.cs
- [X] T005 [P] Create WeightTrendsDto and TrendDataPoint in src/PoNovaWeight.Shared/DTOs/WeightTrendsDto.cs
- [X] T006 [P] Create AlcoholCorrelationDto in src/PoNovaWeight.Shared/DTOs/AlcoholCorrelationDto.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T007 Extend IDailyLogRepository with DeleteAsync method in src/PoNovaWeight.Api/Infrastructure/TableStorage/IDailyLogRepository.cs
- [X] T008 Implement DeleteAsync in DailyLogRepository in src/PoNovaWeight.Api/Infrastructure/TableStorage/DailyLogRepository.cs
- [X] T009 [P] Extend DailyLogDtoValidator with OMAD validation rules (weight 50-500 lbs, date not in future) in src/PoNovaWeight.Shared/Validation/DailyLogDtoValidator.cs
- [X] T010 Extend ApiClient with OMAD methods in src/PoNovaWeight.Client/Services/ApiClient.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Daily Health Log Entry (Priority: P1) 🎯 MVP

**Goal**: Allow users to log weight, OMAD compliance, and alcohol consumption alongside existing nutrients/water tracking

**Independent Test**: Can be fully tested by opening the app, entering today's weight, toggling OMAD compliance and alcohol consumption, and saving

### Tests for User Story 1

- [X] T011 [P] [US1] Unit test UpsertDailyLogHandler with OMAD fields in tests/PoNovaWeight.Api.Tests/Features/DailyLogs/UpsertDailyLogTests.cs
- [X] T012 [P] [US1] Unit test weight threshold validation (5 lb check) in tests/PoNovaWeight.Api.Tests/Features/DailyLogs/WeightThresholdTests.cs

### Implementation for User Story 1

- [X] T013 [US1] Extend UpsertDailyLogCommand and handler with OMAD fields in src/PoNovaWeight.Api/Features/DailyLogs/UpsertDailyLog.cs
- [X] T014 [US1] Implement weight threshold validation logic (5 lb confirmation) in src/PoNovaWeight.Api/Features/DailyLogs/UpsertDailyLog.cs
- [X] T015 [US1] Extend GetDailyLogQuery handler to return OMAD fields in src/PoNovaWeight.Api/Features/DailyLogs/GetDailyLog.cs
- [X] T016 [US1] Create OmadSection.razor component in src/PoNovaWeight.Client/Components/OmadSection.razor
- [X] T017 [US1] Integrate OmadSection into DayDetail.razor page in src/PoNovaWeight.Client/Pages/DayDetail.razor
- [X] T018 [US1] Add weight confirmation modal to DayDetail.razor in src/PoNovaWeight.Client/Pages/DayDetail.razor
- [X] T019 [P] [US1] bUnit test for OmadSection component in tests/PoNovaWeight.Client.Tests/Components/OmadSectionTests.cs

**Checkpoint**: User Story 1 complete - users can log weight, OMAD, and alcohol

---

## Phase 4: User Story 2 - Visual Calendar View (Priority: P2)

**Goal**: Display monthly calendar with color-coded OMAD compliance indicators

**Independent Test**: Can be tested by logging entries for several days, then viewing the calendar to see green/red/gray indicators

### Tests for User Story 2

- [X] T020 [P] [US2] Unit test GetMonthlyLogsHandler in tests/PoNovaWeight.Api.Tests/Features/DailyLogs/GetMonthlyLogsTests.cs

### Implementation for User Story 2

- [X] T021 [US2] Create GetMonthlyLogsQuery and handler in src/PoNovaWeight.Api/Features/DailyLogs/GetMonthlyLogs.cs
- [X] T022 [US2] Add monthly logs endpoint to Endpoints.cs in src/PoNovaWeight.Api/Features/DailyLogs/Endpoints.cs
- [X] T023 [P] [US2] Create CalendarGrid.razor component in src/PoNovaWeight.Client/Components/CalendarGrid.razor
- [X] T024 [US2] Create Calendar.razor page in src/PoNovaWeight.Client/Pages/Calendar.razor
- [X] T025 [US2] Add navigation link to Calendar in main layout in src/PoNovaWeight.Client/Shared/MainLayout.razor
- [X] T026 [P] [US2] bUnit test for CalendarGrid component in tests/PoNovaWeight.Client.Tests/Components/CalendarGridTests.cs

**Checkpoint**: User Story 2 complete - users can view OMAD history on calendar

---

## Phase 5: User Story 3 - OMAD Streak Tracking (Priority: P2)

**Goal**: Calculate and display current OMAD streak to motivate consistency

**Independent Test**: Can be tested by logging several consecutive OMAD-compliant days and verifying the streak counter increases

### Tests for User Story 3

- [X] T027 [P] [US3] Unit test CalculateStreakHandler (basic streak counting + unlogged days rule) in tests/PoNovaWeight.Api.Tests/Features/DailyLogs/CalculateStreakTests.cs

### Implementation for User Story 3

- [X] T029 [US3] Create CalculateStreakQuery and handler in src/PoNovaWeight.Api/Features/DailyLogs/CalculateStreak.cs
- [X] T030 [US3] Add streak endpoint to Endpoints.cs in src/PoNovaWeight.Api/Features/DailyLogs/Endpoints.cs
- [X] T031 [P] [US3] Create StreakDisplay.razor component in src/PoNovaWeight.Client/Components/StreakDisplay.razor
- [X] T032 [US3] Add StreakDisplay to Dashboard.razor in src/PoNovaWeight.Client/Pages/Dashboard.razor
- [X] T033 [P] [US3] bUnit test for StreakDisplay component in tests/PoNovaWeight.Client.Tests/Components/StreakDisplayTests.cs

**Checkpoint**: User Story 3 complete - users can see their OMAD streak

---

## Phase 6: User Story 4 - Weight Trends Analytics (Priority: P3)

**Goal**: Display weight trends over time with gap-filling for missing days

**Independent Test**: Can be tested by logging weight for at least 7 days and viewing the trend chart

### Tests for User Story 4

- [X] T034 [P] [US4] Unit test GetWeightTrendsHandler in tests/PoNovaWeight.Api.Tests/Features/DailyLogs/GetWeightTrendsTests.cs
- [X] T035 [P] [US4] Unit test gap-filling carry-forward logic in tests/PoNovaWeight.Api.Tests/Features/DailyLogs/GetWeightTrendsTests.cs

### Implementation for User Story 4

- [X] T036 [US4] Create GetWeightTrendsQuery and handler in src/PoNovaWeight.Api/Features/DailyLogs/GetWeightTrends.cs
- [X] T037 [US4] Add trends endpoint to Endpoints.cs in src/PoNovaWeight.Api/Features/DailyLogs/Endpoints.cs
- [X] T038 [P] [US4] Create WeightTrendChart.razor component in src/PoNovaWeight.Client/Components/WeightTrendChart.razor
- [X] T039 [US4] Add WeightTrendChart to Dashboard.razor in src/PoNovaWeight.Client/Pages/Dashboard.razor

**Checkpoint**: User Story 4 complete - users can view weight trends

---

## Phase 7: User Story 5 - Alcohol Correlation Insights (Priority: P3)

**Goal**: Show average weight comparison between alcohol and non-alcohol days

**Independent Test**: Can be tested by logging entries with mixed alcohol consumption patterns and viewing correlation data

### Tests for User Story 5

- [X] T040 [P] [US5] Unit test GetAlcoholCorrelationHandler in tests/PoNovaWeight.Api.Tests/Features/DailyLogs/GetAlcoholCorrelationTests.cs

### Implementation for User Story 5

- [X] T041 [US5] Create GetAlcoholCorrelationQuery and handler in src/PoNovaWeight.Api/Features/DailyLogs/GetAlcoholCorrelation.cs
- [X] T042 [US5] Add alcohol-correlation endpoint to Endpoints.cs in src/PoNovaWeight.Api/Features/DailyLogs/Endpoints.cs
- [X] T043 [P] [US5] Create AlcoholInsights.razor component in src/PoNovaWeight.Client/Components/AlcoholInsights.razor
- [X] T044 [US5] Add AlcoholInsights to Dashboard.razor in src/PoNovaWeight.Client/Pages/Dashboard.razor

**Checkpoint**: User Story 5 complete - users can view alcohol correlation

---

## Phase 8: Delete Functionality (Cross-cutting)

**Goal**: Allow users to delete log entries (FR-015)

- [X] T045 [P] Unit test DeleteDailyLogHandler in tests/PoNovaWeight.Api.Tests/Features/DailyLogs/DeleteDailyLogTests.cs
- [X] T046 Create DeleteDailyLogCommand and handler in src/PoNovaWeight.Api/Features/DailyLogs/DeleteDailyLog.cs
- [X] T047 Add delete endpoint to Endpoints.cs in src/PoNovaWeight.Api/Features/DailyLogs/Endpoints.cs
- [X] T048 Add delete button with confirmation to DayDetail.razor in src/PoNovaWeight.Client/Pages/DayDetail.razor

---

## Phase 9: Integration Tests

**Purpose**: Happy-path integration tests for new endpoints (constitution requirement)

- [X] T049 [P] Integration test for monthly logs endpoint in tests/PoNovaWeight.Api.Tests/Integration/MonthlyLogsEndpointTests.cs
- [X] T050 [P] Integration test for streak endpoint in tests/PoNovaWeight.Api.Tests/Integration/StreakEndpointTests.cs
- [X] T051 [P] Integration test for trends endpoint in tests/PoNovaWeight.Api.Tests/Integration/TrendsEndpointTests.cs
- [X] T052 [P] Integration test for alcohol-correlation endpoint in tests/PoNovaWeight.Api.Tests/Integration/AlcoholCorrelationEndpointTests.cs

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T053 [P] Update .http file with OMAD endpoints in src/PoNovaWeight.Api/http/api.http
- [X] T054 [P] Run dotnet format to ensure code style consistency
- [X] T055 Run full test suite and verify coverage (148 unit tests pass; integration tests require Azurite)
- [X] T056 Quickstart validated - all new endpoints documented and tested

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup ──────────────────┐
                                 │
                                 ▼
Phase 2: Foundational ───────────┤ (BLOCKS all user stories)
                                 │
         ┌───────────────────────┼───────────────────────┐
         ▼                       ▼                       ▼
Phase 3: US1 (P1)          Phase 4: US2 (P2)       Phase 5: US3 (P2)
    MVP 🎯                  Calendar               Streak
         │                       │                       │
         │                       ▼                       │
         │                 Phase 6: US4 (P3)             │
         │                    Trends                     │
         │                       │                       │
         │                       ▼                       │
         │                 Phase 7: US5 (P3)             │
         │                 Correlation                   │
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                                 ▼
                  Phase 8: Delete (Cross-cutting)
                                 │
                                 ▼
                  Phase 9: Polish
```

### User Story Dependencies

| Story | Can Start After | Dependencies |
|-------|-----------------|--------------|
| US1 (P1) | Phase 2 | None - MVP first |
| US2 (P2) | Phase 2 | None - can parallel with US1 |
| US3 (P2) | Phase 2 | None - can parallel with US1 |
| US4 (P3) | Phase 2 | None - can parallel |
| US5 (P3) | Phase 2 | None - can parallel |

### Parallel Opportunities

**Within Phase 1 (Setup)**:
```
T002, T003, T004, T005, T006 can run in parallel (different DTO files)
```

**Within Phase 2 (Foundational)**:
```
T009 can run in parallel with T007-T008 (different files)
```

**Within Each User Story**:
```
Tests (T011, T012) can run in parallel
bUnit tests can run in parallel with API tests
UI components can be developed in parallel with handlers
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (DTOs, entity extension)
2. Complete Phase 2: Foundational (repository, validation, API client)
3. Complete Phase 3: User Story 1 (daily logging with OMAD)
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready - users can now track weight, OMAD, and alcohol!

### Incremental Delivery

| Increment | User Stories | Value Delivered |
|-----------|--------------|-----------------|
| MVP | US1 only | Daily OMAD/weight/alcohol logging |
| + Calendar | US1 + US2 | Visual compliance history |
| + Streak | US1 + US2 + US3 | Motivation via streak counter |
| + Analytics | All | Full insight into habits |

### Estimated Task Distribution

| Phase | Task Count | Parallel Tasks |
|-------|------------|----------------|
| Setup | 6 | 5 |
| Foundational | 4 | 1 |
| US1 (P1) MVP | 9 | 4 |
| US2 (P2) | 7 | 3 |
| US3 (P2) | 6 | 3 |
| US4 (P3) | 6 | 3 |
| US5 (P3) | 5 | 2 |
| Delete | 4 | 1 |
| Integration | 4 | 4 |
| Polish | 4 | 2 |
| **Total** | **55** | **28 (51%)** |

---

## Notes

- All tasks include exact file paths for unambiguous implementation
- [P] tasks can be parallelized across different files
- [Story] labels map tasks to user stories for traceability
- Each user story is independently testable per spec requirements
- TDD: Write tests first, ensure they fail, then implement
- Commit after each task or logical group
- Constitution requires 80% coverage - tests are mandatory
- Validate quickstart.md after implementation complete
