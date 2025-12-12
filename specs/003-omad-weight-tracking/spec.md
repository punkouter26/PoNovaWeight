# Feature Specification: OMAD Weight Tracking

**Feature Branch**: `003-omad-weight-tracking`  
**Created**: December 12, 2025  
**Status**: Draft  
**Input**: User description: "Add functionality to add weight, track alcohol consumption, and OMAD compliance for the day - bringing all functionality from PoOmad project"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Daily Health Log Entry (Priority: P1)

As a user tracking my OMAD lifestyle, I want to quickly log my daily health metrics so I can maintain accountability and build healthy habits.

**Why this priority**: This is the core functionality that enables all other features. Without the ability to log daily entries, the application has no value.

**Independent Test**: Can be fully tested by opening the app, entering today's weight, toggling OMAD compliance and alcohol consumption, and saving. Delivers immediate value through daily tracking capability.

**Acceptance Scenarios**:

1. **Given** I am logged in, **When** I navigate to the daily log page (existing nutrients/water page), **Then** I see fields for weight (lbs), OMAD compliance (yes/no), and alcohol consumption (yes/no) alongside existing nutrient and water tracking
2. **Given** I am on the daily log form, **When** I enter a weight of 175.5 lbs, toggle OMAD compliant to "Yes", toggle alcohol to "No", and save, **Then** my entry is persisted and I see confirmation of success
3. **Given** I have already logged today, **When** I open the daily log form, **Then** the form is pre-populated with my previously saved values
4. **Given** I enter a new weight today, **When** the change exceeds 5 lbs from my previous day's weight, **Then** I am prompted to confirm the change before saving

---

### User Story 2 - Visual Calendar View (Priority: P2)

As a user, I want to see my OMAD compliance history on a calendar so I can visualize my consistency and stay motivated.

**Why this priority**: Visual feedback is a key motivator for habit tracking. The calendar provides instant visual feedback on compliance patterns.

**Independent Test**: Can be tested by logging entries for several days, then viewing the calendar to see green (compliant) and red (non-compliant) indicators.

**Acceptance Scenarios**:

1. **Given** I have logged entries for multiple days, **When** I view the monthly calendar, **Then** I see days color-coded (green for OMAD success, red for missed, gray for unlogged)
2. **Given** I am viewing the calendar, **When** I click on a day with a log entry, **Then** I can see the details of that day's log (weight, alcohol status)
3. **Given** I am viewing the current month, **When** I navigate to previous/next month, **Then** the calendar updates to show that month's data

---

### User Story 3 - OMAD Streak Tracking (Priority: P2)

As a user, I want to see my current OMAD streak so I feel motivated to maintain my consistency.

**Why this priority**: Streak tracking leverages "don't break the chain" psychology to reinforce habit formation.

**Independent Test**: Can be tested by logging several consecutive OMAD-compliant days and verifying the streak counter increases.

**Acceptance Scenarios**:

1. **Given** I have logged 5 consecutive OMAD-compliant days, **When** I view my dashboard, **Then** I see a streak counter showing "5 days"
2. **Given** I have a streak of 5 days, **When** I log a non-compliant day, **Then** my streak resets to 0
3. **Given** I have a streak of 5 days, **When** I skip logging for a day (no entry), **Then** my streak does NOT break (only explicit non-compliance breaks streak)

---

### User Story 4 - Weight Trends Analytics (Priority: P3)

As a user, I want to see my weight trends over time so I can understand my progress.

**Why this priority**: Analytics provide insight into long-term progress, but require sufficient data to be meaningful.

**Independent Test**: Can be tested by logging weight for at least 7 days and viewing the trend chart.

**Acceptance Scenarios**:

1. **Given** I have at least 3 days of logged weight data, **When** I view the trends section, **Then** I see a chart showing my weight over time
2. **Given** I have missing days between entries, **When** I view the chart, **Then** gaps are filled with the last known weight (carry-forward)
3. **Given** I have logged data for 30+ days, **When** I view the trends, **Then** I see my total weight change (loss/gain)

---

### User Story 5 - Alcohol Correlation Insights (Priority: P3)

As a user, I want to see if alcohol consumption correlates with my weight changes so I can make informed decisions.

**Why this priority**: Advanced analytics that require substantial data to be statistically meaningful.

**Independent Test**: Can be tested by logging entries with mixed alcohol consumption patterns and viewing correlation data.

**Acceptance Scenarios**:

1. **Given** I have at least 7 days of logged data with some alcohol days and some non-alcohol days, **When** I view the insights, **Then** I see my average weight on alcohol vs non-alcohol days
2. **Given** I have sufficient data, **When** I view the correlation, **Then** I see a clear indicator of whether alcohol consumption is associated with weight gain

---

### Edge Cases

- What happens when the user enters a weight outside the valid range (50-500 lbs)?
  - System shows validation error and prevents saving
- How does the system handle logging for past dates?
  - Users can log entries for past dates, allowing catch-up logging
- What happens if the user tries to log for a future date?
  - System prevents logging for future dates with a validation error
- How does the system handle weight changes greater than 5 lbs?
  - System prompts for confirmation before saving (safety check for data entry errors)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow users to log a daily entry containing weight, OMAD compliance status, and alcohol consumption status
- **FR-002**: System MUST validate weight entries are between 50 and 500 lbs
- **FR-003**: System MUST prevent logging entries for future dates
- **FR-004**: System MUST allow logging entries for past dates (backfill capability)
- **FR-005**: System MUST prompt for confirmation when weight change exceeds 5 lbs from the previous day's entry
- **FR-006**: System MUST pre-populate the daily log form with the previous entry's values if one exists for that date
- **FR-007**: System MUST display a monthly calendar view showing OMAD compliance with color-coded indicators (green for success, red for missed)
- **FR-008**: System MUST calculate and display the current OMAD streak (consecutive compliant days)
- **FR-009**: System MUST NOT break the streak when a day has no logged entry (only explicit non-compliance breaks streak)
- **FR-010**: System MUST display weight trends over time in a chart format
- **FR-011**: System MUST fill gaps in weight data using carry-forward from the last known weight
- **FR-012**: System MUST display alcohol correlation insights showing average weight on alcohol vs non-alcohol days
- **FR-013**: System MUST persist all log entries per user with timestamps
- **FR-014**: System MUST allow users to edit existing log entries for any date
- **FR-015**: System MUST allow users to delete their own log entries

### Key Entities

- **Daily Log Entry**: Captures a single day's health metrics. Key attributes: date, weight (lbs), OMAD compliance (boolean), alcohol consumed (boolean), server timestamp. One entry per user per day.
- **User**: The authenticated user tracking their OMAD journey. Has many Daily Log Entries.
- **Streak**: Calculated value representing consecutive OMAD-compliant days. Not stored, computed on demand from Daily Log Entries.
- **Weight Trend**: Calculated time-series data showing weight over a date range. Includes gap-filling logic.
- **Alcohol Correlation**: Calculated statistics comparing weight on alcohol vs non-alcohol days.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete a daily log entry in under 10 seconds
- **SC-002**: Monthly calendar loads and displays within 2 seconds
- **SC-003**: Streak calculation is accurate and updates immediately after logging
- **SC-004**: 95% of users successfully log their first entry on first attempt
- **SC-005**: Weight trends chart displays correctly with at least 3 data points
- **SC-006**: Data persists across sessions and devices for the same user

## Assumptions

- Users are already authenticated via Google OAuth (handled by existing 002-google-auth feature)
- Weight is tracked in pounds (lbs) - no unit conversion is required
- The application uses Azure Table Storage for persistence (consistent with existing architecture)
- Mobile-first, responsive design is required (consistent with existing client architecture)
- Dark mode is the default theme (consistent with PoOmad design)
- Online-only operation - no offline support required (entries require network connection)

## Clarifications

### Session 2025-12-12

- Q: Can users delete log entries entirely? → A: Yes, users can delete their own entries (full data control)
- Q: Where do users access the daily log form? → A: Combine with existing page containing nutrients and water intake
- Q: Should users be able to log entries when offline? → A: No, online-only (entries require network connection)
- Q: What color for unlogged days on calendar? → A: Gray/neutral to distinguish from success/failure
- Q: Should the system support kilogram (kg) input? → A: No, pounds only (no unit conversion)
