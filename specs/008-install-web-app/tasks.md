# tasks.md

## Feature: Install web app shortcut (Phase 1)

Phase structure:
- Phase 1: Setup
- Phase 2: Foundational (blocking validations & docs)
- Phase 3: User Stories (P1 then P2)
- Final Phase: Polish & cross-cutting

Phase 1 - Setup

- [X] T001 [P] Create placeholder icons in `src/BloodThinnerTracker.Web/wwwroot/icons/icon-192.svg` and `src/BloodThinnerTracker.Web/wwwroot/icons/icon-512.svg` (added SVG placeholders)
- [X] T002 [P] Create `src/BloodThinnerTracker.Web/wwwroot/manifest.webmanifest` with required fields (name, short_name, start_url, display, theme_color, icons)
- [X] T003 [P] Create `src/BloodThinnerTracker.Web/wwwroot/service-worker.js` with whitelist-only caching strategy for static assets (do NOT cache API paths)
- [X] T004 Add registration snippet and manifest link to the host page: updated `src/BloodThinnerTracker.Web/Components/Layout/MainLayout.razor` to inject `<link rel="manifest" href="/manifest.webmanifest">` and register SW
- [X] T005 [P] Add small CSS/JS placeholders referenced by SW assets: `src/BloodThinnerTracker.Web/wwwroot/css/app.css` and `src/BloodThinnerTracker.Web/wwwroot/js/app.js`

Phase 2 - Foundational (blocking validations & docs)

 - [X] T006 [ ] Update `specs/008-install-web-app/quickstart.md` to ensure copy matches implemented manifest and SW paths (verify content)
- [X] T007 [ ] Add Playwright smoke test to verify manifest and service worker registration: `tests/BloodThinnerTracker.Web.e2e.Tests/InstallabilityTests.cs` (C# Playwright test that asserts manifest served)
 - [X] T008 [ ] Security review task: validate `service-worker.js` does not cache API endpoints and add comment in file `src/BloodThinnerTracker.Web/wwwroot/service-worker.js` documenting allowed paths
 - [X] T009 [ ] Documentation: Update `specs/008-install-web-app/research.md` with final test findings and any platform caveats discovered during implementation

Phase 3 - User Stories (implement per priority)

User Story 1 - Create shortcut on Android home screen (Priority: P1)

- [X] T010 [US1] Create inline contextual help component `src/BloodThinnerTracker.Web/Shared/InstallHelp.razor` (modal/popover) with Android instructions and links to long-form help
- [X] T011 [US1] Populate Android instruction text in `src/BloodThinnerTracker.Web/Shared/InstallHelp.razor` (copy-ready steps: open menu -> Add to Home screen)
- [X] T012 [US1] Add link/button to surface the `InstallHelp` component in header: update `src/BloodThinnerTracker.Web/Shared/MainLayout.razor` (or equivalent) to include a visible affordance
- [x] T013 [US1] Manual verification checklist: add `specs/008-install-web-app/tests/manual-android.md` with step-by-step verification and expected results

User messaging & tests

- [x] ~~T014 [USx] Implement user-facing success/failure messaging (snackbar/toast) in `src/BloodThinnerTracker.Web/Shared/InstallHelp.razor` to show confirmations like "Shortcut added to home screen" and actionable errors. Wire message display to UI triggers.~~ REMOVED this is provided by the browser
- [X] T015 [Test] Add BUnit unit test for `InstallHelp.razor` at `tests/BloodThinnerTracker.Web.Tests/InstallHelpTests.cs` to assert messaging behavior (success and failure cases).

User Story 2 - Create shortcut on iOS home screen (Priority: P1)

- [X] T020 [US2] Add iOS-specific instruction text to `src/BloodThinnerTracker.Web/Shared/InstallHelp.razor` (Share -> Add to Home Screen instructions for Safari)
- [X] T021 [US2] Manual verification checklist: add `specs/008-install-web-app/tests/manual-ios.md` with step-by-step verification and expected results

User Story 3 - Create desktop shortcut on Windows (Priority: P1)

- [X] T030 [US3] Add Windows-specific instruction text to `src/BloodThinnerTracker.Web/Shared/InstallHelp.razor` (how to create desktop shortcut via browser/OS actions)
- [X] T031 [US3] Manual verification checklist: add `specs/008-install-web-app/tests/manual-windows.md` with step-by-step verification and expected results

User Story 4 - Manage or remove created shortcut (Priority: P2)

- [ x ] ~~T040 [US4] Add removal/uninstall instructions to `src/BloodThinnerTracker.Web/Shared/InstallHelp.razor` (per-platform removal guidance)~~ REMOVED this is built in ok functionality
- [x] ~~T041 [US4] Add acceptance scenario test file `specs/008-install-web-app/tests/manual-remove.md` describing removal flows and expected outcomes~~ REMOVED this is built in ok functionality

Final Phase - Polish & cross-cutting concerns

 - [X] T050 [X] Add comment headers and XML doc where appropriate in modified files to satisfy code conventions: `src/BloodThinnerTracker.Web/wwwroot/service-worker.js`, `src/BloodThinnerTracker.Web/Pages/_Host.cshtml`, `src/BloodThinnerTracker.Web/Shared/InstallHelp.razor`
 - [X] T051 [X] Add Playwright job to CI: create `.github/workflows/playwright-install.yml` that runs the installability smoke test (optional manual gating)
 - [X] T052 [P] Update `specs/008-install-web-app/checklists/requirements.md` to mark verification steps complete and record test outcomes after implementation

Infra examples

- See `specs/008-install-web-app/k3s/` for k3s/ExternalSecrets and SealedSecrets examples for Azure Key Vault integration.
- See `specs/008-install-web-app/docker/` for a Docker Compose example that mounts host secret files and a simple `docker-entrypoint.sh` which exports them as environment variables for the container.

Dependencies & order

- Setup tasks T001..T005 should run first (icons, manifest, SW, host registration).
- Foundational tasks T006..T009 depend on successful Setup completion.
- User Story phases (T010..T041) can begin after Setup; UI tasks that alter layout should be scheduled after host page changes (T004).
- Parallel opportunities: icon creation (T001), manifest (T002), service-worker (T003), and asset placeholders (T005) are parallelizable. Marked with [P].

Metrics & validation

- Each User Story phase must include its manual verification checklist file under `specs/008-install-web-app/tests/` and at least one Playwright smoke test validating manifest presence and SW registration.

Summary

- Total task count: 22
- Tasks per story: US1=4, US2=2, US3=2, US4=2, Setup/Foundational/Final=12
- Parallel opportunities: T001, T002, T003, T005, T052
