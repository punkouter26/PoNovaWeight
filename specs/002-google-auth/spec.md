# Feature Specification: Google OAuth Authentication

**Feature Branch**: `002-google-auth`  
**Created**: 2025-12-11  
**Status**: Draft  
**Input**: User description: "Create auth for google user so that they can auth through their google email. Do this in the least complex way possible."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sign In with Google (Priority: P1)

As a user, I want to sign in to PoNovaWeight using my Google account so that I don't need to remember another password and can quickly access the app.

**Why this priority**: This is the core authentication flow. Without it, no other auth-related feature works. It delivers the primary value of passwordless, familiar login.

**Independent Test**: Can be fully tested by clicking "Sign in with Google", completing Google's consent flow, and verifying the user lands on the authenticated home page.

**Acceptance Scenarios**:

1. **Given** an unauthenticated user on the login page, **When** they click "Sign in with Google", **Then** they are redirected to Google's OAuth consent screen
2. **Given** a user on Google's consent screen, **When** they approve access, **Then** they are redirected back to PoNovaWeight and authenticated with a session
3. **Given** a user on Google's consent screen, **When** they deny access or cancel, **Then** they are redirected back to PoNovaWeight with an appropriate message

---

### User Story 2 - Persistent Session (Priority: P2)

As a returning user, I want to remain signed in across browser sessions so that I don't have to re-authenticate every time I visit the app.

**Why this priority**: Improves user experience by reducing friction on return visits. Depends on US1 being complete.

**Independent Test**: Sign in once, close the browser, reopen and navigate to the app—user should still be authenticated.

**Acceptance Scenarios**:

1. **Given** an authenticated user, **When** they close the browser and return within 30 days, **Then** they remain authenticated
2. **Given** an authenticated user whose session has expired (>30 days), **When** they visit the app, **Then** they are prompted to sign in again

---

### User Story 3 - Sign Out (Priority: P2)

As a user, I want to sign out of PoNovaWeight so that I can secure my account on shared devices.

**Why this priority**: Essential for security and user control. Depends on US1.

**Independent Test**: While authenticated, click "Sign out" and verify the user is returned to unauthenticated state.

**Acceptance Scenarios**:

1. **Given** an authenticated user, **When** they click "Sign out", **Then** their session is cleared and they see the login page
2. **Given** a signed-out user, **When** they try to access protected pages, **Then** they are redirected to the login page

---

### Edge Cases

- What happens when Google's OAuth service is temporarily unavailable?
  - Show a user-friendly error message and allow retry
- What happens when a user's Google account is deleted/suspended after initial auth?
  - Allow session to continue until expiry, next auth attempt will fail gracefully
- What happens when the browser blocks cookies?
  - Display a message explaining that cookies are required for authentication

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support "Sign in with Google" using OAuth 2.0 / OpenID Connect
- **FR-002**: System MUST use ASP.NET Core's built-in Google authentication provider for simplicity
- **FR-003**: System MUST maintain the existing session cookie approach (`nova-session`) for authenticated state
- **FR-004**: System MUST store Google OAuth credentials (Client ID, Client Secret) in appsettings.json for development and Azure Key Vault for production
- **FR-005**: System MUST support the existing public path exemptions (health, swagger, static files)
- **FR-006**: System MUST redirect unauthenticated users to a login page rather than returning 401 for browser requests
- **FR-007**: System MUST provide a sign-out endpoint that clears the session
- **FR-008**: System MUST extract and store the user's email and display name from the Google token for personalization
- **FR-009**: System MUST log all authentication events (sign-in, sign-out, failures) using structured logging
- **FR-010**: System MUST remove the existing passcode-based authentication (replaced by Google OAuth)
- **FR-011**: System MUST persist user profile data (email, display name, picture URL, login timestamps) to Azure Table Storage on sign-in

### Non-Functional Requirements

- **NFR-001**: Authentication round-trip (redirect to Google and back) should complete in under 5 seconds on average network conditions
- **NFR-002**: Session cookies must use HttpOnly, Secure, and SameSite=Strict for security

### Key Entities

- **User Session**: Represents an authenticated session; includes user email (primary identifier), display name, session token, and expiry timestamp
- **OAuth State**: Temporary state token used during OAuth flow to prevent CSRF attacks (handled by ASP.NET Core)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete Google sign-in in under 30 seconds (including Google consent flow)
- **SC-002**: 95% of authentication attempts succeed on first try (excluding user cancellations)
- **SC-003**: Returning users within 30 days are automatically authenticated without re-entering credentials
- **SC-004**: Zero authentication-related support tickets in first month of deployment (excluding Google account issues)

## Clarifications

### Session 2025-12-11

- Q: Should user data be keyed by email address or Google's unique user ID (`sub` claim)? → A: Email address
- Q: What should happen to the existing passcode authentication? → A: Remove entirely (Google-only)
- Q: Should any Google account be able to sign in, or only whitelisted/domain-restricted accounts? → A: Any valid Google account (open registration)
- Q: Should Azure Entra ID be included as a second authentication provider? → A: No, keep Google-only for simplicity; Entra is future enhancement

## Assumptions

- The application will use a single Google Cloud project for OAuth credentials
- Only email scope is required (no access to Google Drive, Calendar, etc.)
- User data will be keyed by the user's email address (primary identifier)
- Existing passcode authentication will be removed entirely; Google OAuth is the sole authentication method
- Any valid Google account can sign in (open registration, no whitelist or domain restriction)

## Out of Scope

- Multi-provider authentication (Microsoft, Apple, etc.)—future enhancement
- Role-based access control—all authenticated users have equal access
- Account linking with existing passcode-authenticated sessions
- Self-service account deletion
