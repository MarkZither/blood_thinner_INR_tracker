# Feature Specification: Mobile: Splash screen, Login, Read-only recent INR

**Feature Branch**: `010-title-mobile-splash`  
**Created**: 2025-11-21  
**Status**: Draft  
**Input**: User description: "Create the mobile app functionality to display a splash screen with a pulsing logo, allow the user to login and get the read only recent INR values from the API."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - First-run launch and login (Priority: P1)

As a registered user, I open the mobile app and see a polished splash screen while the app initializes; I sign in and immediately see my most recent INR results in a simple, read-only list.

**Why this priority**: This is the core user journey that delivers immediate value — getting patients access to their latest INR values quickly and reliably after launching the app.

**Independent Test**: Install the mobile app on a device or emulator, launch it, complete login, and verify the recent INR list is visible and populated.

**Acceptance Scenarios**:

1. **Given** the app is installed and the device has network connectivity, **When** the user opens the app, **Then** the splash screen with pulsing logo is shown and the app transitions to the login screen or main screen when ready.
2. **Given** the user provides valid credentials, **When** they submit the login form, **Then** the app authenticates the user and displays the read-only list of the most recent INR results.
  - **Exchange detail**: The client must obtain an `id_token` from the external provider and successfully exchange it with the backend for an internal bearer token. Acceptance evidence MUST include: the OIDC callback containing the `id_token` (or proof of id_token receipt), a successful `POST /auth/exchange` response from the backend, and a subsequent authenticated API call using the returned internal bearer token that returns INR data.
3. **Given** the user provides invalid credentials, **When** they submit the login form, **Then** the app shows a clear, actionable error message.

---

### User Story 2 - View recent INR values (Priority: P1)

As a signed-in user, I can view my recent INR test results (value and date) in a concise, chronological list so I can check trends at a glance.

**Why this priority**: Core value — allows patients to quickly check INR readings without editing or submitting data.

**Independent Test**: After signing in, verify the app calls the API, shows a loading state, then renders the most recent N INR entries with date and value. Each item is non-editable.

**Acceptance Scenarios**:

1. **Given** the user is authenticated and the API returns data, **When** the INR list is fetched, **Then** the most recent 5 results are shown sorted newest-first, each with date (localised) and numeric INR value.
2. **Given** the API returns an empty list, **When** the list is rendered, **Then** the app displays a friendly empty state explaining "No recent INR results found."

---

### User Story 3 - Accessibility & motion preferences (Priority: P2)

As a user with motion sensitivity, I want the pulsing animation to be disabled when my device accessibility setting requests reduced motion so the app is comfortable to use.

**Why this priority**: Accessibility requirement ensures the app is inclusive and meets basic usability standards.

**Independent Test**: Enable the OS-level "reduce motion" accessibility setting and verify the splash animation is disabled.

**Acceptance Scenarios**:

1. **Given** the device accessibility setting requests reduced motion, **When** the app launches, **Then** the logo is presented without pulsing animation.

---

### Edge Cases

- App launch without network connectivity: app must show splash, then present login screen with offline notice and (if applicable) cached INR data or a clear message that data is unavailable.
- API returns malformed or partial data: app must handle gracefully, log the error locally, and show a user-friendly message.
- Slow network: app shows progress indicators and must allow the user to retry fetches.
- Token/credential expiry during fetch: app must surface an authentication error and allow re-login.
- Device language/timezone differences: dates displayed should be localised and unambiguous.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: App MUST display a splash screen on startup showing the application logo centered on screen with a pulsing animation, unless the user or device accessibility setting requests reduced motion.
- **FR-002**: App MUST present a login screen where the user can enter credentials and authenticate to the backend service.  
  - **FR-002a**: Authentication method: OAuth 2.0 with external identity provider(s) (e.g., Google, Azure AD). The app MUST support sign-in with configured identity providers and clearly surface when external consent or browser-based flows are required.
   - **FR-002b**: Mobile authentication flow and token exchange: The mobile client will perform an OIDC / Authorization Code + PKCE flow against the configured external provider (Azure or Google). The provider returns an `id_token` to the client via the OIDC callback. The mobile client MUST POST the received `id_token` to a backend token-exchange endpoint (for example `POST /auth/exchange`) to obtain an internal bearer token suitable for API authorization. The backend MUST validate the `id_token` (issuer, audience, signature, expiry) and may apply additional checks (scopes, account status) before issuing the internal bearer token. The mobile client MUST store the internal bearer token securely (platform `SecureStorage`) and use it for API requests in the `Authorization: Bearer <token>` header.
   
     Additional token lifecycle requirements (must be specified before implementation):
     - The backend-issued internal bearer token SHOULD be short-lived (recommended TTL: 15 minutes) and opaque to the client. The backend MAY also issue a refresh mechanism (refresh token or re-exchange flow) for long-lived sessions; if a refresh token is used it MUST be stored securely and be revocable server-side.
     - The mobile client MUST handle token expiry by attempting a silent refresh (re-exchange or refresh token use). If refresh fails, the app MUST surface a clear re-authentication UX that preserves the user's context where possible.
     - Error cases and invalid tokens MUST be logged (local telemetry) and surfaced as actionable messages (see FR-004/FR-008). The `auth-exchange.md` contract MUST document expected response codes, error fields, and test cases for invalid/expired `id_token`.
- **FR-003**: After successful authentication, app MUST fetch the user's most recent INR test results (read-only) from the API and display them in a list view.
  - **FR-003a**: By default the list SHOULD show the most recent 5 INR results; include date and numeric value for each item.
- **FR-004**: App MUST show loading states when fetching data and user-friendly error states when fetch or authentication fails.
- **FR-005**: App MUST respect device accessibility settings for reduced motion; if reduced motion is requested, the pulsing animation MUST be disabled and replaced with a static logo presentation.
 - **FR-006**: App SHOULD cache the last-fetched read-only INR results for up to 7 days to allow offline viewing. The app MUST display a warning if the cached data is older than 1 hour indicating the results may be stale. The cached data MUST be encrypted at rest using AES-256 and stored using the platform's secure storage mechanisms (e.g., iOS Keychain, Android Keystore). Key management MUST follow platform best practices and avoid storing raw keys in plain storage.
   
   Key management & encryption guidance (implementation MUST follow these guidelines):
   - Use an authenticated encryption mode (recommended: `AesGcm`) with associated authenticated data (AAD) that includes a version and device identifier to bind payloads to the device.
   - Derive per-device encryption keys using a platform-protected root (e.g., Keychain/Keystore) combined with a KDF such as HKDF or PBKDF2 when deriving keys from secrets; do NOT store raw AES keys in plain text files.
   - Include a key-rotation strategy: the app MUST support rotating encryption keys and re-encrypting cached payloads when the app detects key rotation. Provide a migration path in the `CacheService` to handle older key versions.
   - Unit tests MUST cover encrypt/decrypt roundtrips, tamper-detection (invalid/corrupted payloads), and rotation/migration scenarios.
- **FR-007**: App MUST not allow modification of INR data via this feature (read-only view only).
- **FR-008**: App MUST present clear, actionable messages for authentication failures, network errors, and empty results.

### Key Entities *(include if feature involves data)*

- **INRTest**: Represents a recorded INR measurement. Key attributes (business-level):
  - `Date` (timestamp of test)
  - `Value` (numeric INR)
  - `Source` (e.g., lab or clinic identifier, optional)
  - `Notes` (optional short text)
- **UserSession**: Represents the authenticated user context (business-level); used to authorize API requests. Do not include implementation details such as token format in this spec.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: New users see the splash screen and reach the login or main screen within 3 seconds on a typical mobile network (4G/modern wifi).
- **SC-002**: Authenticated users see the most recent 5 INR results on the main screen within 10 seconds of submitting credentials under a normal mobile network.
 - **SC-003**: When the API returns data, 95% of valid requests render the INR list without user-visible errors in a standard acceptance test run. The test harness and acceptance run MUST be defined: e.g., a CI acceptance job that performs N (recommended 100) authenticated fetch+render sequences against a staging endpoint with controlled latency and network profile, and records render success rate. If 95% threshold is required, the CI job MUST publish the measured success rate and fail the gate if below threshold.
- **SC-004**: The splash animation is disabled when the device indicates reduced motion; automated accessibility check verifies animation suppression.
- **SC-005**: The feature includes clear fallback states: empty data, offline/no-network, and authentication failure; user-facing messages are verified in acceptance tests.

## Assumptions

- Default number of recent INR values to display is 5 unless product owner requests a different count.
- The API exposes an endpoint that returns recent INR tests for an authenticated user; authentication is required to access personal INR data.
- The mobile client must present only read-only data in this feature; editing or uploading INR results is out of scope.
- Local caching policy is optional and subject to the clarification requested above.


## Open Questions / Clarifications (max 3)

1. **Authentication method** (see FR-002a): Chosen option: **B — OAuth with external provider(s)**.

2. **Offline caching requirement** (see FR-006): Chosen option: Cache for 7 days with an in-app warning displayed when cached data is older than 1 hour. The warning should read: "Results may be older than 1 hour — connect to the network to refresh." 

---

## Clarifications

### Session 2025-11-21

- Q: Which authentication method should the mobile app use? → A: OAuth with external provider(s) (B). Rationale: aligns with project guidance for OAuth 2.0, provides stronger security for health data, and supports SSO across platforms.

- Q: Should cached INR data be encrypted at rest? → A: Yes. Use AES-256 and platform secure storage (Keychain/Keystore). Rationale: medical data requires AES-256 encryption per project requirements and reduces compliance risk.

