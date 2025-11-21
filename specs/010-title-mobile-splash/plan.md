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

**Structure Decision**: Use a single MAUI app project `Mobile.BloodThinnerTracker` under `src/` with a services folder to host `IInrService`. Provide two implementations: `ApiInrService` (calls remote API) and `MockInrService` (in-memory mock) registered at startup by environment/DI. Tests live under `tests/` matching MAUI project structure.

## Complexity Tracking

No constitution violations. This plan follows the Constitution's guidance (options pattern, encryption, testing). Any additional scope (store publication automation details, Play Store signing, MSIX packaging) will be captured as discrete tasks in Phase 2 to keep feature size small.

````
