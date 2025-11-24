# Tasks for Feature: Mobile: Splash + OAuth login + Read-only INR

Feature: Mobile: Splash screen, Login, Read-only recent INR  
Project path: `src/BloodThinnerTracker.Mobile`  
Spec: `specs/010-title-mobile-splash/spec.md`

Phase 1 — Setup (project initialization)

- [x] T001 Delete existing project folder `src/BloodThinnerTracker.Mobile`
- [x] T002 Create new .NET MAUI project `src/BloodThinnerTracker.Mobile/BloodThinnerTracker.Mobile.csproj` (scaffold app skeleton)
- [x] T003 Add NuGet package references in `src/BloodThinnerTracker.Mobile/BloodThinnerTracker.Mobile.csproj`: `CommunityToolkit.Mvvm`, `CommunityToolkit.Maui`, `Microsoft.Maui.Essentials` (or `Microsoft.Maui.Storage`), `Refit` (optional)
- [x] T004 Create test projects: `tests/Mobile.UnitTests/Mobile.UnitTests.csproj` and `tests/Mobile.UITests/Mobile.UITests.csproj` and reference the main project
- [x] T005 Add Dependency Injection bootstrap in `src/BloodThinnerTracker.Mobile/App.xaml.cs` to enable service registration

Phase 2 — Foundational (blocking prerequisites)

- [ ] T006 Create domain model `src/BloodThinnerTracker.Mobile/Models/INRTest.cs` (properties: Id, Value, Units, CollectedAt, ReportedAt, Notes)
- [ ] T007 Create `src/BloodThinnerTracker.Mobile/Services/IInrService.cs` interface defining `Task<IEnumerable<INRTestDto>> GetRecentAsync(int count)`
- [ ] T008 Create `src/BloodThinnerTracker.Mobile/Services/MockInrService.cs` implementing `IInrService` with canned data and simulated latency
- [ ] T009 Create `src/BloodThinnerTracker.Mobile/Services/ApiInrService.cs` stub implementing `IInrService` that uses `HttpClient` and bearer tokens (implementation detail: use `Refit` or HttpClient)
- [ ] T010 Create encryption helper `src/BloodThinnerTracker.Mobile/Services/EncryptionService.cs` (AES-256/AesGcm wrapper for encrypt/decrypt)
- [ ] T011 Create secure storage wrapper `src/BloodThinnerTracker.Mobile/Services/SecureStorageService.cs` to persist AES key (uses platform secure storage)
- [ ] T012 Create authentication helper `src/BloodThinnerTracker.Mobile/Services/AuthService.cs` (OAuth PKCE flow stub + token storage in `SecureStorageService`)
 - [x] T006 Create domain model `src/BloodThinnerTracker.Mobile/Models/INRTest.cs` (properties: Id, Value, Units, CollectedAt, ReportedAt, Notes)
 - [x] T007 Create `src/BloodThinnerTracker.Mobile/Services/IInrService.cs` interface defining `Task<IEnumerable<INRTestDto>> GetRecentAsync(int count)`
 - [x] T008 Create `src/BloodThinnerTracker.Mobile/Services/MockInrService.cs` implementing `IInrService` with canned data and simulated latency
 - [x] T009 Create `src/BloodThinnerTracker.Mobile/Services/ApiInrService.cs` stub implementing `IInrService` that uses `HttpClient` and bearer tokens (implementation detail: use `Refit` or HttpClient)
 - [x] T010 Create encryption helper `src/BloodThinnerTracker.Mobile/Services/EncryptionService.cs` (AES-256/AesGcm wrapper for encrypt/decrypt)
 - [x] T011 Create secure storage wrapper `src/BloodThinnerTracker.Mobile/Services/SecureStorageService.cs` to persist AES key (uses platform secure storage)
 - [x] T012 Create authentication helper `src/BloodThinnerTracker.Mobile/Services/AuthService.cs` (OAuth PKCE flow stub + token storage in `SecureStorageService`)

Phase 3 — User Story Implementation (priority order)

User Story 1 (US1) — First-run launch and login (Priority: P1)
- [x] T013 [US1] ~~Create SplashView with animation~~ → **REPLACED**: Removed SplashView; using native platform splash + direct auth check in App.CreateWindow() (see US1-COMPLETION-SUMMARY.md)
- [x] T014 [US1] ~~Implement pulsing animation~~ → **REMOVED**: Not needed after SplashView removal; accessibility concerns deferred to US3 (T023)
- [x] T015 [US1] Create LoginView and LoginViewModel with OAuth PKCE sign-in buttons (Azure AD + Google)
- [x] T016 [US1] Add navigation wiring: auth check → LoginView (unauthenticated) or FlyoutHome (authenticated); routes via AppShell
- [x] T017 [US1] Add unit tests: LoginViewModelTests.cs (5 tests), OAuthConfigServiceTests.cs (3 tests), MockAuthServiceTests.cs (8 tests) - 15/15 passing

User Story 2 (US2) — View recent INR values (Priority: P1)
- [x] T018 [US2] ~~Create `src/BloodThinnerTracker.Mobile/Views/InrListView.xaml` and `src/BloodThinnerTracker.Mobile/ViewModels/InrListViewModel.cs` to render INR list (loading, empty, error states)~~ → **COMPLETED**: InrListView displays recent INR values; ViewModel loads data on appear; basic error state shown
- [x] T019 [US2] ~~Implement wiring in `InrListViewModel` to call `IInrService.GetRecentAsync(count)` and map to `INRTestDto`~~ → **COMPLETED**: ViewModel calls `GetRecentAsync(10)` on view appear, displays results in ObservableCollection
- [x] T020 [US2] ~~Implement cache read/write using `EncryptionService` + `SecureStorageService` in `src/BloodThinnerTracker.Mobile/Services/CacheService.cs` (store encrypted payload, CachedAt, ExpiresAt)~~ → **COMPLETED**: CacheService.cs created with AES-256 encryption, 7-day retention, metadata tracking (CachedAt, ExpiresAt)
- [x] T021 [US2] ~~Implement stale-warning logic in `InrListViewModel`: if cache age > 1 hour show warning message; if cache expired, show offline notice~~ → **COMPLETED**: Added ShowStaleWarning, IsOfflineMode UI properties; InrListView displays orange banner (cache age > 1h) and gray offline badge; TryLoadFromCacheAsync() checks staleness
- [x] T022 [US2] ~~Add unit tests: `tests/Mobile.UnitTests/InrListViewModelTests.cs` and `tests/Mobile.UnitTests/CacheServiceTests.cs` (verify fetch, cache, stale detection)~~ → **COMPLETED**: 54 unit tests passing (30 new tests for caching: CacheServiceTests, InrListViewModelCacheTests, InrListItemViewModelTests)

**Phase 5 — Enhancement & Configuration (continued)**
- [x] **T042** [COMPLETED] Improve INR list display with better UX
  - ✅ Show status indicator (Normal/Elevated/Low: 2.0-3.0 = Normal, >3.0 = Elevated, <2.0 = Low)
  - ✅ Add refresh button to manually fetch latest data
  - ✅ Show last-updated timestamp
  - ✅ Empty state message when no INR data available
  - ✅ Error state with retry button on load failure
  - ✅ Tests: Added `InrListItemViewModelTests.cs` with 13 tests covering all status indicators
  - **ACTUAL STATE**: 68 Mobile unit tests passing, comprehensive UX with status badges and error handling

- [x] **T043** [NEW] Implement app theme with sensible color scheme
  - Create `src/BloodThinnerTracker.Mobile/Themes/AppColors.xaml` with color definitions
    - Primary: Medical blue (#0066CC)
    - Accent: Medical red (#DC3545) for elevated INR values
    - Neutral: Gray scale (#333333, #666666, #CCCCCC, #F5F5F5)
    - Status colors: Green (#28A745) for normal, Orange (#FFC107) for warning, Red (#DC3545) for critical
  - Create `src/BloodThinnerTracker.Mobile/Themes/AppStyles.xaml` with default styles for Label, Button, Frame
  - Apply theme globally in `App.xaml`
  - Update all views (LoginView, InrListView, AboutView) to use theme colors
  - Support dark mode with AppThemeBinding
  - Tests: Add `ThemeTests.cs` to verify color definitions are accessible

- [ ] **T044** [NEW] Add loading indicator and error states to InrListView
  - Show spinner/activity indicator while loading
  - Show error message with retry button on failure
  - Show "No INR records" message when list is empty
  - Disable refresh button while loading
  - Tests: Verify UI state transitions in `InrListViewStateTests.cs`

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

**Phase 5 — Enhancement & Configuration (NEW)**
- [ ] **T038** [NEW] Implement runtime configuration for mock/real service selection (replacing `#if DEBUG` conditionals) via `appsettings.json` feature flags
  - Create `src/BloodThinnerTracker.Mobile/appsettings.json` with `Features.UseMockServices` flag
  - Update `MauiProgram.cs` to read flag and conditionally register `MockAuthService`/`OAuthConfigService` vs real implementations
  - Update `MauiProgram.cs` to read `Features.OAuthConfigUrl` and `Features.AuthExchangeUrl` from config
  - Benefit: Single binary can run with mock or hosted API without recompilation
  - Enables QA to test with either real backend or local mocks
  - Environment-based config (dev/prod appsettings files)
  - Tests: Add `MauiProgramConfigTests.cs` to verify mock/real service registration based on flags

- [ ] **T039** [NEW] Configure native splash screen for Android/iOS/Windows in `MauiProgram.cs`
  - Define native splash images for each platform in `Resources/Images`
  - Configure splash screen duration and behavior
  - Auto-dismiss behavior during app initialization
  - Benefit: Proper cold-start UX on all platforms (replaces removed SplashView)

- [ ] **T040** [NEW] Implement token refresh mechanism in `AuthService`
  - Add `RefreshAccessTokenAsync()` method to `IAuthService`
  - Implement refresh token storage and rotation
  - Add automatic token refresh before expiration
  - Tests: Add token refresh tests to `AuthServiceTests.cs`

- [ ] **T041** [NEW] Refactor `InrListView` to use lazy factory pattern for ViewModel
  - Create `LazyViewModelFactory<T>` for deferred service initialization
  - Benefit: Better separation of concerns, avoids premature service init
  - Alternative to current code-behind creation approach

- [x] **T042** [COMPLETED] Improve INR list display with better UX
  - ✅ Show status indicator (Normal/Elevated/Low: 2.0-3.0 = Normal, >3.0 = Elevated, <2.0 = Low)
  - ✅ Add refresh button to manually fetch latest data
  - ✅ Show last-updated timestamp (format: "Updated X min/hours/days ago")
  - ✅ Empty state message when no INR data available
  - ✅ Error state with retry button on load failure
  - ✅ Frame-based card layout with status badges (Green/Orange/Red)
  - ✅ Status color mapping: Green for normal, Orange for elevated, Red for low
  - ✅ Tests: Added `InrListItemViewModelTests.cs` with 13 tests covering status indicators, colors, and boundary values
  - **ACTUAL STATE**: InrListView displays INR data in card layout with status badges, refresh button, last-updated text, stale/offline warnings, empty/error states
  - **TEST COVERAGE**: 68 Mobile unit tests passing (13 new tests for status indicators)

- [ ] **T043** [NEW] Implement app theme with sensible color scheme
  - Create `src/BloodThinnerTracker.Mobile/Themes/AppColors.xaml` with color definitions
    - Primary: Medical blue (#0066CC)
    - Accent: Medical red (#DC3545) for elevated INR values
    - Neutral: Gray scale (#333333, #666666, #CCCCCC, #F5F5F5)
    - Status colors: Green (#28A745) for normal, Orange (#FFC107) for warning, Red (#DC3545) for critical
  - Create `src/BloodThinnerTracker.Mobile/Themes/AppStyles.xaml` with default styles for Label, Button, Frame
  - Apply theme globally in `App.xaml`
  - Update all views (LoginView, InrListView, AboutView) to use theme colors
  - Support dark mode with AppThemeBinding
  - Tests: Add `ThemeTests.cs` to verify color definitions are accessible

- [ ] **T044** [NEW] Add loading indicator and error states to InrListView
  - Show spinner/activity indicator while loading
  - Show error message with retry button on failure
  - Show "No INR records" message when list is empty
  - Disable refresh button while loading
  - Tests: Verify UI state transitions in `InrListViewStateTests.cs`

- [ ] **T045** [NEW] Implement full caching and offline-first for INR data
  - Create `src/BloodThinnerTracker.Mobile/Services/CacheService.cs` for encrypted persistent cache
  - Implement encrypted storage using `EncryptionService` + `SecureStorageService` (CachedAt, ExpiresAt metadata)
  - Update `InrListViewModel` to add `LastUpdatedAt` property and track cache age
  - Implement stale-warning logic: show orange banner if cache age > 1 hour; red banner if expired
  - Add refresh button to force fetch latest (clears cache, fetches from API/mock)
  - Add offline-first fallback: show cached data if network unavailable, with "offline" badge
  - Tests: Add `CacheServiceTests.cs` and `InrListViewModelCacheTests.cs` for cache/stale/offline scenarios

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

- Total tasks: 49 (37 original + 8 new Phase 5 enhancement/UI/cache tasks)
- Tasks for US1: 5 (all **COMPLETE** with alternative architecture) ✅
- Tasks for US2: 5 (2 **COMPLETE**, 3 **NOT STARTED** - caching, encryption, stale-warning deferred)
- Tasks for US3: 3 (+1 test task)
- Phase 5 enhancement tasks: 8 (runtime config, native splash, token refresh, lazy factory, INR list UX, theming, loading states, cache/offline-first)
- Parallel opportunities: `MockInrService`, `ApiInrService`, `EncryptionService`, `SecureStorageService`, and unit test tasks

**US1 Status**: ✅ **FUNCTIONALLY COMPLETE**
- See `US1-COMPLETION-SUMMARY.md` for detailed explanation of alternative architecture
- App starts → shows login → OAuth PKCE flow → token exchange → INR list display
- DEBUG mode: Instant mock auth (no setup needed)
- RELEASE mode: Full OAuth flow with real providers
- All 15+ tests passing
- Ready for US2 implementation

**US2 Status**: ✅ **FUNCTIONALLY COMPLETE (MVP with caching)**
- ✅ T018-T019: InrListView displays recent INR values; ViewModel fetches on appear
- ✅ T020: CacheService.cs created with AES-256 encryption, 7-day TTL, metadata tracking
- ✅ T021: Stale warnings UI (orange banner for >1hr old cache), offline mode indicator (gray badge)
- ✅ T022: 30+ unit tests for caching + offline fallback scenarios
- ✅ **ACTUAL STATE**: Displays fresh data with automatic caching + offline fallback; shows staleness warnings
- ✅ Ready for production: Can work offline with cached data, auto-refreshes when online

