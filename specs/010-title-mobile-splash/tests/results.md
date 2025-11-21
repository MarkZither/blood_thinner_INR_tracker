# Acceptance Test Results

Use this file to record test runs and evidence. Copy a row for each test execution.

| Test ID | Date | Tester | Environment | Steps Performed | Expected Result | Actual Result | Pass/Fail | Evidence (screenshots/logs) | Notes |
|--------:|------:|-------|-------------|----------------:|----------------:|--------------:|:---------:|----------------------------|------|
| AT-001 | 2025-11-21 | | emulator/device | Cold-start -> observe login | Median <= 3s | | | | |
| AT-002 | 2025-11-21 | | emulator/device | Login -> observe INR list | Median <= 10s | | | | |
| AT-003 | 2025-11-21 | | CI / test fixture | Automated N runs | Success rate >= 95% | | | | |
| AT-004 | 2025-11-21 | | device with reduced motion | Launch with reduced motion | No pulsing animation | | | | |
| AT-005 | 2025-11-21 | | emulator/device | Empty/offline/auth-fail scenarios | Correct user message shown | | | | |

Instructions
- For each run, fill in the Actual Result, mark Pass/Fail, and link to screenshots or logs (store artifacts under `specs/010-title-mobile-splash/tests/artifacts/`).
