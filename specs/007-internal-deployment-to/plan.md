# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Provide a simple, repeatable internal deployment path for the API and Web front-end to run on internal Raspberry Pi (ARM64) and Windows (x64) hosts. The approach is K.I.S.S.: use hosted published artifacts, operator-run copy-and-start steps, VPN-based access via Tailscale (Tailnet + magic DNS) for remote access, and minimal local service configuration. Scripts are already started at `tools/deploy-to-pi.ps1` and `tools/deploy-windows-baremetal.ps1`.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C#/.NET 10 (project uses .NET 10 per repository constraints)
**Primary Dependencies**: ASP.NET Core for API, Blazor Server/WebAssembly (existing) for Web; PowerShell/Bash helper scripts for deploy; Tailscale for VPN (operator-managed).
**Storage**: Local filesystem for published artifacts and logs; existing application DBs unchanged (N/A for this deployment guide).
**Testing**: Unit, integration, and E2E tests are required per the constitution; this plan includes manual smoke tests for health endpoints and explicit tasks to add integration/E2E tests where applicable. Acceptance tests: scripted health check and log inspection.
**Target Platform**: Raspberry Pi (ARM64 Linux with service manager); Windows x64 (desktop/laptop).
**Project Type**: Web/API (backend + web front-end).  
**Performance Goals**: User-perceived responsiveness; health endpoint must respond within 30s of service start; no high concurrency SLAs for this internal deployment.
**Constraints**: Minimal host configuration; operator must have admin-capable SSH on Linux; Windows may be local operator-only.
**Transport posture**: Default transport protection for internal deployments is VPN-only (operator-managed). TLS/HTTPS is REQUIRED for traffic addressed via Tailnet hostnames (Tailnet magic DNS). Operators must obtain and install Tailnet-issued certificates on-host (example: `tailscale cert <hostname>`). Documentation and tasks will include explicit steps to obtain and install certificates.
**Scale/Scope**: Local/internal usage (tens of clients on LAN or VPN), not designed for large public scale.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

[Gates determined based on constitution file]

## Project Structure

### Documentation (this feature)

```
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: Use the existing repo layout. This feature focuses on documentation and scripts that invoke existing compiled projects. Key locations:

- `specs/007-internal-deployment-to/plan.md` (this file)
- `specs/007-internal-deployment-to/spec.md` (feature spec)
- `tools/deploy-to-pi.ps1` (operator-side helper to publish & copy)
- `tools/deploy-windows-baremetal.ps1` (operator-side helper to copy & install on Windows)
- `docs/deployment/RASPBERRY-PI-INTERNAL.md` and `docs/deployment/WINDOWS-BAREMETAL.md` (architectural references)

Rationale: no new runtime components are added to the repo; the plan produces docs, small scripts, and test commands only.

## Complexity Tracking

*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

## Phase 0: Research tasks

1. Consolidate Tailscale usage notes and minimal Tailnet/magic DNS configuration snippets (see `research.md`).
2. Confirm systemd service pattern for ARM Linux and Windows local service registration steps; create one-line templates (see `quickstart.md`).
3. Document rollback process and implement artifact retention policy (one previous artifact) and a simple local rollback command (documented in spec and tasks).

## Phase 1: Design & Contracts

- `data-model.md`: operational artifacts description (created)
- `quickstart.md`: operator quickstart and smoke tests (created)
- `contracts/openapi.yaml`: placeholder (created)

Next step: run `/speckit.tasks` to expand Phase 2 actionable items and produce task list for implementation.
