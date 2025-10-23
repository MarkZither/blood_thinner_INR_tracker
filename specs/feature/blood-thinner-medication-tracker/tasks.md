# Tasks: Blood Thinner Medication & INR Tracker

**Feature**: Blood Thinner Medication & INR Tracker
**Branch**: feature/blood-thinner-medication-tracker
**Date**: 2025-10-15

---

## Phase 1: Setup

- [x] T001 Create multi-platform .NET 10 solution structure with global.json and Directory.Build.props per plan.md
- [x] T002 [P] Initialize Git repository and configure .editorconfig, StyleCop, and Roslyn analyzers
- [ ] T003 [P] Add .NET Aspire orchestration projects (AppHost, ServiceDefaults) in src/ <!-- INCOMPLETE: Projects exist but are placeholders without Aspire.Hosting SDK, service discovery, dashboard, or OpenTelemetry integration -->
  - [ ] T003a Add Aspire.Hosting SDK to AppHost project and Aspire.Hosting.AppHost package
  - [ ] T003b Configure service discovery for Api, Web, and Mcp projects in AppHost/Program.cs
  - [ ] T003c Add OpenTelemetry integration through Aspire (enables automatic instrumentation for T013)
  - [ ] T003d Add Aspire Dashboard for local development monitoring
  - [ ] T003e Configure ServiceDefaults to use AddServiceDefaults() extension with health checks and resilience
- [x] T004 [P] Create src/, tests/, docs/, samples/, tools/ folder structure per David Fowler conventions
- [ ] T005 [P] Add README.md and copy quickstart.md to repo root <!-- README exists but minimal -->
- [x] T006 [P] Add .gitignore and basic CI/CD pipeline config in .github/workflows/

## Phase 2: Foundational

- [x] T007 Add Entity Framework Core and configure multi-provider (SQLite, PostgreSQL) in src/BloodThinnerTracker.Api/
- [x] T008 [P] Implement User, Medication, INR, Device, Preferences, AuditLog, SyncMetadata entities in src/BloodThinnerTracker.Shared/Models/
- [x] T009 [P] Add initial database migrations and apply to local SQLite and PostgreSQL
- [ ] T010 [P] Implement authentication abstraction and OAuth2 integration (Azure AD, Google) in src/BloodThinnerTracker.Api/ <!-- INCOMPLETE: OAuth2 middleware configured but not wired up. Currently accepts any email/password and returns JWT without authentication. See docs/OAUTH_GAP_ANALYSIS.md -->
  - [x] T010a Add OAuth2 middleware configuration (Google, Azure AD) in AuthenticationExtensions.cs
  - [ ] T010b **MERGED WITH T015b** - OAuth2 initiation endpoints moved to T015b to avoid duplication
  - [ ] T010c **MERGED WITH T015c** - OAuth2 callback handlers moved to T015c to avoid duplication
  - [ ] T010d Implement ID token validation service for mobile OAuth flows (shared service used by T015d)
  - [ ] T010e Create ExternalLoginRequest model with Provider and IdToken fields (remove password-based LoginRequest - see docs/OAUTH_GAP_ANALYSIS.md)
  - [ ] T010f Update AuthenticationService.AuthenticateExternalAsync() with real Google/Azure AD token validation using Microsoft.Identity.Web and Google.Apis.Auth
  - [ ] T010g Add database migration for User entity: Add ExternalUserId (string), AuthProvider (enum: Google/AzureAD) fields, remove any password-related fields
- [ ] T011 [P] Add JWT token issuance and validation middleware in src/BloodThinnerTracker.Api/ <!-- INCOMPLETE: JWT middleware works but issues tokens for fake/unauthenticated users. Must connect to real OAuth2 flow. -->
  - [x] T011a Implement JwtTokenService.GenerateAccessToken() and GenerateRefreshToken()
  - [x] T011b Add JWT Bearer authentication middleware in Program.cs
  - [ ] T011c Connect JWT generation to OAuth2-authenticated users (not placeholder users) - Remove fake user creation from AuthenticationService.AuthenticateAsync()
  - [ ] T011d Add refresh token persistence: Create RefreshToken entity with fields (Token, UserId, ExpiresAt, CreatedAt, RevokedAt), add to DbContext, create migration
  - [ ] T011e Implement token revocation endpoint (POST /api/auth/revoke) with refresh token invalidation in database
- [x] T012 [P] Add SignalR hub for real-time sync in src/BloodThinnerTracker.Api/
- [ ] T013 [P] Add OpenTelemetry and health checks to all services <!-- BLOCKED: Requires T003a-e to be completed first. Aspire provides automatic OpenTelemetry instrumentation and health check infrastructure. See docs/ASPIRE_IMPLEMENTATION.md -->
  - [ ] T013a **BLOCKED BY T003c** - Use Aspire's automatic OpenTelemetry integration (requires T003c completion)
  - [ ] T013b Add custom health check endpoints for database connectivity, SignalR hub status, and notification service availability
  - [ ] T013c Add health check UI component in Aspire Dashboard (enabled by T003d)
- [ ] T014 [P] Add medical disclaimer component to all UI projects (Mobile, Web, Console) and verify display on every data screen with E2E tests in tests/BloodThinnerTracker.Integration.Tests/ <!-- Partial: Web done, Mobile/Console/E2E tests need verification -->
  - [x] T014a Implement medical disclaimer component in BloodThinnerTracker.Web
  - [ ] T014b Implement medical disclaimer component in BloodThinnerTracker.Mobile (MAUI)
  - [ ] T014c Implement medical disclaimer display in BloodThinnerTracker.Cli (Console header)
  ` [ ] T014d Add E2E tests verifying disclaimer displays on all health data screens (medication log, INR charts, reports)

## Phase 3: [US1] Cross-Device Account Setup & Data Sync

- [ ] T015 [US1] Implement user registration and login endpoints in src/BloodThinnerTracker.Api/Controllers/AuthController.cs <!-- INCOMPLETE: Placeholder /login endpoint exists but accepts any email/password. Need OAuth2 endpoints instead. See docs/OAUTH_GAP_ANALYSIS.md -->
  - [ ] T015a Remove password-based /api/auth/login endpoint and LoginRequest model
  - [ ] T015b Add OAuth2 web flow initiation endpoint (GET /api/auth/external/{provider}) - redirects to Google/Azure AD consent page (MERGED from T010b)
  - [ ] T015c Add OAuth2 callback handler (GET /api/auth/callback/{provider}) - exchanges authorization code for tokens (MERGED from T010c)
  - [ ] T015d Add OAuth2 mobile endpoint for ID token exchange (POST /api/auth/external/mobile) - validates ID token from platform-native auth using T010d validation service
  - [ ] T015e Implement automatic user creation on first OAuth login: Check if ExternalUserId exists, create new User if not, update LastLoginAt
  - [ ] T015f **COMPLETED BY T010g** - ExternalUserId field added via User entity migration in T010g
- [ ] T016 [P] [US1] Implement device registration and sync endpoints in src/BloodThinnerTracker.Api/Controllers/DeviceController.cs <!-- DeviceController not found - CREATE NEW -->
  - [ ] T016a Create DeviceController with endpoints: POST /api/devices/register, GET /api/devices, DELETE /api/devices/{id}
  - [ ] T016b Implement device fingerprinting logic (platform, OS version, app version, device ID)
  - [ ] T016c Add device trust verification for sensitive operations
- [x] T017 [P] [US1] Implement user session management and refresh tokens in src/BloodThinnerTracker.Api/Services/
  - [ ] T017a [P] [US1] Implement persistent session storage and automatic token refresh across app restarts in src/BloodThinnerTracker.Shared/Services/SessionService.cs (Uses refresh tokens from T011d)
- [ ] T018 [P] [US1] Implement user account creation UI in src/BloodThinnerTracker.Mobile/Pages/ and src/BloodThinnerTracker.Web/Pages/ <!-- INCOMPLETE: UI shells exist (~30% done) but no API integration, no authentication state management, broken navigation routes. See docs/BLAZOR_WEB_ISSUES.md -->
  - [x] T018a Fix navigation route mismatches in MainLayout.razor (/inr-tracking → /inr, /medication-log → /medications)
  - [ ] T018b Create missing report pages (/reports/inr-trends, /reports/medication-adherence, /reports/export)
  - [ ] T018c Implement CustomAuthenticationStateProvider with JWT token management in src/BloodThinnerTracker.Web/Services/
  - [ ] T018d Connect Dashboard to API endpoints (GET /api/users/profile, GET /api/medications, GET /api/inr)
  - [ ] T018e Connect INRTracking page to API (GET /api/inr, POST /api/inr) 
  - [ ] T018f Connect Medications page to API (GET /api/medications, POST /api/medications)
  - [ ] T018g Implement logout functionality and redirect to login
  - [ ] T018h Create Help/Support page at /help
  - [ ] T018i Add [Authorize] attributes and protected route handling
  - [ ] T018j Implement user profile dropdown functionality (load real user data from GET /api/users/profile)
  - [ ] T018k Add HttpClient configuration with authentication interceptor in Program.cs (adds JWT bearer token to all API requests)
  - [ ] T018l Add secure token storage service (ISecureStorageService implementation for Web using browser localStorage with encryption)
  - [ ] T018m Add missing business rule: Duplicate dose detection logic to prevent logging multiple doses for same medication on same day
- [x] T019 [P] [US1] Implement cross-device data sync logic in src/BloodThinnerTracker.Shared/Services/SyncService.cs
- [ ] T019a [P] [US1] Implement timezone-aware scheduling with DST transition handling and travel scenario support in src/BloodThinnerTracker.Api/Services/SchedulingService.cs
  - [ ] T019a-i Store medication schedules in UTC, convert to user's current timezone for display
  - [ ] T019a-ii Handle DST "spring forward" (2am→3am): Adjust 2:30am medication to 3:30am to preserve intent
  - [ ] T019a-iii Handle DST "fall back" (2am→1am): Prevent duplicate 1:30am reminders
  - [ ] T019a-iv Detect timezone changes via device location/settings, update user timezone in database
  - [ ] T019a-v Add UI warning when user travels across timezones: "Your medication time has been adjusted to 7:00 PM local time"
- [ ] T020 [P] [US1] Add integration tests for account creation and sync in tests/BloodThinnerTracker.Integration.Tests/

## Phase 4: [US2] Daily Medication Reminders & Logging

- [x] T021 [US2] Implement medication scheduling endpoints in src/BloodThinnerTracker.Api/Controllers/MedicationController.cs
- [x] T022 [P] [US2] Implement medication reminder notification service in src/BloodThinnerTracker.Api/Services/NotificationService.cs
- [x] T023 [P] [US2] Implement medication logging endpoints in src/BloodThinnerTracker.Api/Controllers/MedicationLogController.cs
- [ ] T024 [P] [US2] Implement medication reminder UI with accidental dismissal protection (confirmation required) in src/BloodThinnerTracker.Mobile/Pages/ and src/BloodThinnerTracker.Web/Pages/
  - [ ] T024-i Show confirmation dialog when user dismisses notification: "Are you sure you want to dismiss without logging your dose?"
  - [ ] T024-ii Provide "Snooze 15 minutes" option in addition to "Log Dose" and "Dismiss"
- [ ] T024a [P] [US2] Implement notification permission checks and fallback UI warnings in src/BloodThinnerTracker.Mobile/Services/ and src/BloodThinnerTracker.Web/Components/
  - [ ] T024a-i Check notification permissions on app startup, display banner if disabled: "Notifications are disabled. You may miss medication reminders."
  - [ ] T024a-ii Provide deep link to system notification settings
- [ ] T025 [P] [US2] Implement 12-hour safety window logic with warning display (not hard block) in src/BloodThinnerTracker.Api/Services/MedicationService.cs
- [ ] T026 [P] [US2] Add unit and integration tests for reminders and logging in tests/BloodThinnerTracker.Api.Tests/

## Phase 5: [US3] Configurable INR Test Reminders & Logging

- [ ] T027 [US3] Implement INR schedule endpoints in src/BloodThinnerTracker.Api/Controllers/INRController.cs
  - [ ] T027-i POST /api/inr/schedules - Create/update INR testing schedule
  - [ ] T027-ii GET /api/inr/schedules - Retrieve user's current INR schedule
  - [ ] T027-iii GET /api/inr/schedules/next - Calculate next INR test date based on frequency
- [ ] T028 [P] [US3] Implement INR reminder notification service in src/BloodThinnerTracker.Api/Services/NotificationService.cs
- [ ] T029 [P] [US3] Implement INR logging endpoints in src/BloodThinnerTracker.Api/Controllers/INRLogController.cs
- [ ] T029a [P] [US3] Implement INR value validation (0.5-8.0 range) and outlier flagging in src/BloodThinnerTracker.Api/Services/INRValidationService.cs
  - [ ] T029a-i Enforce hard validation: Reject INR values <0.5 or >8.0 as invalid input
  - [ ] T029a-ii Flag outlier values for review: INR <1.5 (low, bleeding risk) or >4.5 (high, clotting risk)
  - [ ] T029a-iii Add OutlierFlag field to INRTest entity with enum values: Normal, LowRisk, HighRisk
  - [ ] T029a-iv Display outlier warning in UI: "This INR value is outside normal range. Please consult your healthcare provider."
- [ ] T030 [P] [US3] Implement INR reminder UI in src/BloodThinnerTracker.Mobile/Pages/ and src/BloodThinnerTracker.Web/Pages/
- [ ] T031 [P] [US3] Add unit and integration tests for INR reminders and logging in tests/BloodThinnerTracker.Api.Tests/

## Phase 6: [US4] Historical Data Visualization & Trends

- [ ] T032 [US4] Implement medication and INR history endpoints in src/BloodThinnerTracker.Api/Controllers/HistoryController.cs
- [ ] T033 [P] [US4] Implement charting components for medication and INR history in src/BloodThinnerTracker.Web/Components/ and src/BloodThinnerTracker.Mobile/Pages/
- [ ] T034 [P] [US4] Implement data export endpoints with email sharing, PDF generation, and print preview (JSON, CSV, PDF formats) in src/BloodThinnerTracker.Api/Controllers/ExportController.cs
- [ ] T035 [P] [US4] Add UI for data export with email/print options and date range selection in src/BloodThinnerTracker.Web/Pages/ and src/BloodThinnerTracker.Mobile/Pages/
- [ ] T036 [P] [US4] Add tests for data visualization and export in tests/BloodThinnerTracker.Web.Tests/

## Phase 7: [US5] Missed Dose Recovery & Safety Warnings

- [ ] T037 [US5] Implement missed dose detection and warning logic in src/BloodThinnerTracker.Api/Services/MedicationService.cs
- [ ] T038 [P] [US5] Implement UI for missed dose warnings and recovery in src/BloodThinnerTracker.Mobile/Pages/ and src/BloodThinnerTracker.Web/Pages/
- [ ] T039 [P] [US5] Add tests for missed dose scenarios in tests/BloodThinnerTracker.Api.Tests/

## Final Phase: Polish & Cross-Cutting Concerns

- [ ] T040 Add accessibility (WCAG 2.1 AA) checks to all UI projects
- [ ] T041 [P] Add performance profiling and optimization scripts in tools/scripts/
- [ ] T042 [P] Add security review and OWASP compliance validation
- [ ] T043 [P] Add end-to-end test coverage report and enforce 90% coverage
- [ ] T044 [US8] Implement notification reliability tracking in src/BloodThinnerTracker.Api/Services/NotificationService.cs
- [ ] T044a [US8] Add reminder delivery monitoring with uptime tracking and SLA alerting (99.9% target) in src/BloodThinnerTracker.Api/Services/MonitoringService.cs
  - [ ] T044a-i Create NotificationDeliveryLog entity with fields: NotificationId, UserId, ScheduledAt, SentAt, DeliveredAt, FailureReason
  - [ ] T044a-ii Implement delivery tracking: Log sent/delivered/failed status for each notification attempt
  - [ ] T044a-iii Add metrics endpoint: GET /api/admin/metrics/notification-reliability (calculates success rate over last 24h/7d/30d)
  - [ ] T044a-iv Add alerting: Send alert to admin if delivery rate drops below 99.9% in 24-hour window
  - [ ] T044a-v Implement retry logic: Retry failed notifications up to 3 times with exponential backoff

### Phase 9: Deployment & Release (Tasks T045-T047)
**Assigned To:** DevOps Lead

---

## Dependencies

- US1 (Account & Sync) → US2, US3, US4, US5 (all depend on account setup and sync)
- US2 (Medication Reminders) → US4 (history/trends), US5 (missed dose)
- US3 (INR Reminders) → US4 (history/trends)

## Parallel Execution Examples

- T002, T003, T004, T005, T006 can be done in parallel after T001
- Within each user story phase, [P] tasks can be executed in parallel (e.g., T016, T017, T018, T019)
- Test tasks ([P] in Tests/) can be run in parallel with implementation tasks after endpoints are stubbed

## Implementation Strategy

- MVP: Complete all tasks for US1 (T015–T020) to deliver cross-device account setup and data sync
- Incremental delivery: Each user story phase is independently testable and can be released as a complete increment
- Parallelize foundational and [P] tasks to accelerate delivery

---

**Format validation**: All tasks follow strict checklist format with TaskID, [P] marker, [USx] label, and file paths.
