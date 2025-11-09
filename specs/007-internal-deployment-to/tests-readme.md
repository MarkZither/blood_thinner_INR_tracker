# Test scaffolding summary

This file summarizes the test scaffolds created for the internal deployment feature.

- `tests/DeployScripts.Tests/` — Pester tests to validate deploy script structure and basic assertions (no remote actions).
- `tests/BloodThinnerTracker.Api.Tests/` — xUnit project skeleton for backend unit/integration tests.
- `tests/playwright/` — placeholder guidance for Playwright E2E tests.
- `tests/bunit/` — placeholder guidance for BUnit component tests.

Next steps:
- Implement actual Pester tests that mock SSH/scp and assert script behavior.
- Implement xUnit tests targeting backend logic that the deployment affects (e.g., health endpoint tests using TestServer).
- Add CI jobs to run Pester and dotnet test.
