# Tasks: Google OAuth Authentication

**Input**: Design documents from `/specs/002-google-auth/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Per constitution, TDD workflow required for business logic. Tests included.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Paths are absolute from repository root

---

## Phase 1: Setup

**Purpose**: Add dependencies and configure authentication infrastructure

- [X] T001 Add `Microsoft.AspNetCore.Authentication.Google` to Directory.Packages.props
- [X] T002 [P] Add Google OAuth settings to src/PoNovaWeight.Api/appsettings.json (`Google:ClientId`, `Google:ClientSecret`)
- [X] T003 [P] Add Google OAuth settings to src/PoNovaWeight.Api/appsettings.Development.json with placeholder values

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core authentication infrastructure that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 [P] Create UserEntity in src/PoNovaWeight.Api/Infrastructure/TableStorage/UserEntity.cs per data-model.md
- [X] T005 [P] Create IUserRepository interface in src/PoNovaWeight.Api/Infrastructure/TableStorage/IUserRepository.cs
- [X] T006 Create UserRepository in src/PoNovaWeight.Api/Infrastructure/TableStorage/UserRepository.cs (depends on T004, T005)
- [X] T007 [P] Update AuthDtos.cs in src/PoNovaWeight.Shared/DTOs/AuthDtos.cs: add UserInfo and AuthStatus DTOs, remove PasscodeRequest
- [X] T008 Configure Google authentication in src/PoNovaWeight.Api/Program.cs: AddAuthentication with Cookie + Google schemes
- [X] T009 Register UserRepository in DI container in src/PoNovaWeight.Api/Program.cs
- [X] T010 Delete src/PoNovaWeight.Api/Features/Auth/VerifyPasscode.cs (passcode auth removed per FR-010)

**Checkpoint**: Foundation ready — authentication middleware configured, user storage ready

---

## Phase 3: User Story 1 - Sign In with Google (Priority: P1) 🎯 MVP

**Goal**: Users can sign in with their Google account and be authenticated

**Independent Test**: Click "Sign in with Google" → complete Google consent → land on authenticated dashboard

### Tests for User Story 1

- [X] T011 [P] [US1] Unit test for UserRepository.GetOrCreateAsync in tests/PoNovaWeight.Api.Tests/Unit/Auth/UserRepositoryTests.cs
- [X] T012 [P] [US1] Integration test for GET /api/auth/login redirect in tests/PoNovaWeight.Api.Tests/Integration/Auth/AuthEndpointsTests.cs
- [X] T013 [P] [US1] Integration test for GET /api/auth/me (unauthenticated) in tests/PoNovaWeight.Api.Tests/Integration/Auth/AuthEndpointsTests.cs

### Implementation for User Story 1

- [X] T014 [US1] Implement GET /api/auth/login endpoint in src/PoNovaWeight.Api/Features/Auth/Endpoints.cs (initiates Google OAuth challenge)
- [X] T015 [US1] Implement OAuth callback handler to persist user in src/PoNovaWeight.Api/Features/Auth/Endpoints.cs (OnCreatingTicket event)
- [X] T016 [US1] Implement GET /api/auth/me endpoint in src/PoNovaWeight.Api/Features/Auth/Endpoints.cs (returns AuthStatus)
- [X] T017 [US1] Update AuthMiddleware in src/PoNovaWeight.Api/Infrastructure/AuthMiddleware.cs to use User.Identity.IsAuthenticated
- [X] T018 [US1] Add structured logging for sign-in events in src/PoNovaWeight.Api/Features/Auth/Endpoints.cs
- [X] T019 [P] [US1] Create Login.razor page in src/PoNovaWeight.Client/Pages/Login.razor with "Sign in with Google" button
- [X] T020 [P] [US1] Create AuthService in src/PoNovaWeight.Client/Services/AuthService.cs to call /api/auth/me
- [X] T021 [US1] Create AuthenticationStateProvider in src/PoNovaWeight.Client/Services/NovaAuthStateProvider.cs
- [X] T022 [US1] Register auth services in src/PoNovaWeight.Client/Program.cs (AuthenticationStateProvider, AuthService)
- [X] T023 [P] [US1] Add bUnit test for Login.razor in tests/PoNovaWeight.Client.Tests/Components/LoginTests.cs

**Checkpoint**: User Story 1 complete — users can sign in with Google and see authenticated UI

---

## Phase 4: User Story 2 - Persistent Session (Priority: P2)

**Goal**: Returning users remain signed in across browser sessions for 30 days

**Independent Test**: Sign in, close browser, reopen → still authenticated

### Implementation for User Story 2

- [X] T024 [US2] Configure cookie ExpireTimeSpan to 30 days in src/PoNovaWeight.Api/Program.cs
- [X] T025 [US2] Configure cookie SlidingExpiration for session refresh in src/PoNovaWeight.Api/Program.cs
- [X] T026 [US2] Update LastLoginUtc on returning user sign-in in src/PoNovaWeight.Api/Features/Auth/Endpoints.cs

**Checkpoint**: User Story 2 complete — sessions persist across browser restarts

---

## Phase 5: User Story 3 - Sign Out (Priority: P2)

**Goal**: Users can sign out to secure their account on shared devices

**Independent Test**: While authenticated, click "Sign out" → return to login page

### Tests for User Story 3

- [X] T027 [P] [US3] Integration test for POST /api/auth/logout in tests/PoNovaWeight.Api.Tests/Integration/Auth/AuthEndpointsTests.cs

### Implementation for User Story 3

- [X] T028 [US3] Implement POST /api/auth/logout endpoint in src/PoNovaWeight.Api/Features/Auth/Endpoints.cs
- [X] T029 [US3] Add structured logging for sign-out events in src/PoNovaWeight.Api/Features/Auth/Endpoints.cs
- [X] T030 [US3] Add Sign Out button to navigation in src/PoNovaWeight.Client/Shared/MainLayout.razor
- [X] T031 [US3] Add logout method to AuthService in src/PoNovaWeight.Client/Services/AuthService.cs
- [X] T032 [US3] Update NovaAuthStateProvider to handle logout in src/PoNovaWeight.Client/Services/NovaAuthStateProvider.cs

**Checkpoint**: User Story 3 complete — users can sign out

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanup and documentation

- [X] T033 [P] Update src/PoNovaWeight.Api/http/api.http with new auth endpoints (login, logout, me)
- [X] T034 [P] Remove Auth:Passcode from src/PoNovaWeight.Api/appsettings.json
- [X] T035 [P] Add Google auth KQL queries to docs/kql/user-activity.kql (sign-in, sign-out events)
- [X] T036 [P] E2E test for Google auth flow in tests/PoNovaWeight.E2E (Playwright: navigate to login, verify redirect, mock OAuth callback)
- [X] T037 Run dotnet format on solution
- [X] T038 Run dotnet build and resolve any warnings
- [X] T039 Validate against quickstart.md: complete Google OAuth setup and test end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup → No dependencies
Phase 2: Foundational → Depends on Phase 1
Phase 3: User Story 1 (P1) → Depends on Phase 2
Phase 4: User Story 2 (P2) → Depends on Phase 3 (cookie config done in US1)
Phase 5: User Story 3 (P2) → Depends on Phase 3 (sign-in required to test sign-out)
Phase 6: Polish → Depends on all user stories
```

### User Story Independence

| Story | Can Start After | Dependencies on Other Stories |
|-------|-----------------|-------------------------------|
| US1 (Sign In) | Phase 2 | None |
| US2 (Persistent Session) | US1 | Uses cookie config from US1 |
| US3 (Sign Out) | US1 | Requires sign-in to test |

### Parallel Opportunities per Phase

**Phase 1 (Setup):**
```
T002 + T003 can run in parallel
```

**Phase 2 (Foundational):**
```
T004 + T005 + T007 can run in parallel
T006 depends on T004 + T005
T008 + T009 + T010 sequential (same file: Program.cs)
```

**Phase 3 (User Story 1):**
```
Tests: T011 + T012 + T013 can run in parallel
Implementation: T019 + T020 + T023 can run in parallel (different files)
```

**Phase 5 (User Story 3):**
```
T027 can run in parallel with T028
```

**Phase 6 (Polish):**
```
T033 + T034 + T035 can run in parallel
```

---

## Implementation Strategy

### MVP Scope (Recommended)

Complete Phases 1-3 (Setup + Foundational + User Story 1) for a working Google Sign-In MVP.

| MVP Deliverable | Tasks |
|-----------------|-------|
| Google Sign-In | T001-T023 |

### Full Scope

All 38 tasks across 6 phases.

---

## Task Summary

| Phase | Task Count | Parallel Opportunities |
|-------|------------|----------------------|
| Phase 1: Setup | 3 | 2 |
| Phase 2: Foundational | 7 | 3 |
| Phase 3: User Story 1 (P1) | 13 | 7 |
| Phase 4: User Story 2 (P2) | 3 | 0 |
| Phase 5: User Story 3 (P2) | 6 | 2 |
| Phase 6: Polish | 7 | 4 |
| **Total** | **39** | **18** |

---

## Notes

- All tasks include exact file paths from plan.md project structure
- Tests written first per TDD workflow (constitution IV)
- User Story 1 is the MVP — can stop after Phase 3 for working auth
- Passcode auth removal (T010) is in Foundational to avoid confusion during development
- Cookie configuration (US2) is mostly in Program.cs, touched during US1 implementation
- E2E test (T036) uses Playwright with mocked OAuth callback per constitution IV requirement
