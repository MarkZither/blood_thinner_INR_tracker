# Playwright E2E guidance for `BloodThinnerTracker.Web.e2e.Tests`

This folder contains the Playwright E2E test project. Use the existing `BloodThinnerTracker.Web.e2e.Tests` project for browser-based end-to-end tests.

Suggested steps:
1. Ensure Node.js and Playwright are installed on CI runner.
2. Add a `package.json` in this directory with Playwright dev dependency and test scripts.
3. Use `npx playwright test` in CI to run tests against a deployed test host.

Notes: Keep Playwright tests in this dedicated e2e project. Do not duplicate Playwright content under `Web.Tests` which is reserved for bUnit component/unit tests.
