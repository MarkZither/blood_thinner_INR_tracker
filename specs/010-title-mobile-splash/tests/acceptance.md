# Acceptance Tests: Mobile Splash / Login / INR Read-only

This document maps each Success Criterion from the spec to concrete, testable acceptance tests.

Test conventions
- Run each timing-sensitive test 5 times on a representative device/network and report median and max.
- Use the test account(s) or a test API fixture as described in `env.md`.
- Record evidence (screenshots, logs) and add entries to `results.md`.

AT-001 (SC-001) — Splash timing
- Goal: Users reach login or main screen within 3 seconds on a typical mobile network.
- Steps:
  1. Ensure device/emulator network is set to a typical mobile profile (4G) or normal Wi-Fi.
  2. Launch the app from cold start (no process running).
  3. Start timer at process start; stop timer when the login screen or main screen is fully visible (first interactive frame).
  4. Repeat 5 times, record median and max.
- Pass criteria: median <= 3 seconds.
- Evidence: screenshots of splash and login/main screen, timing log entries.

AT-002 (SC-002) — INR list display after login
- Goal: Authenticated users see the most recent 5 INR results within 10 seconds of submitting credentials.
- Steps:
  1. Start with app at login screen.
  2. Submit credentials using test user (see `env.md`).
  2.a Verify OAuth/OIDC flow: ensure the OIDC callback returns an `id_token` to the app (or capture proof of id_token receipt via the test harness).
  2.b Exchange step: verify the app POSTs the `id_token` to `POST /auth/exchange` and receives an internal bearer token; capture response.
  3. Start timer when Submit is tapped, stop when the INR list is rendered and shows at least one item or the empty state.
  4. Repeat 5 times, record median and max.
- Pass criteria: median <= 10 seconds.
- Evidence: network logs showing API call, timing logs, screenshot of INR list.

AT-003 (SC-003) — Render success rate
- Goal: When the API returns data, 95% of valid requests render the INR list without user-visible errors.
- Steps (automated preferred):
  1. Create an automated script or test that performs the login flow and fetches the INR list N=50–100 times (or N≥50 depending on available infra).
  2. Count successful renderings vs failures (UI error message, missing list, or crash).
- Pass criteria: success rate >= 95%.
- Evidence: automated test run logs, pass/fail counts, screenshots for failures.

AT-004 (SC-004) — Reduced motion accessibility
- Goal: When device requests reduced motion, splash animation is disabled.
- Steps:
  1. Enable OS-level "Reduce Motion" on device/emulator.
  2. Launch the app from cold start.
  3. Observe the splash screen; verify logo is static (no pulsing animation).
- Pass criteria: No animation observed when reduced motion is enabled.
- Evidence: short screen recording or pair of screenshots, device accessibility setting confirmation.

AT-005 (SC-005) — Fallback states (empty data, offline, auth failure)
- Goal: App shows clear fallback states for empty data, offline/no-network, and authentication failures.
- Steps:
  1. Empty data: Configure the API or mock to return an empty INR list for the test user; launch app, authenticate, and confirm the empty-state message appears.
  2. Offline: Disable network on device/emulator; launch app and attempt to view INR list; confirm offline message or cached data behavior appears and staleness warning if cached >1 hour.
  3. Auth failure: Supply invalid credentials and verify the actionable error message is displayed.
- Pass criteria: Each scenario shows the exact user-facing message described in the spec.
- Evidence: screenshots, network mock configuration, logs.

Notes
- For automated tests that exercise latency and success rate, prefer running against a test fixture / mock API to avoid production variability.
- For timing tests, ensure the device is stable (no background updates) and use the same device/emulator profile across runs.
