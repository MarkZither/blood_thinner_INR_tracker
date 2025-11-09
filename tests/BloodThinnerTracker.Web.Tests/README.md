# bUnit tests for Blazor Web UI

This project (`tests/BloodThinnerTracker.Web.Tests`) is the canonical location for bUnit Blazor component tests.

Guidance:
- Use bUnit for component-level tests and keep tests that rely on Blazor lifecycles here.
- E2E browser tests (Playwright) belong in `tests/BloodThinnerTracker.Web.e2e.Tests`.
- Keep this project focused on unit/component tests and avoid adding Playwright or Node artifacts here.
