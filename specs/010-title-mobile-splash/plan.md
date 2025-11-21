````markdown
# Implementation Plan: Mobile: Splash + OAuth login + Read-only INR (MAUI)

**Branch**: `010-title-mobile-splash` | **Date**: 2025-11-21 | **Spec**: `C:\Source\github\blood_thinner_INR_tracker\specs\010-title-mobile-splash\spec.md`
**Input**: Feature specification from `C:\Source\github\blood_thinner_INR_tracker\specs\010-title-mobile-splash\spec.md`

## Summary

Build a cross-platform .NET MAUI mobile client (initial targets: Windows and Android) that shows a pulsing splash screen, supports OAuth 2.0 sign-in to configured identity providers, and displays a read-only list of the user's recent INR test results. The client will use MVVM Toolkit from CommunityToolkit for architecture, a mock data source in development, and AES-256 encrypted local caching for offline viewing (7-day retention, 1-hour staleness warning). CI/CD pipelines (GitHub Actions) will build and publish artifacts to Google Play and Microsoft Store using secure credentials/secret management.

## Technical Context

**Language/Version**: .NET 10 (C# 13)
**Primary Dependencies**:
- `Microsoft.Maui` (MAUI app)
- `CommunityToolkit.Mvvm` (MVVM Toolkit)
- `CommunityToolkit.Maui` (controls/helpers)
- `Refit` or lightweight HTTP client wrapper for API calls (optional)
- `Microsoft.Maui.Storage` / `SecureStorage` for secure key storage
- `System.Security.Cryptography` for AES-256 encryption
- `Moq` / in-memory mock services for development/testing
**Storage**:
- Local: encrypted cache (AES-256) stored via platform secure storage; cache metadata persisted as JSON in app-local storage.
- Remote: REST API (authenticated via OAuth bearer tokens) — mock API used for local development.
**Authentication / Token Exchange**:
- Mobile performs Authorization Code + PKCE (OIDC) against external providers (Azure, Google). The provider returns an `id_token` to the mobile client via the OIDC callback.
- The mobile client MUST POST the `id_token` to a backend token-exchange endpoint (e.g., `POST /auth/exchange`) to obtain an internal bearer token used for API requests. The backend MUST validate the `id_token` (issuer, audience, signature, expiry) and may apply additional checks (scopes, account status) before issuing the internal bearer token.
- The mobile client stores the internal bearer token in platform `SecureStorage` and uses it for authenticated API calls.
**Testing**:
- Unit tests: xUnit (shared library)
- UI tests: MAUI Test (or Appium) for cross-platform automation
- Contract/Integration: simple API-level tests using HTTP test fixtures
**Target Platform**: Windows (desktop MAUI), Android (emulator / Play Store); future: iOS
**Project Type**: Mobile app (MAUI) with optional local mock service for development
**Performance Goals**:
- Cold start to login/main screen: median <= 3s on target devices (SC-001)
- Post-login INR list render: median <= 10s (SC-002)
**Constraints**:
- Must use AES-256 encryption for cached health data (Constitution requirement)
- Must respect device reduced-motion accessibility setting
- Code must pass repository constitution gates (StyleCop, analyzers, tests)
**Scale/Scope**:
- Initially single-user-per-device client with small local cache; server scale out not required for this client feature

## Constitution Check

GATE: This plan must adhere to the project's Constitution principles. Key checks:

- Configuration: Use strongly-typed options pattern for OAuth endpoints, API base URL, and feature flags (CONFORM)
- Security: AES-256 for cached health data; OAuth for authentication; TLS 1.3 for API (CONFORM)
- Testing: Plan includes unit and UI tests (CONFORM); aim for required coverage in feature code
- UI: MAUI + CommunityToolkit and accessibility support (CONFORM)
- Deployment: GitHub Actions with secure secrets / OIDC for publishing (CONFORM)

No constitution violations detected for the described approach. If additional integrations are requested (e.g., device-native SSO beyond OAuth), re-evaluate gates.

## Project Structure

```
src/
├── BloodThinnerTracker.Mobile/            # MAUI app (Windows + Android) — existing project (may be replaced)
│   ├── App.xaml.cs
│   ├── Views/
│   ├── ViewModels/
│   ├── Services/
│   │   ├── IInrService.cs                 # interface
│   │   ├── ApiInrService.cs               # real HTTP implementation
│   │   └── MockInrService.cs              # development mock
│   └── Models/
└── tests/
    ├── Mobile.UnitTests/                  # xUnit tests for ViewModels and services
    └── Mobile.UITests/                    # Appium or MAUI Test UI tests

specs/010-title-mobile-splash/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── openapi.yaml
└── tests/
    ├── acceptance.md
    ├── env.md
    └── results.md
```

**Structure Decision**: Use a single MAUI app project `BloodThinnerTracker.Mobile` under `src/` with a services folder to host `IInrService`. Provide two implementations: `ApiInrService` (calls remote API) and `MockInrService` (in-memory mock) registered at startup by environment/DI. Tests live under `tests/` matching MAUI project structure.

**Repository safety note**: The implementation will perform a clean scaffold of `src/BloodThinnerTracker.Mobile` and the plan's tasks (see `tasks.md`) intentionally include deleting the existing folder. This is considered safe because the repository is version-controlled and all current content is recoverable from Git history. Ensure any uncommitted local changes are saved or stashed before running destructive steps.

**Branch naming (Constitution compliance)**: The project Constitution mandates branch names use the `feature/NNN-short-description` pattern. Current feature branch naming should be aligned accordingly (for example: `feature/010-mobile-splash`). To rename the local branch safely, use:

```
git branch -m 010-title-mobile-splash feature/010-mobile-splash
git push origin :010-title-mobile-splash
git push -u origin feature/010-mobile-splash
```

If you prefer to preserve the current branch name, a formal constitution amendment is required (see Constitution/Governance). Please choose which path before running destructive scaffold steps.

**Testing policy exception (MAUI native UI)**: The repository Constitution prescribes Playwright for UI automation for web/Blazor surfaces. Native MAUI mobile UI automation is not Playwright-native. This plan documents a formal exception to allow platform-appropriate native UI tooling (MAUI Test or Appium) for MAUI client automated UI tests. The exception must be recorded in the feature's acceptance notes and the CI pipeline. If you prefer to enforce the Constitution strictly, we will instead limit Playwright to web surfaces and defer native UI automation until a constitution amendment is ratified.

CI implications for native UI automation:

- **Runner requirements**: Native UI tests require platform-specific runners: Android emulator support is available on GitHub-hosted Linux/Windows runners (via `actions/setup-android` and emulator start), while Windows MAUI UI tests require Windows runners (GitHub `windows-latest`) or self-hosted Windows runners for reliable UI automation. Consider device-farm options (Firebase Test Lab, AppCenter, or cloud device farms) for broad device coverage.
- **Emulator setup**: CI jobs must install SDK components, start an emulator, and wait for readiness. Include reproducible emulator images and network profiles for performance tests.
- **Test artifacts**: CI should collect UI test logs, screenshots, and video artifacts for failed runs. Store artifacts in workflow run or upload to an artifacts bucket for debugging.
- **Security & secrets**: CI runs that exercise authentication flows must avoid using real production credentials. Use staging identity providers or mocked OIDC endpoints and inject credentials via repository secrets or OIDC where necessary.
- **Coverage & gating**: Add CI workflow steps to run unit tests and UI tests, collect coverage metrics, and gate merges on required coverage thresholds (see tasks T035/T036 added to `tasks.md`).

Implementation tasks (CI-focused) will include:

- Add a CI workflow to provision Android emulators and run MAUI Test/Appium UI suites on `windows-latest` / `ubuntu-latest` as appropriate.
- Add steps to capture artifacts (screenshots, logs, traces) on failure.
- Add workflow to run performance telemetry (cold-start, render timing) under a controlled network profile and publish metrics.

## Complexity Tracking

No constitution violations. This plan follows the Constitution's guidance (options pattern, encryption, testing). Any additional scope (store publication automation details, Play Store signing, MSIX packaging) will be captured as discrete tasks in Phase 2 to keep feature size small.

````
