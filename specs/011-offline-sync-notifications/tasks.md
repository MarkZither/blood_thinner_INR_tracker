---
description: "Generated task list for Offline Sync & Notifications"
---

# Tasks: Offline Sync & Notifications

**Input**: Design documents from `specs/011-offline-sync-notifications/`
**Prerequisites**: `plan.md` (required), `spec.md` (required for user stories), `research.md`, `data-model.md`, `contracts/`

## Notes

All tasks are organized by user story. Tasks follow the strict checklist format required by `speckit.tasks`.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and configuration that supports the feature across the mobile client.

- [ ] T001 [P] Add `SyncOptions` configuration model and defaults in `src/BloodThinnerTracker.Mobile/Config/SyncOptions.cs` and register it in `src/BloodThinnerTracker.Mobile/MauiProgram.cs`
- [ ] T002 [P] Verify EF Core SQLite package references and versions in `src/BloodThinnerTracker.Mobile/BloodThinnerTracker.Mobile.csproj` (ensure compatibility with shared models) and restore packages if any are missing
- [ ] T003 [P] Verify the existing `DbContext` registration and migration behavior in `src/BloodThinnerTracker.Mobile/MauiProgram.cs` (no new `MobileDbContext` creation required); document where the context is defined and how migrations run at startup
- [ ] T004 [P] Document DB initialization/migration behavior in `specs/011-offline-sync-notifications/quickstart.md` and add a verification test that confirms migrations run on cold start
- [ ] T005 [P] Configure logging and diagnostics for the mobile app in `src/BloodThinnerTracker.Mobile/Services/LoggingSetup.cs` and register in `MauiProgram.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before any user story implementation begins.

- [ ] T006 [P] Use a generated API client (Kiota/OpenAPI) instead of hand-crafted client classes. Add a generation target that outputs a shared client project at `src/BloodThinnerTracker.Clients/` and document how `src/BloodThinnerTracker.Mobile` and `src/BloodThinnerTracker.Web` should reference it (project reference or local package). Do NOT add client classes to the API/server project.
 - [ ] T007 [P] Extend the existing `CacheSyncService` (`src/BloodThinnerTracker.Mobile/Services/CacheSyncService.cs`) to fetch INR tests, medications, and medication logs. Implement extension points to call the generated API client and upsert into the existing `MobileDbContext`. Do NOT create a duplicate background worker; consolidate periodic/background logic into `CacheSyncService`.
- [ ] T008 [P] Ensure `CacheSyncService` is registered as a hosted/background service in `src/BloodThinnerTracker.Mobile/MauiProgram.cs` with configurable intervals from `SyncOptions`. Update the timer logic to respect `DefaultInterval` and `FastPathInterval` and to call `NotificationScheduler` for scheduled reminders after sync.
- [ ] T009 Implement an `INotificationScheduler` abstraction in `src/BloodThinnerTracker.Mobile/Services/NotificationScheduler.cs` (platform-agnostic interface)
 - [ ] T033 [P] Implement `NotificationScheduler.Android` using WorkManager in `src/BloodThinnerTracker.Mobile/Platforms/Android/NotificationScheduler.Android.cs`. Behavior: schedule notifications via `OneTimeWorkRequest` with a tag/unique id; record the WorkRequest id (GUID string) in the Phase-1 JSON schedule store (`src/BloodThinnerTracker.Mobile/Data/scheduled_reminders.json`) so UI and boot-resume logic can correlate work to reminders. Document this as temporary storage and plan migration to DB in Phase 2.
- [ ] T034 [P] Implement optional exact-alarm path using AlarmManager in `src/BloodThinnerTracker.Mobile/Platforms/Android/NotificationScheduler.Alarm.cs` for high-priority reminders. Request `SCHEDULE_EXACT_ALARM` in docs and gracefully fall back to WorkManager if permission is not available. Use unique `requestCode` or encoded PendingIntent extras to allow cancellation.
 - [ ] T035 [P] For this phase, track scheduled reminders in a JSON file instead of extending the DB: create read/write helpers that persist scheduled items to `src/BloodThinnerTracker.Mobile/Data/scheduled_reminders.json` (or platform-appropriate storage path). Document this temporary approach in `specs/011-offline-sync-notifications/quickstart.md` and mark as a migration task for later phases to move to DB.
 - [ ] T036 [P] Implement reboot/boot-complete reschedule: `BootReceiver` in `src/BloodThinnerTracker.Mobile/Platforms/Android/BootReceiver.cs` that reads scheduled reminders from the JSON file and re-enqueues WorkManager requests or re-registers alarms.
 - [ ] T037 [P] Implement `CancelAsync(Guid reminderId)` and `GetScheduledAsync(Guid reminderId)` on `INotificationScheduler` using the JSON store as the source-of-truth for scheduled items.
	- `GetScheduledAsync` behavior: for WorkManager query `WorkManager.Instance.GetWorkInfosByTag(reminderTag)` (or `GetWorkInfoByIdAsync`) and return WorkInfo; for AlarmManager use `PendingIntent.GetBroadcast(..., PendingIntentFlags.NoCreate)` to check existence (OS doesn't expose trigger time) and rely on the JSON file for trigger metadata.
- [ ] T038 [P] Add unit and device integration tests for scheduling, firing, cancellation, and reboot rescheduling in `tests/BloodThinnerTracker.Mobile.Tests/NotificationSchedulerTests.cs` and `specs/011-offline-sync-notifications/quickstart.md` device steps.
- [ ] T039 [P] Update `specs/011-offline-sync-notifications/quickstart.md` with exact-alarm permission guidance (`SCHEDULE_EXACT_ALARM`), manufacturer caveats, DnD notes, how to inspect scheduled WorkManager work via `adb shell` commands for debugging, and details about the temporary JSON file approach (`src/BloodThinnerTracker.Mobile/Data/scheduled_reminders.json`) including migration notes for later phases.
 - [ ] T040 [P] Implement `NotificationScheduler.Windows` in `src/BloodThinnerTracker.Mobile/Platforms/Windows/NotificationScheduler.Windows.cs` using `ScheduledToastNotification` APIs. Responsibilities: schedule toasts with an `Id` (use reminder GUID), query scheduled toasts, cancel scheduled toasts, and surface delivery metadata to the app.
 - [ ] T041 [P] Register AUMID and notification activator (DesktopNotificationManagerCompat pattern) in `src/BloodThinnerTracker.Mobile/Platforms/Windows/` startup code; document steps and packaging impacts in `specs/011-offline-sync-notifications/quickstart.md`.
 - [ ] T042 [P] Implement JSON-based persistence reconciliation for Windows: on app start, `NotificationScheduler.Windows` should read `src/BloodThinnerTracker.Mobile/Data/scheduled_reminders.json`, compare with `ToastNotificationManager.CreateToastNotifier(appId).GetScheduledToastNotifications()`, and re-sync missing schedules.
 - [ ] T043 [P] Implement `GetScheduledAsync(Guid reminderId)` and `CancelAsync(Guid reminderId)` on `INotificationScheduler` for Windows using `GetScheduledToastNotifications()` and `RemoveFromSchedule()` and ensure these operations update the JSON store consistently.
 - [ ] T044 [P] Add a debug page `src/BloodThinnerTracker.Mobile/Pages/Debug/ScheduledReminders.razor` that lists JSON-scheduled reminders and OS-scheduled toasts (Windows) or WorkManager work (Android) to aid debugging during development.
 - [ ] T045 [P] Add Windows-specific unit/integration tests in `tests/BloodThinnerTracker.Mobile.Tests/NotificationSchedulerWindowsTests.cs` that verify scheduling, querying, cancellation, and app-start reconciliation (use sim/hosted environment where possible).
 - [ ] T046 [P] Update `specs/011-offline-sync-notifications/quickstart.md` with Windows-specific instructions: AUMID registration, DesktopNotificationManagerCompat notes for unpackaged apps, packaging (MSIX) benefits, and how to use the debug page to inspect scheduled toasts.
- [ ] T010 [P] Add test project wiring: `tests/BloodThinnerTracker.Mobile.Tests/` and a baseline test in `tests/BloodThinnerTracker.Mobile.Tests/StartupTests.cs`
 - [ ] T011 Add configuration for sync intervals in `appsettings.json` (or platform config) at `src/BloodThinnerTracker.Mobile/appsettings.json` and document defaults in `specs/011-offline-sync-notifications/quickstart.md`
 - [ ] T032 [P] Add a client-generation script (`tools/clients/generate-client.ps1`) and CI workflow step that regenerates `src/BloodThinnerTracker.Clients/` from the canonical OpenAPI file in `specs/011-offline-sync-notifications/contracts/openapi.yaml`. Document regeneration steps in `specs/011-offline-sync-notifications/quickstart.md`.

**Checkpoint**: Foundational work complete — models, DB, API client, and background skeleton in place.

---

## Phase 3: User Story 1 - Offline availability & automatic sync (Priority: P1) [US1]

**Goal**: Persist medication lists, medication logs, and INR tests locally and perform scheduled/foreground synchronization from the server.

**Independent Test**: Put device in airplane mode and verify the medication list loads from local SQLite within 2 seconds.

- [ ] T012 [US1] Verify the shared entities in `src/BloodThinnerTracker.Shared/Models` are already mapped by the mobile project's existing `DbContext` (confirm property types and table names match server); if a mapping gap exists, create a focused migration to align types
- [ ] T013 [P] [US1] Verify required indexes (`Medication.IsActive`, `MedicationLog.ScheduledTime`, `INRTests.TestDate`) are present in the applied migrations for the mobile DB; if missing, add a small migration in `src/BloodThinnerTracker.Mobile/Migrations/` to create them
- [ ] T014 [US1] Implement `CacheSyncService.FetchAndUpsertAsync()` in `src/BloodThinnerTracker.Mobile/Services/CacheSyncService.cs` to call the generated client read endpoints and upsert results into the existing mobile DB context (do not create a new DbContext type)
- [ ] T015 [US1] Implement foreground refresh hookup: invoke `CacheSyncService.SyncOnceAsync()` on app start in `src/BloodThinnerTracker.Mobile/App.xaml.cs` or `src/BloodThinnerTracker.Mobile/Pages/MainPage.razor` so a foreground refresh runs when appropriate.
- [ ] T016 [US1] Ensure the existing background hosted worker (`CacheSyncService`) performs periodic syncs and respects `SyncOptions` intervals (`DefaultInterval` and `FastPathInterval`). Confirm there is no duplicate `SyncWorker` background job; if one exists, consolidate logic into `CacheSyncService` and remove duplicates. Update registrations in `src/BloodThinnerTracker.Mobile/MauiProgram.cs` as needed.
- [ ] T017 [US1] Add unit tests for `CacheSyncService` in `tests/BloodThinnerTracker.Mobile.Tests/CacheSyncServiceTests.cs` (mock the generated API client and the existing mobile DB context to verify fetch-and-upsert and error handling)
- [ ] T018 [US1] Add an integration smoke test that starts the mobile app (or test harness) and validates local DB is populated from a running API (document test steps in `specs/011-offline-sync-notifications/quickstart.md`)

**Checkpoint**: US1 should be independently testable and pass offline availability acceptance scenarios

---

## Phase 4: User Story 2 - Reliable local reminders (Priority: P1) [US2]

**Goal**: Deliver local notifications at scheduled times, even when the app is backgrounded or the device is offline.

**Independent Test**: Schedule a medication for next minute and confirm a local notification is delivered while the app is backgrounded.

- [ ] T019 [P] [US2] Implement Android-specific `NotificationScheduler` in `src/BloodThinnerTracker.Mobile/Platforms/Android/NotificationScheduler.Android.cs` and ensure `NotificationChannel` creation
- [ ] T020 [US2] Implement Windows-specific `NotificationScheduler` in `src/BloodThinnerTracker.Mobile/Platforms/Windows/NotificationScheduler.Windows.cs` (if supported by app targets)
- [ ] T021 [US2] Ensure `CacheSyncService` computes upcoming reminders from `MobileDbContext` after a successful sync and calls `NotificationScheduler.ScheduleNotification()` (no separate `SyncWorker` implementation required unless platform-specific differences demand it). Update `CacheSyncService` to expose extension points for reminder computation and scheduling.
- [ ] T022 [US2] Respect user notification permissions: add permission checks and graceful fallback in `src/BloodThinnerTracker.Mobile/Services/NotificationScheduler.cs` and surface guidance in `src/BloodThinnerTracker.Mobile/Pages/Settings.razor`
- [ ] T023 [US2] Add a manual verification checklist and device-run instructions in `specs/011-offline-sync-notifications/quickstart.md` for reminder delivery tests

---

## Phase 5: User Story 3 - Manual Sync and visibility (Priority: P2) [US3]

**Goal**: Provide a visible sync status and a manual "Sync Now" action the user can trigger.

**Independent Test**: Trigger "Sync Now" while online; verify network calls complete and the UI reports success. Trigger while offline; verify button is disabled and UI shows offline state.

- [ ] T024 [P] [US3] Add a `SyncStatus` component in `src/BloodThinnerTracker.Mobile/Components/SyncStatus.razor` that displays last sync time and current state
 - [ ] T025 [US3] Add a `Sync Now` button to `src/BloodThinnerTracker.Mobile/Pages/Settings.razor` or `src/BloodThinnerTracker.Mobile/Pages/SyncPage.razor` and connect it to `CacheSyncService.SyncOnceAsync()` (or to a small facade `ISyncController.SyncNowAsync()` that delegates to `CacheSyncService`).
- [ ] T026 [US3] Implement UI state management to disable `Sync Now` when offline and show a pending indicator in `src/BloodThinnerTracker.Mobile/Services/SyncStateService.cs`
- [ ] T027 [US3] Add UI tests or integration steps in `tests/BloodThinnerTracker.Mobile.Tests/SyncUiTests.cs` (or document manual verification if UI automation isn't available)

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T028 [P] Update documentation: `specs/011-offline-sync-notifications/quickstart.md`, `specs/011-offline-sync-notifications/research.md`, and `README.md`
- [ ] T029 [P] Add telemetry/tracing to `CacheSyncService` and `NotificationScheduler` in `src/BloodThinnerTracker.Mobile/Services/Telemetry/SyncTelemetry.cs` (capture sync outcomes, scheduling events, and delivery results).
- [ ] T030 [P] Add retention/cleanup job to prune local data older than configured retention in `src/BloodThinnerTracker.Mobile/Services/DataRetentionService.cs`
 - [ ] T031 [P] Run performance checks and ensure foreground refresh completes within 60s for <=100 items (document results in `docs/`)
 - [ ] T047 [P] Add CI coverage gate: configure the mobile test pipeline to fail the build if unit+integration coverage for `src/BloodThinnerTracker.Mobile` is below 80%. Document the gate and add a coverage badge to README.
 - [ ] T048 [P] Integrate test-coverage measurement: enable coverage collection in `tests/BloodThinnerTracker.Mobile.Tests` (e.g., `coverlet`), publish coverage artifacts in CI, and add a pipeline task that uploads coverage reports for analysis.
 - [ ] T049 [CRITICAL] Ensure mobile at-rest encryption and JSON-store protection: configure and verify AES-256 encryption for the mobile SQLite store (use `sqlcipher` or platform equivalent) and ensure the Phase-1 JSON schedule file `src/BloodThinnerTracker.Mobile/Data/scheduled_reminders.json` is stored encrypted or under platform-protected storage. Add unit/integration tests that validate the DB is encrypted and that the JSON file cannot be read by other apps/users. Document the encryption configuration in `specs/011-offline-sync-notifications/quickstart.md`.
 - [ ] T050 [P] Add performance benchmark and CI measurement tasks: add automated MAUI startup and foreground-refresh benchmark jobs to CI (measure cold start time, warm start time, and foreground refresh latency for <=100 items). Fail the pipeline if startup exceeds configured thresholds (e.g., MAUI cold start >3s) and publish benchmark artifacts.
 - [ ] T051 [P] Optional refactor task — rename `CacheSyncService` to `SyncService`: if the team chooses a different canonical name, perform a careful refactor: create `SyncService` facade that delegates to `CacheSyncService`, update DI registrations and references, run tests, and perform a separate PR. Document rationale and add rollback instructions. This task is explicitly a refactor and should be scheduled separately from Phase 2 implementation work.

---

## Dependencies & Execution Order

- Phase 1 (Setup) must complete before Phase 2 (Foundational) begins.
- Phase 2 (Foundational) must complete before any User Story phases begin.
- User stories (Phase 3+) can be worked on in parallel after Phase 2 is complete. Prioritize P1 stories (US1, US2) as MVP.

## Parallel execution examples

- Workstream A: Implement `MobileDbContext` (`T003`, `T012`) and DB init (`T004`) while Workstream B implements `MedicationApiClient` (`T006`) and `CacheSyncService` scaffold (`T007`) — these are parallelizable.
- Platform work: Android `NotificationScheduler` (`T019`) can be implemented in parallel with Windows scheduler (`T020`) and UI work (`T024`–`T026`).

## Implementation strategy (MVP)

1. Finish Phase 1 → Setup (T001–T005).
2. Complete Phase 2 → Foundational (T006–T011).
3. Implement US1 (T012–T018) and validate offline availability.
4. Implement US2 (T019–T023) and validate reminder delivery.
5. Implement US3 (T024–T027) and validate Sync Now UX.
6. Polish, add telemetry, and validate performance (T028–T031).

---

Generated on 2025-12-05 from `specs/011-offline-sync-notifications/spec.md` and `plan.md`.
# Implementation Tasks — Offline Sync & Notifications

Priority order (smallest deliverable first):

1. Verify read-only API endpoints exist and return expected shapes (API smoke tests) — 1d
2. Add `SyncOptions` (IOptions) to mobile app with `DefaultIntervalMinutes` and `FastPathIntervalMinutes` — 0.5d
3. Extend existing background worker to also fetch medications + medication logs and schedule notifications — 2d
4. Wire foreground refresh on app start if last sync older than `DefaultIntervalMinutes` — 1d
5. Add telemetry/logging for sync outcomes and expose a UI sync status indicator — 1d
6. Add unit & integration tests for worker and API contract (xUnit) — 2d
7. Add a Playwright e2e test that runs API and mobile host (when possible) to validate sync + basic notification flow — 2d
8. Documentation & quickstart updates (this folder) — 0.5d

Total estimate: ~10 days (can be scoped to <=2 weeks by splitting tasks 3 and 6 across sub-tasks)
