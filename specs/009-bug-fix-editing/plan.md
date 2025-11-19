# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Fix editing and deletion of INR entries by enabling in-place edits and soft-deletes with audit logging. Use the existing audit mechanism and implement an EF Core SaveChanges interceptor (AuditInterceptor) to record BeforeJson/AfterJson for edits and soft-deletes. Keep UI on MudBlazor and reuse existing edit/delete flows; minimal changes required to services and controllers to ensure UpdatedBy/UpdatedAt are set via current user identity.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: .NET 10 (C# 13)
**Primary Dependencies**: ASP.NET Core, Entity Framework Core, MudBlazor, Serilog
**Storage**: PostgreSQL (cloud) / SQLite (local dev) via EF Core
**Testing**: xUnit (backend), BUnit (Blazor), Playwright (UI) as per constitution
**Target Platform**: Web (Blazor Server/WA) and API services (Linux/Azure)
**Project Type**: Web application (frontend: Blazor/MudBlazor, backend: ASP.NET Core Web API)
**Performance Goals**: Maintain current project goals (API responses within 2s; local operations <200ms)
**Constraints**: Respect Constitution rules: options pattern for configuration, high test coverage, MudBlazor UI, use existing audit mechanism where possible.
**Scale/Scope**: Small feature change; targeted to be completed within a short sprint (2-3 weeks). Keep PRs small and tests comprehensive.

## Constitution Check

GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.

Key gates from constitution:
- UI must use MudBlazor - satisfied (no UI framework change required)
- Options pattern - configuration unchanged
- Testing discipline - tests to be added (xUnit/BUnit)
- Security & OWASP - no new surface; ensure auth/authorization enforced for owner-only edits

All gates are satisfied or will be enforced during implementation. No constitution violations expected.

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

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
