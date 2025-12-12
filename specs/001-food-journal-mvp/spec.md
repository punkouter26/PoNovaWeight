# Feature Specification: PoNovaWeight Food Journal MVP

**Feature Branch**: `001-food-journal-mvp`  
**Created**: 2025-12-10  
**Status**: Draft  
**Input**: User description: "PoNovaWeight mobile-first web app to digitize Nova Physician Wellness Center food journaling with AI-assisted meal scanning and Unit tracking"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manual Daily Unit Logging (Priority: P1)

As a wellness program participant, I want to manually track my daily unit consumption for each food category (Proteins, Vegetables, Fruits, Starches/Carbs, Fats, Dairy) using simple stepper controls, so I can monitor my adherence to the 1500 Calorie Plan without complex calculations.

**Why this priority**: This is the core functionality that replaces the paper log. Without manual logging, the app has no value. Users must be able to record their intake before any other feature matters.

**Independent Test**: Can be fully tested by logging units for each category on a single day and verifying the data persists. Delivers immediate value by digitizing the paper log.

**Acceptance Scenarios**:

1. **Given** I am viewing a day's log entry, **When** I tap the "+" button for Proteins, **Then** the Protein unit count increases by 1 and the display updates immediately
2. **Given** I have logged 15 Protein units today, **When** I tap the "+" button again, **Then** the system allows it (no hard cap) but the progress bar displays in red to indicate over-target
3. **Given** I have logged 3 Vegetable units, **When** I tap the "-" button, **Then** the count decreases to 2 (minimum 0)
4. **Given** I have logged units for the day, **When** I close the app and reopen it, **Then** my logged values persist and display correctly
5. **Given** I am viewing Dairy units (showing "2" dairy units), **When** the system calculates weekly protein equivalents, **Then** Dairy counts as 2 Protein units each (4 total protein equivalent)

---

### User Story 2 - Weekly Dashboard View (Priority: P1)

As a user, I want to see my current week's progress at a glance on a vertically scrolling dashboard with summary progress bars for each day, so I can quickly assess my weekly adherence without tapping into each day.

**Why this priority**: The dashboard is the primary interface users interact with. It provides the "at a glance" view that differentiates this app from the paper log. Tied for P1 because logging needs somewhere to display.

**Independent Test**: Can be tested by viewing a pre-populated week of data and verifying all 7 day cards display with correct progress bars. Delivers immediate value as the main navigation and overview.

**Acceptance Scenarios**:

1. **Given** I open the app, **When** the dashboard loads, **Then** I see 7 day cards in a vertical scrolling list (Sunday at top, Saturday at bottom)
2. **Given** a day has 12/15 Proteins logged, **When** I view that day's card, **Then** the Protein progress bar shows ~80% filled in green
3. **Given** a day has 6/5 Vegetables logged (over target), **When** I view that day's card, **Then** the Vegetable progress bar shows 100% filled in red
4. **Given** I am on the dashboard, **When** I scroll to the bottom, **Then** I see a weekly summary showing total units consumed vs. weekly allowance for each category
5. **Given** I tap on a specific day card, **When** the detail view opens, **Then** I can access the stepper controls for manual unit entry

---

### User Story 3 - Water Intake Tracking (Priority: P2)

As a user, I want to track my daily water intake with a tappable visual tracker (8 segments representing 8 oz each), so I can ensure I meet my 64 oz daily goal with a satisfying interaction that mimics checking off cups on paper.

**Why this priority**: Water tracking is part of the complete wellness log but is secondary to food unit tracking. The app is still useful without it, but it completes the paper log replacement.

**Independent Test**: Can be tested by tapping water segments and verifying visual fill state persists. Delivers value by completing the daily tracking picture.

**Acceptance Scenarios**:

1. **Given** I am viewing a day's log, **When** I see the water tracker, **Then** I see 8 tappable segments (droplets or checkboxes) representing 8 oz each
2. **Given** I have 3 segments filled (24 oz), **When** I tap the 4th segment, **Then** it fills and the total shows 32 oz
3. **Given** all 8 segments are filled (64 oz), **When** I view the water tracker, **Then** a visual indicator shows goal achieved (e.g., green checkmark or "Goal Met" label)
4. **Given** segment 5 is filled, **When** I tap segment 5 again, **Then** it unfills (toggle behavior) and total decreases by 8 oz
5. **Given** I have logged water intake, **When** I return to the dashboard, **Then** the day card shows a water progress indicator

---

### User Story 4 - AI-Assisted Meal Scanning (Priority: P2)

As a user, I want to photograph my meal and have the app analyze it to suggest unit values based on Nova wellness rules, so I can quickly log food without manually calculating units from portion sizes.

**Why this priority**: AI scanning is the "intelligence" differentiator but the app is fully functional without it. Users can always fall back to manual entry. This is a convenience enhancement.

**Independent Test**: Can be tested by photographing a sample meal and verifying AI suggestions appear for review. Delivers value by reducing friction in logging.

**Acceptance Scenarios**:

1. **Given** I am on a day's log entry, **When** I tap "Scan Meal", **Then** the camera interface opens
2. **Given** I take a photo of my meal, **When** the image is captured, **Then** it is compressed/resized on-device before upload (to reduce data usage)
3. **Given** the AI has analyzed my photo of "grilled chicken breast with steamed broccoli", **When** results are ready, **Then** I see a confirmation screen with suggested units (e.g., "Proteins: 6 units, Vegetables: 2 units")
4. **Given** I am on the confirmation screen, **When** I disagree with a suggestion, **Then** I can edit the values using stepper controls before saving
5. **Given** I approve the AI suggestions, **When** I tap "Save", **Then** the units are added to my daily log and I return to the day view
6. **Given** the image has been processed, **When** the data is extracted, **Then** the image is discarded and not stored long-term
7. **Given** I have no internet connection, **When** I tap "Scan Meal", **Then** I see a message indicating the feature requires connectivity and am offered manual entry instead

---

### User Story 5 - Access Control (Priority: P3)

As the app owner, I want the app to require a simple passcode to access, so I can prevent unauthorized public access and protect AI service costs.

**Why this priority**: Security is important but the app's core value is logging. A hardcoded user or simple passcode is sufficient for initial release. Can be enhanced later with proper authentication.

**Independent Test**: Can be tested by attempting to access the app without passcode and verifying access is denied. Delivers value by protecting the app from unauthorized use.

**Acceptance Scenarios**:

1. **Given** I open the app for the first time, **When** the app loads, **Then** I am prompted to enter a passcode before accessing any features
2. **Given** I enter an incorrect passcode, **When** I submit, **Then** I see an error message and cannot access the app
3. **Given** I enter the correct passcode, **When** I submit, **Then** I gain access to the dashboard
4. **Given** I have previously authenticated, **When** I reopen the app within the same session (app was backgrounded, not closed), **Then** I am not prompted for the passcode again
5. **Given** the app/browser has been fully closed, **When** I reopen it, **Then** I must re-enter the passcode

---

### User Story 6 - PWA Installation (Priority: P3)

As a user, I want to install the app to my phone's home screen so it opens without the browser address bar, giving me a native app experience.

**Why this priority**: PWA installation is a user experience enhancement. The app works fully in a browser; installation just improves the experience. Lower priority than core functionality.

**Independent Test**: Can be tested by triggering PWA install prompt and verifying the installed app launches in standalone mode. Delivers value by improving perceived quality.

**Acceptance Scenarios**:

1. **Given** I am using the app in a mobile browser, **When** PWA criteria are met, **Then** I can see an "Add to Home Screen" prompt or banner
2. **Given** I have installed the app, **When** I launch it from my home screen, **Then** the browser address bar is hidden and it feels like a native app
3. **Given** I have installed the app, **When** I use it offline, **Then** previously loaded pages/data remain accessible (graceful degradation)

---

### Edge Cases

- What happens when a user tries to log negative units? → System enforces minimum of 0 for all categories
- What happens when AI analysis fails (timeout, service error)? → User sees friendly error message and is offered manual entry fallback
- What happens if the user logs data for a future date? → System only allows logging for the current week (Sunday-Saturday containing today)
- How does the system handle the Dairy-to-Protein conversion for weekly totals? → Dairy is displayed separately but the weekly summary includes a note showing protein equivalents
- What happens when internet connectivity is lost mid-photo-upload? → Upload fails gracefully with retry option; no partial data saved
- What happens on the first day of a new week? → Dashboard automatically shifts to show the new current week

## Requirements *(mandatory)*

### Functional Requirements

**Unit Tracking System**

- **FR-001**: System MUST track six distinct unit categories: Proteins (target 15/day), Vegetables (target 5/day), Fruits (target 2/day), Starches/Carbs (target 2/day), Fats (target 4/day), and Dairy (max 3/day)
- **FR-002**: System MUST display Dairy as a separate category in the UI while mathematically treating 1 Dairy unit as equivalent to 2 Protein units for conversion purposes
- **FR-003**: System MUST allow unit values from 0 to unlimited (no upper cap) for all categories
- **FR-004**: System MUST persist logged units per day with one log entry per day granularity

**Dashboard & Navigation**

- **FR-005**: System MUST display a weekly view showing Sunday through Saturday with the current week (containing today)
- **FR-006**: System MUST display progress bars for each category on day cards showing percentage of target achieved
- **FR-007**: System MUST color progress bars green when at or below target, red when exceeding target
- **FR-008**: System MUST provide a weekly summary aggregating total units consumed vs. weekly allowances
- **FR-009**: System MUST allow users to tap a day card to access detailed logging for that day

**Water Tracking**

- **FR-010**: System MUST provide a visual water tracker with 8 tappable segments (8 oz each, totaling 64 oz goal)
- **FR-011**: System MUST support toggle behavior on water segments (tap to fill, tap again to unfill)
- **FR-012**: System MUST display a goal-achieved indicator when all 8 water segments are filled

**Unit Entry**

- **FR-013**: System MUST provide stepper controls (+/- buttons) for each unit category
- **FR-014**: System MUST update displayed values immediately upon stepper interaction
- **FR-015**: System MUST prevent unit counts from going below zero

**AI Meal Analysis**

- **FR-016**: System MUST provide a "Scan Meal" option to capture food photos
- **FR-017**: System MUST compress/resize images on-device before uploading for analysis
- **FR-018**: System MUST analyze images and return unit suggestions mapped to Nova wellness rules
- **FR-019**: System MUST present AI suggestions on a confirmation screen for user review before saving
- **FR-020**: System MUST allow users to edit AI-suggested values before saving
- **FR-021**: System MUST discard images after data extraction (no long-term image storage)
- **FR-022**: System MUST display an appropriate message when AI features are unavailable (offline) and offer manual entry

**Security**

- **FR-023**: System MUST require passcode entry before granting access to app features
- **FR-024**: System MUST deny access when an incorrect passcode is entered
- **FR-025**: System MUST maintain session state to avoid repeated passcode prompts within a session

**PWA Capabilities**

- **FR-026**: System MUST meet PWA installability criteria (manifest, service worker, HTTPS)
- **FR-027**: System MUST launch in standalone mode (no browser chrome) when installed
- **FR-028**: System MUST cache previously loaded data for graceful offline access

### Key Entities

- **DailyLog**: Represents a single day's tracking data; contains date, unit counts for all six categories, and water intake (0-8 segments). One entry per day per user.
- **UnitCategory**: Represents a trackable food category; has name, daily target, display order, and color coding rules. Six predefined categories.
- **WeeklySummary**: Derived/calculated entity aggregating seven DailyLogs; shows totals vs. weekly targets, includes Dairy-to-Protein conversion math.
- **MealScan**: Transient entity for AI analysis workflow; contains captured image (temporary), extracted unit suggestions, and user-confirmed values. Not persisted after saving.
- **UserSession**: Represents authenticated access; tracks passcode verification state and session expiry.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can log a complete meal (all relevant categories) in under 30 seconds using manual stepper controls
- **SC-002**: Users can assess their weekly progress from the dashboard within 5 seconds without tapping into individual days
- **SC-003**: AI meal scanning returns unit suggestions within 10 seconds of photo capture for 95% of requests
- **SC-004**: 90% of AI-suggested unit values require no user correction (accuracy threshold)
- **SC-005**: App loads and displays the dashboard in under 3 seconds on a 4G mobile connection
- **SC-006**: 100% of user logging sessions persist data correctly across app restarts
- **SC-007**: PWA-installed users can view their last week's data without connectivity
- **SC-008**: Water tracking goal completion rate increases 20% compared to paper log baseline (user-reported)
- **SC-009**: Users rate the app experience 4+ stars (out of 5) for ease of use in post-launch survey

## Assumptions

- Users are existing Nova Physician Wellness Center participants familiar with the unit system from paper logs
- The default daily targets (Proteins: 15, Vegetables: 5, Fruits: 2, Starches: 2, Fats: 4, Dairy: max 3) are correct per the 1500 Calorie Plan and do not need per-user customization in MVP
- A single shared passcode is acceptable for MVP security (no individual user accounts)
- Users will have smartphones with cameras for AI meal scanning feature
- AI service costs are acceptable given the expected user base (passcode restricts access)
- Users primarily use the app in portrait orientation on mobile devices
- Week boundaries follow Sunday-Saturday convention
- AI accuracy (SC-004) will be measured server-side by comparing suggestions to final saved values; no in-app feedback UI required for MVP

## Clarifications

### Session 2025-12-10

- Q: What constitutes "extended period" for session expiry requiring passcode re-entry? → A: Session expires when browser/app is fully closed (not just backgrounded)
- Q: Should there be AI feedback mechanism for low-confidence or disputed suggestions? → A: No feedback mechanism needed for MVP; users edit via steppers, accuracy measured server-side
