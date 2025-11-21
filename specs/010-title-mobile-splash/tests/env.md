# Test Environment & Runbook

Prerequisites
- .NET SDK (10.0.x) with MAUI workloads installed (for MAUI prototype builds)
- Android Emulator (API 29+) or iOS Simulator (Xcode required on macOS)
- Device with network connectivity to run timing tests (or emulator network throttling to simulate 4G)
- Tools: `adb` (Android), `dotnet`, and optionally Appium or MAUI Test for UI automation

Environment variables (examples)
- `ASPNETCORE_ENVIRONMENT=Development`
- `API_BASE_URL` — base URL of test API or mock server (e.g., https://test-api.local)
- `OAUTH_CLIENT_ID` — OAuth client id for test identity provider
- `OAUTH_REDIRECT_URI` — configured redirect uri for mobile OAuth flow

Test accounts & API fixtures
- Use a dedicated test user account. Do NOT use production user credentials.
- If possible, run a mock/test API instance that returns deterministic INR results and supports the following endpoints:
  - `GET {API_BASE_URL}/api/inr/recent` — returns recent INR results for authenticated user
  - `POST {API_BASE_URL}/auth/token` (or OAuth flow) — test auth (see OAuth test instructions below)

OAuth testing notes
- For OAuth flows, prefer using a test identity provider or a test tenant (Azure AD test app registration or Google test client).
- Configure `OAUTH_REDIRECT_URI` and client ID to accept app redirect (use system browser flow for mobile if required).

Simulating conditions
- Network throttling (simulate 4G): use emulator network settings or tools such as `tc` on Linux hosts, or the Android emulator network throttling UI.
- Offline: disable network on emulator/device or set airplane mode.
- Empty data: configure mock API to return empty arrays for `inr/recent`.

Collecting evidence
- Capture screenshots via emulator controls or `adb exec-out screencap`.
- Capture logs: use `adb logcat` for Android, Xcode Console for iOS, and app logs (console) from the MAUI app.
