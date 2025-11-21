# Tasks for Feature: Mobile: Splash + OAuth login + Read-only INR

Feature: Mobile: Splash screen, Login, Read-only recent INR  
Project path: `src/BloodThinnerTracker.Mobile`  
Spec: `specs/010-title-mobile-splash/spec.md`

Phase 1 — Setup (project initialization)

- [ ] T001 Delete existing project folder `src/BloodThinnerTracker.Mobile`
- [ ] T002 Create new .NET MAUI project `src/BloodThinnerTracker.Mobile/BloodThinnerTracker.Mobile.csproj` (scaffold app skeleton)
- [ ] T003 Add NuGet package references in `src/BloodThinnerTracker.Mobile/BloodThinnerTracker.Mobile.csproj`: `CommunityToolkit.Mvvm`, `CommunityToolkit.Maui`, `Microsoft.Maui.Essentials` (or `Microsoft.Maui.Storage`), `Refit` (optional)
- [ ] T004 Create test projects: `tests/Mobile.UnitTests/Mobile.UnitTests.csproj` and `tests/Mobile.UITests/Mobile.UITests.csproj` and reference the main project
- [ ] T005 Add Dependency Injection bootstrap in `src/BloodThinnerTracker.Mobile/App.xaml.cs` to enable service registration

Phase 2 — Foundational (blocking prerequisites)

- [ ] T006 Create domain model `src/BloodThinnerTracker.Mobile/Models/INRTest.cs` (properties: Id, Value, Units, CollectedAt, ReportedAt, Notes)
- [ ] T007 Create `src/BloodThinnerTracker.Mobile/Services/IInrService.cs` interface defining `Task<IEnumerable<INRTestDto>> GetRecentAsync(int count)`
- [ ] T008 Create `src/BloodThinnerTracker.Mobile/Services/MockInrService.cs` implementing `IInrService` with canned data and simulated latency
- [ ] T009 Create `src/BloodThinnerTracker.Mobile/Services/ApiInrService.cs` stub implementing `IInrService` that uses `HttpClient` and bearer tokens (implementation detail: use `Refit` or HttpClient)
- [ ] T010 Create encryption helper `src/BloodThinnerTracker.Mobile/Services/EncryptionService.cs` (AES-256/AesGcm wrapper for encrypt/decrypt)
- [ ] T011 Create secure storage wrapper `src/BloodThinnerTracker.Mobile/Services/SecureStorageService.cs` to persist AES key (uses platform secure storage)
- [ ] T012 Create authentication helper `src/BloodThinnerTracker.Mobile/Services/AuthService.cs` (OAuth PKCE flow stub + token storage in `SecureStorageService`)

Phase 3 — User Story Implementation (priority order)

User Story 1 (US1) — First-run launch and login (Priority: P1)
- [ ] T013 [US1] Create `src/BloodThinnerTracker.Mobile/Views/SplashView.xaml` and `src/BloodThinnerTracker.Mobile/ViewModels/SplashViewModel.cs` (show logo, startup logic)
- [ ] T014 [US1] Implement pulsing animation in `SplashView.xaml` and respect reduced-motion in `SplashViewModel` (use platform API to read accessibility setting)
- [ ] T015 [US1] Create `src/BloodThinnerTracker.Mobile/Views/LoginView.xaml` and `src/BloodThinnerTracker.Mobile/ViewModels/LoginViewModel.cs` with OAuth sign-in button calling `AuthService`
- [ ] T016 [US1] Add navigation wiring in `src/BloodThinnerTracker.Mobile/App.xaml.cs` to transition from Splash -> Login -> Main based on auth state
- [ ] T017 [US1] [P] Add unit tests: `tests/Mobile.UnitTests/SplashViewModelTests.cs` and `tests/Mobile.UnitTests/LoginViewModelTests.cs` (verify startup logic and auth invocation)

User Story 2 (US2) — View recent INR values (Priority: P1)
- [ ] T018 [US2] Create `src/BloodThinnerTracker.Mobile/Views/InrListView.xaml` and `src/BloodThinnerTracker.Mobile/ViewModels/InrListViewModel.cs` to render INR list (loading, empty, error states)
- [ ] T019 [US2] Implement wiring in `InrListViewModel` to call `IInrService.GetRecentAsync(count)` and map to `INRTestDto`
- [ ] T020 [US2] Implement cache read/write using `EncryptionService` + `SecureStorageService` in `src/BloodThinnerTracker.Mobile/Services/CacheService.cs` (store encrypted payload, CachedAt, ExpiresAt)
- [ ] T021 [US2] Implement stale-warning logic in `InrListViewModel`: if cache age > 1 hour show warning message; if cache expired, show offline notice
- [ ] T022 [US2] [P] Add unit tests: `tests/Mobile.UnitTests/InrListViewModelTests.cs` and `tests/Mobile.UnitTests/CacheServiceTests.cs` (verify fetch, cache, stale detection)

User Story 3 (US3) — Accessibility & motion preferences (Priority: P2)
- [ ] T023 [US3] Implement reduced-motion detection utility `src/BloodThinnerTracker.Mobile/Services/AccessibilityService.cs` (wrap platform APIs)
- [ ] T024 [US3] Wire `AccessibilityService` into `SplashViewModel` to disable animation when requested
- [ ] T025 [US3] Add UI test `tests/Mobile.UITests/AccessibilityTests.cs` to assert animation is disabled when reduced-motion is enabled (mock or emulator setting)

Phase 4 — Polish & Cross-cutting concerns
- [ ] T026 Implement localized date formatting and resource strings in `src/BloodThinnerTracker.Mobile/Resources/Strings.resx` and wire to views
- [ ] T027 Implement robust error messaging and logging in `src/BloodThinnerTracker.Mobile/Services/ErrorHandling.cs` and local telemetry hooks (debug logging)
- [ ] T028 Add example GitHub Actions workflow `/.github/workflows/mobile-build.yml` to build `src/BloodThinnerTracker.Mobile` for `net10.0-android` and `net10.0-windows10.0.19041.0`
- [ ] T029 Add a placeholder workflow `/.github/workflows/mobile-publish.yml` with steps for Play Store / Microsoft Store publishing (use secrets/OIDC guidance in `specs/010-title-mobile-splash/research.md`)

- [ ] T030 Document server-side token-exchange endpoint contract in `specs/010-title-mobile-splash/contracts/auth-exchange.md` (describe `POST /auth/exchange` request/response and validation rules)
- [ ] T031 [P] Add integration test `tests/Integration/AuthExchangeTests.cs` that simulates obtaining an `id_token` (mocked) and verifies the backend validates the `id_token` and issues an internal bearer token
- [ ] T032 [US1] Update acceptance tests in `specs/010-title-mobile-splash/tests/acceptance.md` to include id_token → internal-bearer exchange evidence steps (capture id_token receipt, token-exchange response, authenticated API call)
- [ ] T033 Add CI/QA check `/.github/workflows/check-auth-exchange.yml` to run `tests/Integration/AuthExchangeTests.cs` against a staging auth endpoint (requires secrets / staging config)

- [ ] T034 Implement encryption key-management tasks: KDF/HKDF or PBKDF2 derivation, per-device key derivation, and key-rotation/migration support; add unit tests for rotation and tamper detection.
- [ ] T035 Add CI coverage gating: collect XPlat code coverage for `tests/Mobile.UnitTests` and fail CI if coverage < 80% for feature projects; add workflow snippet to `.github/workflows/coverage-check.yml`.
- [ ] T036 Add performance telemetry tasks: implement cold-start and render timing telemetry, create a CI benchmark job to validate SC-001/SC-002 under a defined network profile.
- [ ] T037 Correct tasks metadata and summary: update the summary counts and ensure the README/summary accurately reflects the task list.

Dependencies (story completion order)

- US1 must be implemented before full US2 verification (users must authenticate before viewing private INR data).  
- US2 depends on T006..T012 (models, IInrService, encryption, cache, auth) in Phase 2.  
- US3 is largely independent but requires AccessibilityService (T023) to be implemented before T014 animation suppression.

Parallel execution examples

- Tasks that can run in parallel (no file overlaps): T008 `MockInrService`, T009 `ApiInrService` stub, T010 `EncryptionService` development, and T011 `SecureStorageService` can be implemented concurrently by separate engineers. Marked with `[P]` where appropriate in the list.
- Unit test development tasks (T017, T022) can proceed in parallel with View/ViewModel implementation once interfaces exist.

Independent test criteria per user story

- US1 independent test: Install app, launch -> splash shown -> navigate to login -> perform OAuth (mock) -> land on main screen. Evidence: screenshot of splash, login flow trace, main screen with INR list.
- US2 independent test: Authenticated device fetches INR list -> shows five latest results, shows empty state when none, shows stale warning when cache age > 1 hour. Evidence: unit test pass, UI screenshot, logs.
- US3 independent test: With reduced-motion enabled, splash animation is not animated. Evidence: UI test assertion + screenshot.

Implementation strategy (MVP first)

- MVP scope: Deliver US1 and US2 minimal flow with `MockInrService` (no publishing). That includes T001..T022 at minimum.  
- Incremental delivery: implement Phase 1+2 first, then implement US1, then US2, then US3, adding tests after each story's core code is in place.

Files created/edited by tasks (high-level)

- `src/BloodThinnerTracker.Mobile/**` (project, views, viewmodels, services, models)
- `tests/Mobile.UnitTests/**`
- `tests/Mobile.UITests/**`
- `.github/workflows/mobile-build.yml` and `.github/workflows/mobile-publish.yml`
- `specs/010-title-mobile-splash/tasks.md` (this file)

Summary

- Total tasks: 37
- Tasks for US1: 5 (+2 test tasks)  
- Tasks for US2: 5 (+1 test task)  
- Tasks for US3: 3 (+1 test task)  
- Parallel opportunities: `MockInrService`, `ApiInrService`, `EncryptionService`, `SecureStorageService`, and unit test tasks
