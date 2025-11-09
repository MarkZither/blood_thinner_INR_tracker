# Tasks: Internal deployment to Raspberry Pi and Windows bare metal

Feature: Internal deployment to Raspberry Pi (ARM64) and Windows (x64) with VPN-limited external access
Spec: `specs/007-internal-deployment-to/spec.md`
Plan: `specs/007-internal-deployment-to/plan.md`

NOTE: Several tasks reference helper scripts and templates that do not yet exist in the repo; those files will be created in the scope of this work (see foundational tasks T005-T010 and story tasks that create scaffolds).

## Phase 1 — Setup (project initialization)

- [ ] T001  Ensure repository prerequisites script is runnable and documents `FEATURE_DIR` in JSON: `.specify/scripts/powershell/check-prerequisites.ps1`
- [ ] T002  Create `specs/007-internal-deployment-to/tasks.md` (this file) using the spec and plan as source artifacts: `specs/007-internal-deployment-to/plan.md`
- [ ] T003  Verify existence and canonical paths for required docs: `specs/007-internal-deployment-to/spec.md`, `specs/007-internal-deployment-to/research.md`, `specs/007-internal-deployment-to/data-model.md`, `specs/007-internal-deployment-to/contracts/openapi.yaml`
- [ ] T004  Add brief README in `specs/007-internal-deployment-to/README.md` linking the spec, plan, quickstart and tasks files

## Phase 2 — Foundational (blocking prerequisites)

- [ ] T005  [P] [US1] Confirm .NET 10 publish profile for API (linux-arm64) exists or add publish script update existing `tools/deploy-to-pi.ps1`
- [ ] T006  [P] [US2] Confirm .NET 10 publish profile for Web (linux-arm64) exists or add publish script update existing `tools/deploy-to-pi.ps1`
 - [X] T005  [P] [US1] Confirm .NET 10 publish profile for API (linux-arm64) exists or add publish script update existing `tools/deploy-to-pi.ps1`
 - [X] T006  [P] [US2] Confirm .NET 10 publish profile for Web (linux-arm64) exists or add publish script update existing `tools/deploy-to-pi.ps1`
- [ ] T007  [P] Create a small validation script that checks host prerequisites on RPi update existing `tools/deploy-to-pi.ps1` — checks disk, .NET runtime, available ports
- [ ] T008  Create a Windows host prerequisite checker PowerShell script: update existing `tools/deploy-windows-baremetal.ps1` — checks .NET 10 presence, disk, ports, firewall admin
- [ ] T009  Create canonical config template files and examples for deployments: `tools/deployment.config.example.json`
- [ ] T010  Add host service templates for systemd and Windows (nssm/sc): `tools/templates/systemd-service.example` and `tools/templates/windows-service-notes.md`
 - [X] T010  Add host service templates for systemd and Windows (nssm/sc): `tools/templates/systemd-service.example` and `tools/templates/windows-service-notes.md`

## Phase 3 — User Story Phases (priority order)

### User Story 1 — Deploy API to Raspberry Pi (Priority: P1)
Goal: Produce a published API artifact for linux-arm64, install it on an RPi, register a systemd service, and verify the health endpoint.

Independent test criteria: Publish artifact -> copy to RPi -> install/start service -> GET http://<host>:5000/health returns 200

- [ ] T011  [P] [US1] Publish API artifact for linux-arm64 from project root: update or add `tools/publish-api-arm64.ps1` referencing the API project (file path in repo)
 - [X] T011  [P] [US1] Publish API artifact for linux-arm64 from project root: update or add `tools/publish-api-arm64.ps1` referencing the API project (file path in repo)
- [ ] T012  [US1] Create or update operator deployment script for RPi: `tools/deploy-to-pi.ps1` — copy artifact to `~/deployments/api/<version>` and set permissions
- [ ] T013  [US1] Add systemd service file template for the API on RPi in `tools/templates/systemd-service.example` and document installation steps in `specs/007-internal-deployment-to/quickstart.md`
- [ ] T014  [US1] Implement smoke-check script on operator host to call the health endpoint and assert 200: `specs/007-internal-deployment-to/quickstart-smoke.sh`
- [ ] T015  [US1] [P] Create a verification checklist in `specs/007-internal-deployment-to/quickstart.md` with exact commands to validate service start and logs (journalctl commands)
- [ ] T016  [US1] Add log rotation guidance and default paths to `docs/deployment/internal-deployment.md` and `specs/007-internal-deployment-to/data-model.md` (artifact & log paths)
- [ ] T017  [US1] [P] Add disk-space check into `tools/deploy-to-pi.ps1` and fail with clear message if under threshold (e.g., <500MB)
- [ ] T018  [US1] Create an automated step (documented) to retain previous artifact under `~/deployments/api/previous` and confirm rollback path exists: document in `specs/007-internal-deployment-to/spec.md`

### User Story 2 — Deploy Web (Blazor) to Raspberry Pi (Priority: P1)
Goal: Publish the Web app for linux-arm64, deploy to RPi, configure base URL to point to local API, and verify UI loads and calls the API successfully.

Independent test criteria: Publish web artifact -> copy to RPi -> start web server or static host -> open UI in browser and complete a primary flow that triggers an API call

- [ ] T019  [P] [US2] Publish Web artifact for linux-arm64: `tools/publish-web-arm64.ps1` (or use existing Web publish tooling)
 - [X] T019  [P] [US2] Publish Web artifact for linux-arm64: `tools/publish-web-arm64.ps1` (or use existing Web publish tooling)
- [ ] T020  [US2] [P] Update `tools/deploy-to-pi.ps1` to include web copy steps to `~/deployments/web/<version>` and optional reverse-proxy notes
- [ ] T021  [US2] Add a minimal reverse proxy example section to `docs/deployment/internal-deployment.md` showing nginx or Caddy config snippets
- [ ] T022  [US2] Add verification script that opens the UI URL and performs a primary API-backed action using curl or a headless browser: `specs/007-internal-deployment-to/quickstart-web-smoke.sh`
- [ ] T023  [US2] [P] Ensure the web app config/template supports an environment variable for API base URL and document usage in `tools/templates/deployment.config.example.json`
- [ ] T024  [US2] Document steps in `specs/007-internal-deployment-to/quickstart.md` for web + API integration testing on RPi
- [ ] T025  [US2] Add optional guidance for serving static Web assets (when applicable) and clarify ports in `docs/deployment/internal-deployment.md`

### User Story 3 — Deploy to Windows bare metal (Priority: P1)
Goal: Publish artifacts for Windows x64, copy to target host, register services, configure firewall, and verify health endpoints and UI availability.

Independent test criteria: Publish Windows artifacts -> copy to Windows host -> register service -> confirm health endpoint responds and UI loads

- [ ] T026  [P] Publish API artifact for Windows x64 (self-contained or framework-dependent) and place in `artifacts/windows/api/<version>` via `tools/publish-api-windows.ps1`
- [ ] T027  [P] Publish Web artifact for Windows x64 and place in `artifacts/windows/web/<version>` via `tools/publish-web-windows.ps1`
- [ ] T028  [US3] Create or update `tools/deploy-windows-baremetal.ps1` to copy artifacts, configure Windows service registration notes (nssm or `sc.exe`) and optionally create basic firewall rules
- [ ] T029  [US3] Add verification helper PowerShell to check service status and call health endpoint: `specs/007-internal-deployment-to/windows-smoke.ps1`
- [ ] T030  [US3] Document required local user permissions and how to locally register the service in `tools/templates/windows-service-notes.md`
- [ ] T031  [US3] Add a rollback step for Windows artifacts (copy previous artifact back and restart service) in `specs/007-internal-deployment-to/quickstart.md`
- [ ] T032  [US3] [P] Add firewall guidance to `docs/deployment/internal-deployment.md` with example PowerShell commands for allowing internal traffic and restricting public access
- [ ] T033  [US3] Add disk and port checks into `tools/check-windows-prereqs.ps1` (from foundational tasks)
 - [X] T033  [US3] Add disk and port checks into `tools/check-windows-prereqs.ps1` (from foundational tasks)
- [ ] T034  [US3] Add guidance to confirm .NET 10 runtime and link to `global.json` verification in `specs/007-internal-deployment-to/plan.md`

### User Story 4 — Secure limited external access via VPN (Priority: P2)
Goal: Provide operator guidance and example configs to enable VPN-only remote access for maintenance and limited external usage.

Independent test criteria: Configure a VPN client and server example -> client can reach services; public IP cannot reach services

- [ ] T035  [US4] Add Tailscale quickstart notes and example (Tailnet + magic DNS) to `docs/deployment/internal-deployment.md` and `specs/007-internal-deployment-to/research.md`
- [ ] T036  [US4] Provide an optional Tailscale troubleshooting note and diagnostic checklist in `tools/templates/tailscale-troubleshooting.md`
- [ ] T037  [US4] Document key rotation and minimal hardening checklist in `docs/deployment/internal-deployment.md`
- [ ] T038  [US4] Add a VPN connectivity verification step to `specs/007-internal-deployment-to/quickstart.md` that confirms access and verifies external probing fails
- [ ] T039  [US4] [P] Add example commands to integrate Tailscale service start on host boot in the RPi and Windows quickstart docs

### User Story 5 — Quick rollback and update (Priority: P3)
Goal: Provide documented rollback commands and retention of a previous artifact to enable one-step rollback.

Independent test criteria: Deploy new version, run rollback command, verify service returns to previous version and health endpoint is OK

- [ ] T040  [US5] Implement `tools/retain-previous-artifact.ps1` (or update deploy scripts) so the last artifact is saved to `~/deployments/<app>/previous` (documented path)
 - [X] T040  [US5] Implement `tools/retain-previous-artifact.ps1` (or update deploy scripts) so the last artifact is saved to `~/deployments/<app>/previous` (documented path)
- [ ] T041  [US5] Add a `tools/rollback-to-previous.ps1` script (documented usage) and include Windows equivalents in `specs/007-internal-deployment-to/templates/windows-service-notes.md`
- [ ] T042  [US5] Add verification steps for rollback in `specs/007-internal-deployment-to/quickstart.md` that validate version and health endpoint after rollback
- [ ] T043  [US5] Include automated log retention rules and log path verification in `docs/deployment/internal-deployment.md` and `specs/007-internal-deployment-to/data-model.md`
- [ ] T044  [US5] Create a short rollback runbook (one-page) in `specs/007-internal-deployment-to/rollback-runbook.md`

## Final Phase — Polish & cross-cutting concerns

- [ ] T045  [P] Update `docs/deployment/internal-deployment.md` with final verified commands and example output logs
- [ ] T046  Add a short security checklist in `specs/007-internal-deployment-to/security-checklist.md` covering auth posture (JWT), VPN posture, and host hardening
- [ ] T047  [P] Run a smoke-run walkthrough and record the results in `specs/007-internal-deployment-to/verification-results.md` (manual run documented)
- [ ] T048  Update `specs/007-internal-deployment-to/spec.md` with any minor clarifications discovered during implementation and mark the spec as "validated"

## Security & Configuration follow-ups

 - [X] T055  [P] Implement encryption verification tasks: added `tools/verify-encryption.ps1` and `tools/verify-encryption.sh`; documented commands in `specs/007-internal-deployment-to/security-checklist.md`.
- [ ] T056  [P] Create `specs/007-internal-deployment-to/config-alignment.md` mapping config template keys to IOptions<T> properties and add tasks to validate bindings in unit tests.
 - [X] T057  [P] Update deployment docs and templates to require TLS 1.3: added `tools/verify-tls.ps1` and referenced verification in `specs/007-internal-deployment-to/security-checklist.md`.
- [ ] T058  [P] Add a repository scan task to detect magic-string configuration access (quick grep or Roslyn analyzer rule) and create issues for any violations found; document remediation steps.
- [ ] T059  [P] Update Success Criteria and Quickstart to reference encryption and TLS verification steps and add them to `specs/007-internal-deployment-to/verification-results.md` as required checks.

- [X] T060  [P] Certificate install & binding for Windows: create `tools/bind-windows-cert.ps1` to import PFX into LocalMachine\\My, grant private-key access to the service account, and optionally perform `netsh http add sslcert` when HttpSys is used. Document usage in `docs/windows-service-and-cert.md`.
- [X] T061  [P] Configure apps for Windows service + Kestrel cert: Program.cs updated to call `UseWindowsService()` when available and to configure Kestrel endpoints to use certificate thumbprint or PFX path from configuration.
- [ ] T062  [P] Add DryRun/TestMode flags to deploy scripts: update `tools/deploy-windows-baremetal.ps1` and `tools/deploy-to-pi.ps1` to support `-DryRun` and `-TestMode` flags allowing operators to preview deployment actions without executing them.
- [ ] T063  [P] Optimize Windows prereq checker performance: refactor `tools/check-windows-prereqs.ps1` to cache expensive firewall/netstat calls and filter results in-memory to improve execution speed.

## Testing tasks (Pester - unit tests only)

- [ ] T049  [P] Add Pester unit tests for deployment scripts: create tests under `tests/DeployScripts.Tests/` that mock external calls (SSH, SCP, dotnet, journalctl) and assert script logic, error handling and branching. Aim to reach ~90% line/branch coverage for `tools/deploy-to-pi.ps1` and `tools/deploy-windows-baremetal.ps1`.
 - [X] T049  [P] Add Pester unit tests for deployment scripts: create tests under `tests/DeployScripts.Tests/` that mock external calls (SSH, SCP, dotnet, journalctl) and assert script logic, error handling and branching. Aim to reach ~90% line/branch coverage for `tools/deploy-to-pi.ps1` and `tools/deploy-windows-baremetal.ps1`.

NOTE: Unit tests for scripts (Pester) cover script logic. Integration and E2E testing (real-hosts or controlled VMs/containers) are REQUIRED by the constitution and are included below as explicit tasks (T050–T053). If integration/E2E cannot be automated in this iteration, a documented waiver is required (`specs/007-internal-deployment-to/waiver-testing-exception.md`).

### Integration & E2E test tasks

- [ ] T050  [P] Design an integration test harness: document the test environment (VM/container spec), network setup, and preconditions for runs in `specs/007-internal-deployment-to/integration-plan.md`.
- [ ] T051  Implement integration tests that can run in CI or locally against a controlled VM/container; tests should verify artifact copy, service registration, and health endpoint correctness.
- [ ] T052  Add E2E Playwright tests for the web UI (if the web is part of this feature) to validate primary user flows against a deployed test host.
- [ ] T053  Add CI pipeline steps (or documented local commands) to run integration/E2E tests and publish results/artifacts.

### Quality & Linters

- [ ] T054  Add a task to run PSScriptAnalyzer and other relevant linters/quality gates against any new scripts (document fix commands and CI integration) and ensure results are captured in the verification results.

## Dependencies and story completion order

1. Setup (T001-T004) must be completed first.
2. Foundational tasks (T005-T010) must complete before story implementations rely on helper scripts or templates.
3. User stories US1, US2 and US3 (T011-T034) are implementable in parallel once foundational tasks complete. Typically US1 (API to RPi) is the MVP and should be completed first for validation.
4. US4 (VPN) depends on at least one host (US1/US2/US3) being deployable so operators can test remote connectivity.
5. US5 (Rollback) can be implemented in parallel with story implementations but requires the artifact retention mechanism from T018/T040 to be present.
6. Final polish tasks (T045-T048) run after the core stories complete.

## Parallel execution examples

- Example A: While T005 and T006 (publish profiles) run, a second engineer can implement T007 and T008 (host prereq checkers) in parallel since they touch different files and hosts. (Tasks: T005/T006 [P] || T007/T008 [P])
- Example B: After foundations are ready, US1 and US2 deployments can run concurrently on separate RPi hosts: (T011-T018) || (T019-T025)
- Example C: Windows publish tasks (T026/T027) can run in parallel with RPi deployments (T011-T025) since artifacts are different outputs. (T026/T027 [P])

-## Validation & counts

- Total tasks: 63
- Tasks per story/phase:
  - Phase 1 (Setup): 4
  - Phase 2 (Foundational): 6
  - US1 (API RPi): 8
  - US2 (Web RPi): 7
  - US3 (Windows): 9
  - US4 (VPN): 5
  - US5 (Rollback): 5
  - Final Polish: 4
  - Security & Configuration follow-ups: 9
  - Testing: 5

- Parallel opportunities identified: T005/T006 with T007/T008; US1 with US2; Windows publishes with RPi deployments

## Independent test criteria (summary)

- US1: `curl http://<rpi-host>:5000/health` returns HTTP 200 and logs show startup sequence (`journalctl -u <service>`)
- US2: Open web UI URL and perform a primary API-backed action; use headless verification script `quickstart-web-smoke.sh`
- US3: On Windows run `specs/007-internal-deployment-to/windows-smoke.ps1` to validate service and health endpoint
- US4: From a VPN client verify API and Web access and from an unauthenticated public endpoint verify blocked access
- US5: Run `tools/rollback-to-previous.ps1` and confirm previous artifact restored and service healthy within five minutes

## Suggested MVP

- MVP scope: Complete User Story 1 (US1) — Publish API for linux-arm64, provide `tools/deploy-to-pi.ps1`, systemd service template, and smoke-check script. This enables a minimal self-hosted internal deployment and validates the approach.

## Format validation

- All tasks in this file follow the required checklist format: checkbox, TaskID, optional [P], optional [US#], clear description and a file path where applicable.

---

Generated from: `specs/007-internal-deployment-to/spec.md` and `specs/007-internal-deployment-to/plan.md`
