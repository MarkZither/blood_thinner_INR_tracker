# Tasks: Blood Thinner Medication & INR Tracker

**Feature**: Blood Thinner Medication & INR Tracker
**Branch**: feature/blood-thinner-medication-tracker
**Date**: 2025-10-15

---

## Phase 1: Setup

- [ ] T001 Create multi-platform .NET 10 solution structure with global.json and Directory.Build.props per plan.md
- [ ] T002 [P] Initialize Git repository and configure .editorconfig, StyleCop, and Roslyn analyzers
- [ ] T003 [P] Add .NET Aspire orchestration projects (AppHost, ServiceDefaults) in src/
- [ ] T004 [P] Create src/, tests/, docs/, samples/, tools/ folder structure per David Fowler conventions
- [ ] T005 [P] Add README.md and copy quickstart.md to repo root
- [ ] T006 [P] Add .gitignore and basic CI/CD pipeline config in .github/workflows/

## Phase 2: Foundational

- [ ] T007 Add Entity Framework Core and configure multi-provider (SQLite, PostgreSQL) in src/BloodThinnerTracker.Api/
- [ ] T008 [P] Implement User, Medication, INR, Device, Preferences, AuditLog, SyncMetadata entities in src/BloodThinnerTracker.Shared/Models/
- [ ] T009 [P] Add initial database migrations and apply to local SQLite and PostgreSQL
- [ ] T010 [P] Implement authentication abstraction and OAuth2 integration (Azure AD, Google) in src/BloodThinnerTracker.Api/
- [ ] T011 [P] Add JWT token issuance and validation middleware in src/BloodThinnerTracker.Api/
- [ ] T012 [P] Add SignalR hub for real-time sync in src/BloodThinnerTracker.Api/
- [ ] T013 [P] Add OpenTelemetry and health checks to all services
- [ ] T014 [P] Add medical disclaimer to all UI projects (Mobile, Web, Console)

## Phase 3: [US1] Cross-Device Account Setup & Data Sync

- [ ] T015 [US1] Implement user registration and login endpoints in src/BloodThinnerTracker.Api/Controllers/AuthController.cs
- [ ] T016 [P] [US1] Implement device registration and sync endpoints in src/BloodThinnerTracker.Api/Controllers/DeviceController.cs
- [ ] T017 [P] [US1] Implement user session management and refresh tokens in src/BloodThinnerTracker.Api/Services/
- [ ] T018 [P] [US1] Implement user account creation UI in src/BloodThinnerTracker.Mobile/Pages/ and src/BloodThinnerTracker.Web/Pages/
- [ ] T019 [P] [US1] Implement cross-device data sync logic in src/BloodThinnerTracker.Shared/Services/SyncService.cs
- [ ] T020 [P] [US1] Add integration tests for account creation and sync in tests/BloodThinnerTracker.Integration.Tests/

## Phase 4: [US2] Daily Medication Reminders & Logging

- [ ] T021 [US2] Implement medication scheduling endpoints in src/BloodThinnerTracker.Api/Controllers/MedicationController.cs
- [ ] T022 [P] [US2] Implement medication reminder notification service in src/BloodThinnerTracker.Api/Services/NotificationService.cs
- [ ] T023 [P] [US2] Implement medication logging endpoints in src/BloodThinnerTracker.Api/Controllers/MedicationLogController.cs
- [ ] T024 [P] [US2] Implement medication reminder UI in src/BloodThinnerTracker.Mobile/Pages/ and src/BloodThinnerTracker.Web/Pages/
- [ ] T025 [P] [US2] Implement 12-hour safety window logic in src/BloodThinnerTracker.Api/Services/MedicationService.cs
- [ ] T026 [P] [US2] Add unit and integration tests for reminders and logging in tests/BloodThinnerTracker.Api.Tests/

## Phase 5: [US3] Configurable INR Test Reminders & Logging

- [ ] T027 [US3] Implement INR schedule endpoints in src/BloodThinnerTracker.Api/Controllers/INRController.cs
- [ ] T028 [P] [US3] Implement INR reminder notification service in src/BloodThinnerTracker.Api/Services/NotificationService.cs
- [ ] T029 [P] [US3] Implement INR logging endpoints in src/BloodThinnerTracker.Api/Controllers/INRLogController.cs
- [ ] T030 [P] [US3] Implement INR reminder UI in src/BloodThinnerTracker.Mobile/Pages/ and src/BloodThinnerTracker.Web/Pages/
- [ ] T031 [P] [US3] Add unit and integration tests for INR reminders and logging in tests/BloodThinnerTracker.Api.Tests/

## Phase 6: [US4] Historical Data Visualization & Trends

- [ ] T032 [US4] Implement medication and INR history endpoints in src/BloodThinnerTracker.Api/Controllers/HistoryController.cs
- [ ] T033 [P] [US4] Implement charting components for medication and INR history in src/BloodThinnerTracker.Web/Components/ and src/BloodThinnerTracker.Mobile/Pages/
- [ ] T034 [P] [US4] Implement data export endpoints (JSON, CSV, PDF) in src/BloodThinnerTracker.Api/Controllers/ExportController.cs
- [ ] T035 [P] [US4] Add UI for data export and sharing in src/BloodThinnerTracker.Web/Pages/ and src/BloodThinnerTracker.Mobile/Pages/
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
- [ ] T044 [P] Update documentation and user guides in docs/

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
