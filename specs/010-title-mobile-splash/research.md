# Phase 0 — Research & Decisions

This document records decisions and research outcomes needed to begin Phase 1 design and implementation for the Mobile: Splash + OAuth login + Read-only INR feature.

1) Target framework & tooling
- Platform: .NET 10 (required by project constitution). Use `net10.0` and platform-specific monikers for builds (`net10.0-android`, `net10.0-windows10.0.19041.0`).
- UI Framework: .NET MAUI with `CommunityToolkit.Maui` for controls and helpers.
- MVVM: `CommunityToolkit.Mvvm` for ObservableObjects, RelayCommands and source generators.

2) Authentication (OAuth)
- Flow: OAuth 2.0 Authorization Code with PKCE where supported for mobile. Acquire tokens using the system browser (recommended) and PKCE to avoid embedding client secrets.
- Libraries: Use `Microsoft.Identity.Client` (MSAL) for Azure AD if Azure AD is required. For Google or generic OAuth2 providers, implement an Authorization Code + PKCE flow using system browser + custom redirect URI (app://... or platform-specific scheme) and exchange code for tokens against provider.
- Token Storage: Store refresh tokens/short-lived tokens in platform `SecureStorage`. Access tokens kept in memory and refreshed as needed.

3) Local cache & encryption
- Requirement: AES-256 encrypted cache for health data with 7-day retention and staleness warning after 1 hour.
- Approach:
  - Store INR results JSON in app-local storage, but always encrypted using AES-256 before writing to disk.
  - Derive an AES key per device using platform-provided key storage: generate a random AES key and persist that key in platform `SecureStorage` (which itself uses the device keystore/Keychain). The AES key is never persisted as plain text outside `SecureStorage`.
  - For Windows, use `ProtectedData` or `DataProtectionProvider` (Windows DPAPI) if stronger integration desired; `SecureStorage` is preferred for cross-platform simplicity.
- Libraries: Use `System.Security.Cryptography.Aes` and `AesGcm` where available for authenticated encryption. Use `AesGcm` (recommended) for authenticated encryption to avoid tamper risks.

4) Mock data & development experience
- Implement `IInrService` interface. Provide `MockInrService` that returns canned recent INR results with timestamps and simulated network latency. Register `MockInrService` by default in Debug and when environment variable `USE_MOCK_INR=1` is set.
- Provide a small in-app toggle (developer menu) to switch between Mock and API service for faster local testing.

5) UI behavior & accessibility
- Splash: pulsing logo (animated) for up to 3 seconds or until startup completes. Respect OS reduced-motion setting and provide accessibility label for the logo.
- Login: present OAuth sign-in button that opens system browser. Use return URI + App links to capture callback.
- INR list: read-only list of recent INRTest items sorted desc; show `CachedAt` and `Stale` indicator when data older than 1 hour.

6) CI/CD & publishing
- Build: GitHub Actions matrix for `windows-latest` and `ubuntu-latest` to build MAUI app. Use `dotnet build -f net10.0-android` and `dotnet build -f net10.0-windows10.0.19041.0`.
- Publish: Use GitHub Actions secrets or OIDC + secure workflow to sign Android APK/AAB and submit to Google Play (fastlane or `rakyll/gae` actions). For Microsoft Store, produce MSIX and use `windows-store-publish` or the `vswhere`/MSIX packaging + Store submission action.
- Secrets: do NOT put credentials in repo. Use Actions secrets and prefer short-lived tokens or OIDC where possible.

7) Testing plan highlights
- Unit tests for ViewModels and encryption helpers.
- Integration tests: fake HTTP handler for `ApiInrService` to assert correct auth headers and caching behavior.
- UI tests: validate splash animation (reduced-motion respect), login flow (mocked), and INR list rendering with cached and fresh states.

8) Acceptance risks & mitigations
- OAuth redirect handling differs per platform — mitigate with tested redirect URI patterns for Windows and Android and provide good logging.
- AES key management complexity — keep key lifecycle simple: generate on first run, rotate on explicit sign-out (clear cached data and keys).

Decision summary (explicit):
- Use .NET MAUI + CommunityToolkit.Mvvm (confirmed)
- Use Authorization Code + PKCE OAuth flow, store keys in `SecureStorage` (confirmed)
- Encrypt cached INR results with AES-256 (AesGcm) and retain 7 days, warn after 1 hour (confirmed)
- Provide `IInrService` with `MockInrService` for development (confirmed)

Next steps: produce `data-model.md`, API `contracts/openapi.yaml`, and `quickstart.md`.

Clarification — existing project

The repository already contains a mobile project named `BloodThinnerTracker.Mobile`. It has some existing content; after review you asked to prefer a clean replacement. Therefore the plan will choose to delete the existing `BloodThinnerTracker.Mobile` folder and scaffold a fresh MAUI project with the same name. This destructive operation is considered safe because the repository is under source control and all committed content can be recovered from Git history. Ensure any uncommitted local changes are saved or stashed before proceeding.

Selected approach: Delete-and-scaffold (clean replacement). If you later prefer a non-destructive path, we can switch to renaming to `BloodThinnerTracker.Mobile.bak` instead.
