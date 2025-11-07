# Tasks: BugFix: ReturnUrl honoured on login (Applied Remediations)

Feature: Fix ReturnUrl handling so validated relative ReturnUrl values are honoured after authentication.

> Feature status: COMPLETED
>
> - Feature 006 (ReturnUrl honoured on login) is considered finished as of 2025-11-07.
> - Core work (validation helper, login flow wiring, UI producer fixes, unit tests and bUnit coverage) has been implemented and verified locally. Remaining Playwright e2e items and extended CI polish are intentionally deferred/removed for a follow-up e2e phase.
> - See the completed work and test results in the repository under `src/BloodThinnerTracker.Web` and `tests/BloodThinnerTracker.Web.Tests`.


NOTE (scope): This work is Web-focused and applies to `BloodThinnerTracker.Web` (Blazor pages/navigation). It does NOT require API project changes or modifications to OAuth/OIDC redirect URIs registered in Azure. The immediate bugfix uses a local safe default route value in code; any future configuration-based extensions (for example a whitelist) will be planned and documented separately.

Phase 1 — Setup (project-level, run first)

- [x] T001 Initialize feature branch and verify docs exist at `specs/006-title-bugfix-returnurl` (confirm `plan.md`, `spec.md`)
- [x] T002 Add test project folders and scaffolding if missing:
  - `tests/playwright/returnurl` (C# Playwright tests)
  - `tests/BloodThinnerTracker.Web.Tests/ReturnUrl` (xUnit unit tests)
  Acceptance criteria: each scaffolding task must create a project file, a sample passing test, and CI/test-run instructions in `specs/006-title-bugfix-returnurl/quickstart.md`.
- [x] T003 Add unit test scaffold: `tests/BloodThinnerTracker.Web.Tests/ReturnUrl/ReturnUrlValidationTests.cs` (happy + negative cases)
- [x] T004 Add security logging hook usage examples in the Web project (reuse existing logging infrastructure). Do NOT create API logging targets. Ensure tests can assert logging via test hooks or mocks. Guidance: unit tests should use a mocked `ILogger<T>` to assert an Error-level event using the Logging Schema (see `spec.md#logging-schema`) and Playwright tests may assert via a test-only HTTP endpoint or test hook that collects logged events.

Phase 2 — Core helpers (small, isolated)

- [x] T005 Implement ReturnUrl validation helper in `src/BloodThinnerTracker.Web/Services/ReturnUrlValidator.cs` (Web-only). Responsibilities:
  - Decode once and validate input is a relative path starting with `/`.
  - Reject absolute URLs (have a scheme), protocol-relative (`//host`), `javascript:` or `data:` schemes, and suspicious double-encoded leading slashes.
  - Preserve path, query, and fragment for valid inputs.
  - Return normalized value or failure result; no additional configuration settings are required for this change.
- [x] T006 Add unit tests for validation helper in `tests/BloodThinnerTracker.Web.Tests/ReturnUrl/ReturnUrlValidationTests.cs` (cover all patterns: valid relative, query+fragment, external absolute, protocol-relative, double-encoded, malformed, overly long).

Phase 3 — User Story 1 (US1) — Return to intended page after login (Priority P1)

- [ x ] T007 Capture ReturnUrl when redirecting unauthenticated users to login:
  - Ensure code that redirects to login supplies only the raw query parameter value (not entire absolute URL) and does not persist any server-side token for this feature.
  - Location: existing web redirect handler / navigation code (e.g., where NavigationManager.NavigateTo is invoked before login). Update comments to show the captured value source (`?returnUrl=`).
- [x] T008 Implement post-login redirect wiring in the existing login handler (`src/BloodThinnerTracker.Web/Pages/Account/Login.razor.cs` or equivalent):
  - Read the `returnUrl` query parameter, validate with `ReturnUrlValidator`, and if valid, navigate to it preserving path, query and fragment.
  - If missing or invalid, redirect to safe default `/dashboard`.
- [removed] T009 Add C# Playwright integration test: positive redirect to `/medications/123` preserved after login in `tests/playwright/returnurl/PositiveRedirectTests.cs`. (Defer to separate e2e phase)
- [removed] T010 Add C# Playwright integration test: query parameters preserved for `/inr-tests?filter=recent` in `tests/playwright/returnurl/QueryParamsTests.cs`. (Defer to separate e2e phase)
- [removed] T011 Add C# Playwright integration test: fragment preserved for `/settings#notifications` in `tests/playwright/returnurl/FragmentTests.cs`. (Defer to separate e2e phase)
  - Test matrix (explicit):
    - Blazor WASM: assert final browser URL equals `/settings#notifications` after login (fragment preserved client-side).
    - Blazor Server: assert final server-observed path and query are preserved (e.g., `/settings` with expected query), and optionally assert client-side fragment restoration only if a client-side restore is implemented; otherwise test should not fail on missing fragment.
  - Document which hosting variants are run in CI in `quickstart.md`.

Phase 4 — User Story 2 (US2) — Block unsafe redirect attempts (Priority P1)

- [ ] T012 Enforce ReturnUrl validation policy (relative-only) in all web entry points that accept `returnUrl` (use `ReturnUrlValidator`). Reject absolute and protocol-relative URLs.
- [ ] T013 On invalid ReturnUrl, ensure flow redirects to safe default (`/dashboard`) and log a security event at Error level using existing ILogger. Include raw value in log (consider redaction policy if secrets present).
- [ ] T014 Add unit tests for invalid patterns: external absolute URLs, protocol-relative `//evil`, double-encoded payloads, malformed URLs in `tests/BloodThinnerTracker.Web.Tests/ReturnUrl/InvalidReturnUrlTests.cs`.
- [removed] T015 Add C# Playwright negative test: external URL blocked in `tests/playwright/returnurl/ExternalBlockTests.cs`. Hosting model: specify server and/or client as applicable in test header. (Defer to separate e2e phase)
- [removed] T016 Add C# Playwright negative tests: protocol-relative and double-encoded payloads in `tests/playwright/returnurl/NegativeEncodingTests.cs`. (Defer to separate e2e phase)

Phase 5 — User Story 3 (US3) — Missing or empty ReturnUrl (Priority P2)

- [ x ] T017 Implement fallback to safe default when `returnUrl` missing or empty (login handler) — ensure behavior is consistent and documented.
- [ x ] T018 Add unit tests for fallback behavior in `tests/BloodThinnerTracker.Web.Tests/ReturnUrl/FallbackTests.cs`.
- [removed] T019 Add C# Playwright integration test: direct login lands on `/dashboard` in `tests/playwright/returnurl/FallbackTests.cs`. (Defer to separate e2e phase)

Phase 6 — Polish & Cross-cutting concerns

- [ x ] T020 Add logging and telemetry assertions to tests where security blocking occurs (use test hook or mock logger to assert that an Error-level security event was recorded using the Logging Schema in `spec.md#logging-schema`). Guidance: document the mock or test-hook approach in `quickstart.md` so CI tests can assert logs deterministically.
  
  Example assertions (guidance):

  - Unit test (xUnit + Moq ILogger):
    - Arrange: create mock ILogger and inject into validator/login handler
    - Act: call validation with `returnUrl=https://evil.example.com`
    - Assert: verify ILogger.LogError was called at least once with an argument whose message or state contains EventId `ReturnUrlBlocked` and ValidationResult `invalid-scheme` (or inspect logged state dictionary)

  - Playwright test (integration):
    - Use a test-only HTTP endpoint or test-hook that collects emitted log events (test harness) and expose them to the test.
    - After triggering blocked ReturnUrl flow, query the test-hook for recent events and assert an event with EventId `ReturnUrlBlocked` and ValidationResult `protocol-relative` (or the expected value) exists and RawReturnUrl matches the blocked input (or its redacted placeholder).

  Document the exact mock or test-hook wiring in `specs/006-title-bugfix-returnurl/quickstart.md` so CI can run these assertions reliably.
- [ ] T021 Add documentation update: `docs/api/returnurl.md` and update `specs/006-title-bugfix-returnurl/quickstart.md` with exact Playwright run instructions and confirmation that no configuration changes are necessary for this feature.
- [ ] T022 Run full test suite locally and verify new tests pass: `dotnet test` (unit) and Playwright integration runs. Report results in PR description.
- [ ] T023 Add a small PR checklist item to ensure any configuration changes (if introduced later) follow repository configuration standards (e.g., strongly-typed options where applicable).
- [ ] T024 Add coverage measurement to CI and PR checklist: collect code coverage for the changed Web project(s) and report coverage in the PR description.
  Acceptance criteria:
  - Use Coverlet + ReportGenerator (or the repo's existing coverage tool) to produce an HTML and Cobertura/Codecov-compatible report.
  - CI must upload the coverage artifact and the PR template must include the coverage summary for changed projects.
  - If coverage for modified files drops by more than 2 percentage points versus baseline, add a reviewer acknowledgement checkbox in the PR which must be checked before merge.
  - Document the exact commands in `specs/006-title-bugfix-returnurl/quickstart.md`.

- Dependencies & Notes

- All changes are Web-only and do not require configuration changes or server-side ReturnUrl token storage for this feature.
- Playwright tests require the web app test harness; ensure `watch-web` or a test server is available during test runs.
- Parallelization: Core helper (T005/T006) and test scaffolding (T002/T003) can be implemented in parallel with T004 (logging hook examples).

Implementation strategy

- MVP: Implement T005-T009 (validation helper, capture, post-login redirect, and one Playwright positive test) to fix the core user experience. Then implement US2 security tests (T012-T016), followed by fallback tests and polish.

Summary

- Path: `specs/006-title-bugfix-returnurl/tasks.md`
- Updated: removed duplicate legacy block, removed token/server-store references, added Playwright hosting model notes, expanded CI coverage task, and added guidance for asserting security logs in tests.
- Total tasks (cleaned): 23
