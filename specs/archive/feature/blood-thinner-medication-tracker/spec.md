> **⚠️ ARCHIVED FEATURE**
> This feature has been split into multiple smaller features for better maintainability.
> See `specs/FEATURE-SPLIT-PLAN.md` for the new structure.
> Current active feature: `feature/002-docker-deployment-infrastructure`

---

# Feature Specification: Blood Thinner Medication & INR Tracker

**Feature Branch**: `feature/blood-thinner-medication-tracker`  
**Created**: 2025-10-15  
**Status**: Archived - Split into Features 002-007  
**Input**: User description: "Build an application that can help to remind me to take my blood thinners on time, daily at the same time each day with a 12 hour maximum error window after which it should warn against taking up until next dose is due, log the dosage and remind me do my blood test on time, first thing in the morning on a schedule I can configure, and log the INR level. I should be able to log and the values should be available across my devices. This is not a medically approved app and it should not be considered to be giving any medical advice."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cross-Device Account Setup & Data Sync (Priority: P1)

As a blood thinner patient, I need to create an account and have my medication and INR data synchronized across all my devices so I can access my health information anywhere and never lose my tracking history.

**Why this priority**: Foundation requirement - without account management and data sync, users cannot access their data across devices, making the app less reliable for daily medication management.

**Independent Test**: Can be fully tested by creating an account on one device, logging data, then accessing the same data from a different device. Delivers core value of persistent, accessible health tracking.

**Acceptance Scenarios**:

1. **Given** I am a new user on web, **When** I click "Sign in with Microsoft/Google", **Then** I am redirected to the provider's OAuth2 consent page, authenticate, and return to the app logged in (web redirect flow)
2. **Given** I am a new user on mobile, **When** I tap "Sign in with Microsoft/Google", **Then** the platform-native authentication opens, I authenticate, and the app exchanges the ID token for JWT access tokens (mobile ID token exchange flow - see docs/OAUTH_FLOW_REFERENCE.md)
3. **Given** I have an account, **When** I log medication data on my phone, **Then** I can see the same data when I access the app on my tablet
4. **Given** I am logged in on multiple devices, **When** I update data on one device, **Then** the changes appear on other devices within 30 seconds
5. **Given** time changes due to daylight saving or timezone travel, **When** the system calculates my next medication reminder, **Then** it adjusts to maintain the same local time in my current timezone (see T019a)

---

### User Story 2 - Daily Medication Reminders & Logging (Priority: P2)

As a blood thinner patient, I need to receive daily reminders at my scheduled time to take my medication and log the dosage taken so I can maintain consistent medication adherence and track my compliance.

**Why this priority**: Core safety functionality - consistent medication timing is critical for blood thinner effectiveness and patient safety.

**Independent Test**: Can be fully tested by setting a reminder time, receiving the notification, and logging a dose. Delivers immediate value for medication compliance tracking.

**Acceptance Scenarios**:

1. **Given** I have set my daily medication time to 7:00 PM, **When** it's 7:00 PM, **Then** I receive a persistent notification to take my medication
2. **Given** I receive a medication reminder, **When** I take my medication and enter the dosage, **Then** the reminder is dismissed and the dose is logged with timestamp
3. **Given** I haven't taken my medication within 12 hours of the scheduled time, **When** I try to log a dose, **Then** I see a warning advising against taking the medication until the next scheduled dose

---

### User Story 3 - Configurable INR Test Reminders & Logging (Priority: P3)

As a blood thinner patient, I need to set up a custom schedule for INR blood test reminders and log my INR levels so I can track my blood clotting levels according to my doctor's prescribed testing frequency.

**Why this priority**: Important medical monitoring but frequency varies by patient and condition - less urgent than daily medication but essential for long-term management.

**Independent Test**: Can be tested by configuring an INR schedule, receiving morning reminders on scheduled days, and logging INR values. Delivers value for blood level monitoring and trend tracking.

**Acceptance Scenarios**:

1. **Given** I want to configure my INR testing schedule, **When** I access the settings, **Then** I can set testing frequency (daily, weekly, bi-weekly, monthly, or custom days)
2. **Given** today is a scheduled INR test day, **When** it's morning (between 6-9 AM), **Then** I receive a notification to do my blood test
3. **Given** I complete my INR test, **When** I enter my INR level, **Then** the value is logged with the test date and the reminder is dismissed

---

### User Story 4 - Historical Data Visualization & Trends (Priority: P4)

As a blood thinner patient, I need to view my medication dosage history and INR levels over time in charts so I can track trends, identify patterns, and share comprehensive data with my healthcare provider.

**Why this priority**: Analytical feature that provides insights but not critical for day-to-day medication compliance - valuable for medical consultations and trend analysis.

**Independent Test**: Can be tested by logging several doses and INR readings over time, then viewing the historical charts. Delivers value for long-term health monitoring and medical consultations.

**Acceptance Scenarios**:

1. **Given** I have logged medication doses for several days, **When** I view the medication history, **Then** I see a bar chart showing my dosage over time with selectable time ranges (7, 30, 90 days, or custom)
2. **Given** I have recorded INR levels over several tests, **When** I view the INR history, **Then** I see a line chart showing my INR trends with 7-day moving averages, color-coded indicators, and selectable time ranges
3. **Given** I want to share data with my doctor, **When** I select a date range, **Then** I can export or share a summary of my medication and INR data
4. **Given** I am viewing a chart, **When** I select a different time range, **Then** the chart updates to show data for the selected period

---

### User Story 5 - Missed Dose Recovery & Safety Warnings (Priority: P5)

As a blood thinner patient, I need clear guidance when I miss doses or am outside the safe dosing window so I can make informed decisions about medication timing and avoid dangerous situations.

**Why this priority**: Safety feature that handles edge cases - important for patient safety but less frequent than daily medication management.

**Independent Test**: Can be tested by simulating missed doses and delayed medication attempts. Delivers safety value by preventing dangerous dosing decisions.

**Acceptance Scenarios**:

1. **Given** I missed my scheduled dose by more than 12 hours, **When** I try to log a late dose, **Then** I see a warning recommending I wait for the next scheduled dose
2. **Given** I haven't logged a dose for my scheduled time, **When** the next day's reminder appears, **Then** the app notes the missed dose in my history
3. **Given** I want to take medication outside my normal schedule, **When** I manually try to log a dose, **Then** the app calculates time since last dose and provides appropriate guidance

---

### Edge Cases

- What happens when user changes time zones while traveling? → **Handled by T019a**: Medication schedules maintain local time in current timezone
- How does the system handle daylight saving time transitions? → **Handled by T019a**: DST transitions preserve medication reminder local times
- What occurs when user tries to log multiple doses for the same day? → **To be specified**: Business rule needed for duplicate dose detection
- How does the app behave when device notifications are disabled? → **Handled by T024a**: Fallback UI warnings when notifications disabled
- What happens if user enters an invalid or dangerous INR value (e.g., negative numbers, extremely high values)? → **Handled by T029a**: Validation enforces 0.5-8.0 range, flags outliers <1.5 or >4.5
- How should the app handle very long periods without logging (weeks or months)? → **To be specified**: Re-engagement strategy and data gap handling needed

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST require OAuth2 authentication (via Azure AD or Google) before accessing any medication or INR data. NO password-based authentication permitted. Web applications use OAuth2 redirect flow; mobile applications use platform-native OAuth with ID token exchange. On OAuth provider failure or timeout, system shall display user-friendly error message and allow retry or selection of alternate provider. On user cancellation during OAuth flow, system shall return to login screen without error.
- **FR-002**: System MUST send daily notifications at user-configured times for medication reminders with 99.9% delivery reliability measured over rolling 7-day periods (tracked per T044a notification monitoring). For platforms without native push notification support (Console, Web without service workers), system shall use in-app polling notifications or browser-based alerts as fallback. On mobile platforms, system MUST use OS-level background notification services (APNs for iOS, FCM for Android) to ensure delivery even when app is force-closed or in background.
- **FR-003**: System MUST allow users to configure INR test reminder frequency (daily, weekly, bi-weekly, monthly, custom) and modify existing schedules at any time with changes taking effect on next scheduled reminder.
- **FR-004**: System MUST log medication doses with timestamps and associate with authenticated user. Dosage amounts must be positive decimal numbers with supported units: mg (milligrams), mcg (micrograms), mL (milliliters), or tablets. Users may edit or delete medication log entries within 24 hours of creation to correct data entry errors; audit trail shall preserve original values. System MUST prevent logging more than one dose per day for same medication (single daily dosage constraint).
- **FR-005**: System MUST log INR test results with test dates and associate with authenticated user. Users may edit or delete INR test entries within 24 hours of creation to correct data entry errors; audit trail shall preserve original values.
- **FR-006**: System MUST synchronize user data across multiple devices within 30 seconds under normal network conditions. On network failure, system shall queue changes locally and retry synchronization with exponential backoff (1s, 2s, 4s, 8s intervals) until connection restored. System shall achieve 99% synchronization success rate measured over rolling 7-day periods; on synchronization failures (1% tolerance), system shall display warning notification to user indicating data may not be current on all devices. On synchronization conflicts (same record edited on multiple devices), system shall apply last-write-wins strategy with conflict notification shown to user. System shall support maximum of 10 active registered devices per user account.
- **FR-007**: System MUST display a warning (not a hard block) advising against taking medication more than 12 hours after scheduled time, recommending user wait for next scheduled dose (see T025 for UI implementation). Medication scheduled at exactly 12 hours 0 minutes shall NOT trigger warning (boundary: warning only for >12:00:00).
- **FR-008**: System MUST display historical medication doses in chart format (bar chart preferred) with selectable time ranges: 7 days, 30 days, 90 days, or custom date range. **Blazor Web applications MUST use MudBlazor Chart component for native C# charting without JavaScript dependencies**.
- **FR-009**: System MUST display historical INR levels in chart format (line chart) with trend indicators calculated as 7-day moving averages, with visual color coding (green=stable within range, yellow=rising trend, red=declining trend or outside target range). Selectable time ranges: 7 days, 30 days, 90 days, or custom date range. **Blazor Web applications MUST use MudBlazor Chart component for native C# charting without JavaScript dependencies**.
- **FR-010**: System MUST validate INR value ranges (0.5-8.0) and flag outlier values (INR <1.5 or >4.5) for healthcare provider review (see T029a for validation logic)
- **FR-011**: System MUST track missed doses and display them in medication history
- **FR-012**: System MUST allow users to export medication and INR data for sharing with healthcare providers
- **FR-013**: System MUST display medical disclaimer prominently on every screen showing health data or recommendations (all platforms: Web, Mobile, Console - see T014 for implementation tracking). Disclaimer must appear in header or footer with minimum 12pt font size and high contrast (WCAG AA compliant).
- **FR-014**: System MUST prevent accidental dismissal of medication reminders by requiring explicit confirmation dialog before dismissing without logging dose (see T024). Dialog must include clear options: "Log Dose Now", "Snooze Reminder", "Dismiss" with warning text for dismiss action.
- **FR-015**: System MUST maintain user sessions across app restarts using secure OAuth2 refresh token storage (platform keychains/secure storage: iOS Keychain, Android Keystore, Windows Credential Manager) with AES-256 encryption to minimize login friction (see T017a). Access tokens have 15-minute lifetime and shall be automatically refreshed when remaining lifetime is less than 5 minutes. Session timeout is 7 days of inactivity (no API calls), after which user must re-authenticate. On refresh token expiration or failure, system shall prompt user to re-authenticate while preserving any unsynchronized local data.
- **FR-016**: System MUST handle timezone changes (travel) and daylight saving time transitions by maintaining medication schedules in user's current local timezone (see T019a for DST handling implementation). System shall detect device timezone automatically and notify users of timezone adjustments. All schedules stored in UTC in database, all timestamps displayed in user's current local timezone with consistent formatting across all datetime fields. Medication reminder times MUST be restricted to 03:00-23:30 local time range (midnight 00:00-02:59 not permitted to avoid timezone/DST transition ambiguity - medication recommended for evening administration). All health data stored locally on devices MUST be encrypted at rest using AES-256 encryption (platform-specific: iOS Data Protection, Android EncryptedSharedPreferences, Windows DPAPI).
- **FR-017**: System MUST retain all user health data indefinitely by default. Data older than 1 year shall be flagged for optional archival to separate storage while remaining accessible to users through "View Archive" functionality.
- **FR-018**: System MUST encrypt all network transmissions using TLS 1.2 or higher for API communications between client devices and backend services.
- **FR-019**: System MUST comply with WCAG 2.1 Level AA accessibility standards across all platforms, including but not limited to: sufficient color contrast ratios (4.5:1 for normal text, 3:1 for large text), keyboard navigation support for all interactive elements, and screen reader compatibility with proper ARIA labels and semantic HTML. Medical disclaimer (FR-013) and safety warnings (FR-007, FR-010) must be accessible to screen readers with proper announcement priority.
- **FR-020**: System MUST provide user-friendly error messages for all failure scenarios using plain language without technical jargon. Error messages shall include: clear description of what went wrong, actionable guidance on how to resolve the issue, and contact information for support if user cannot resolve independently.
- **FR-021**: System SHOULD maintain local data backups on each device to enable recovery from accidental data deletion or device failure. Users may export full data backup via FR-012 export functionality for manual restoration if needed. Cloud-based automatic backup is out of scope for MVP.
- **FR-022**: System MUST support multiple authentication methods for different use cases:
  - **End Users**: OAuth2 only (Azure AD, Google) via web redirect flow or mobile ID token exchange (FR-001)
  - **Testing & Integration**: Mutual TLS (mTLS) using X.509 client certificates for:
    - Development and testing tools (Swagger, Postman, automated tests)
    - CI/CD pipelines and integration testing
    - Future healthcare system integrations (HL7/FHIR, EMR systems)
    - Internal service-to-service communication
  - **API Testing**: API MUST provide OAuth2 redirect endpoints (`/api/auth/external/{provider}`, `/api/auth/callback/{provider}`) to support Swagger UI OAuth2 authorization and backend-driven authentication flows for web applications
  - mTLS authentication requires valid client certificate with subject matching registered integration partner; certificate validation includes: not expired, trusted CA chain, certificate revocation check (OCSP). Failed mTLS attempts shall be logged for security monitoring.

### Non-Functional Requirements

- **NFR-001**: Offline mode - System shall queue medication and INR log entries locally when network unavailable, with visual indicator showing offline status and pending sync count. All queued data shall synchronize automatically when connection restored (see FR-006 for sync reliability).
- **NFR-002**: Session management - Access tokens expire after 15 minutes; automatic refresh occurs when <5 minutes remaining. Session timeout after 7 days of inactivity requires re-authentication (see FR-015).
- **NFR-003**: **UI Framework Standards - Blazor Web applications MUST use MudBlazor component library (v8.13.0+) for all user interface components**:
  - **Charts**: MudChart for all data visualizations (line charts, bar charts, pie charts) - NO JavaScript charting libraries
  - **Dialogs**: MudDialog for confirmations, alerts, and modal interactions - NO JavaScript alert/confirm
  - **Notifications**: MudSnackbar for toast notifications - NO custom JavaScript toast libraries
  - **Data Tables**: MudDataGrid for tabular data with sorting/filtering - NO JavaScript data table libraries
  - **Forms**: MudTextField, MudNumericField, MudDatePicker, MudSelect for all form inputs
  - **Icons**: MudIcon with Material Design icons - NO external icon fonts
  - **Theming**: MudThemeProvider for consistent Material Design styling
  - JavaScript interop is ONLY permitted for browser APIs unavailable in .NET (clipboard access, file system dialogs, browser storage). All UI interactions and visualizations MUST be pure C#/Blazor components.
  - **Rationale**: Pure .NET implementation eliminates JavaScript prerendering errors, improves type safety, simplifies maintenance, enables full debugging in C#, and aligns with Blazor philosophy of full-stack .NET development.

### Key Entities *(include if feature involves data)*

- **User Account**: OAuth2-authenticated individual (via Azure AD or Google) with ExternalUserId, AuthProvider (Google/AzureAD), email, device registrations, timezone settings. NO password field - authentication handled entirely through OAuth2 providers.
- **Medication Schedule**: User-specific daily reminder time, dosage amount, timezone-aware scheduling
- **Medication Log** (MedicationLog entity): Recorded dose entries with timestamp, dosage amount, user association, missed dose indicators
- **INR Schedule** (INRSchedule entity): User-configured testing frequency (daily/weekly/biweekly/monthly/custom), next test date calculation
- **INR Test** (INRTest entity - formerly "INR Log"): Recorded INR values with test date, result value (validated 0.5-8.0 range), user association, trend calculations, outlier flags
- **Device Registration**: User's registered devices for cross-platform data synchronization
- **Notification Settings**: User preferences for reminder timing, notification types, snooze options

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can set up their medication schedule and receive their first reminder within 5 minutes of account creation
- **SC-002**: 95% of medication reminders result in successful dose logging within 2 hours of notification
- **SC-003**: Data synchronization completes across devices within 30 seconds of logging new information
- **SC-004**: Users can configure INR testing schedules and see updated reminder dates within 1 minute
- **SC-005**: Historical charts display complete data for users with 7+ days of logged information within 3 seconds
- **SC-006**: Safety warnings for missed doses or late medication attempts appear 100% of the time when conditions are met
- **SC-007**: Users can export their complete medication and INR history in under 10 seconds
- **SC-008**: App maintains 99.9% uptime for critical reminder functionality during business hours
- **SC-009**: New users can complete account setup and log their first medication dose within 3 minutes
- **SC-010**: Medical disclaimer is displayed prominently on every screen that shows health data or recommendations

## Assumptions

- Users have smartphones or tablets capable of receiving push notifications
- Users have reliable internet connectivity for data synchronization (offline functionality not required in MVP)
- Users are literate and capable of reading English text and numbers
- Users understand basic concepts of medication dosing and INR testing from their healthcare provider
- Users will have INR values provided by their healthcare provider or home testing device
- Users take the same medication dosage daily (variable dosing not supported in MVP)
- Standard notification permissions will be granted by users for reminder functionality
