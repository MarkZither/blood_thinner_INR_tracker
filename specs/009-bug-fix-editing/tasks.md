```markdown
# Feature 009: Enable edit and delete of INR entries - Tasks

## Task Checklist

- [x] T009-001: Add AuditRecord entity and INRTest schema updates
- [x] T009-002: Implement EF Core SaveChangesInterceptor (AuditInterceptor)
- [x] T009-003: Add IUserContextProvider / reuse ICurrentUserService
- [x] T009-004: Wire UpdatedBy/DeletedBy population in DbContext or repositories 
- [x] T009-005: Implement PATCH /api/inr/{id} endpoint
- [x] T009-006: Implement DELETE /api/inr/{id} endpoint (soft-delete)
- [x] T009-007: Update read queries to exclude soft-deleted entries by default
- [ ] T009-008: Update Web UI (MudBlazor) - Edit form and Delete confirmation wiring
- [ ] T009-009: Add unit/integration tests for API and interceptor
- [x] T009-010: Update quickstart and docs, migration notes
- [ ] T009-011: Run build, tests, and fix issues
- [x] T009-012: Suppress CS1591 compiler warnings and create follow-up API docs issue
- [ ] T009-013: Test coverage plan & CI gate
- [ ] T009-014: Ensure CS1591 suppression is temporary and removed


**Status**: Not Started
**Branch**: `009-bug-fix-editing`
**Created**: 2025-11-16

---

## Overview

This file breaks the feature spec into small, implementable tasks. Each task has an ID (T009-xxx), priority (P1/P2), a short estimate, dependencies, and acceptance criteria that map to the spec's FRs and acceptance tests.

Follow the repository conventions for short commit messages and include the task ID in each commit.

---

## ✅ Completed Tasks

- T009-001: Added `AuditRecord` model and added `UpdatedBy`/`DeletedBy` to `INRTest` in the shared models. Shared-model changes compiled in a full solution build (warnings only). Migrations are deferred to DB-specific projects per the task guidance.

---

## Tasks

### T009-001: Add AuditRecord entity and INRTest schema updates [P1]
**Status**: completed
**Notes**: Shared-model subset implemented: added `src/BloodThinnerTracker.Shared/Models/AuditRecord.cs` and added `UpdatedBy`/`DeletedBy` to `src/BloodThinnerTracker.Shared/Models/INRTest.cs`. Solution build succeeded (with existing CS1591 warnings). Per-DB migrations remain deferred until T009-004/T009-002 are implemented.
**Estimate**: 2.5 hours
**Files/Locations**:
- `src/BloodThinnerTracker.Api/Models/INRTest.cs` (update)
- `src/BloodThinnerTracker.Api/Models/AuditRecord.cs` (new)
- NOTE: The repository uses a shared ContextBase and DB-specific DbContext implementations.
- Do NOT add a new `BloodThinnerDbContext` in `src/BloodThinnerTracker.Api`.
- Instead, update the shared model definitions under `src/BloodThinnerTracker.Api/Models` (or the shared project) and then apply migrations in each DB-specific project (Postgres/SQLite/SQLServer).
- Migrations should be created in the database-specific projects (e.g., `src/BloodThinnerTracker.Api.Postgres/Migrations`), not in the API surface project.
**Description**: Add the AuditRecord entity and update the INRTest entity with UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy fields and indexes as defined in `specs/009-bug-fix-editing/data-model.md`.
**Acceptance**:
- Project builds
- New migration compiles
- INRTest and AuditRecord tables modelled with the fields described in data-model.md
**Dependencies**: none

### T009-002: Implement EF Core SaveChangesInterceptor (AuditInterceptor) [P1]
**Status**: completed
**Estimate**: 4 hours
**Files/Locations**:
- Consider adding the interceptor to the shared Data project (e.g., `src/BloodThinnerTracker.Api.Data.Shared/` or the DB-specific projects where the DbContext implementations live). Do NOT assume a DbContext in `src/BloodThinnerTracker.Api`.
- `src/BloodThinnerTracker.Api.Data.Shared/AuditInterceptor.cs` (new) or in each DB-specific project if interceptor needs DB-specific registration.
- Register the interceptor in the DB-specific DbContext registration (Postgres/SQLite/SQLServer projects) so it runs for the actual DbContext implementation.
- Reuse existing `ICurrentUserService` (or similarly named) instead of introducing `IUserContextProvider`. If `ICurrentUserService` exists, reference it in acceptance criteria and DI registration notes.
**Description**: Implement an interceptor that captures entity changes (INRTest edits and soft-deletes) and creates AuditRecord entries with BeforeJson and AfterJson in the same DbContext transaction. Use JSON serialization with stable ordering. Interceptor should be resilient to null ActorId and include correlation id if available.
**Acceptance**:
- Editing an INRTest results in one AuditRecord with BeforeJson and AfterJson
- Soft-deleting an INRTest results in an AuditRecord with BeforeJson (original) and AfterJson (IsDeleted=true)
- Interceptor writes AuditRecord within same transaction so rollbacks remove both the update and audit record
**Dependencies**: T009-001, T009-004

### T009-003: Add IUserContextProvider to surface current user id to services [P1]
**Status**: completed
**Estimate**: 1 hour
**Files/Locations**:
- `src/BloodThinnerTracker.Api/Services/IUserContextProvider.cs` (new)
- `src/BloodThinnerTracker.Api/Services/UserContextProvider.cs` (new)
- `src/BloodThinnerTracker.Api/Program.cs` (DI registration)
**Description**: Provide a small, testable service that returns the current user's id (Guid?) and display name. Backed by `IHttpContextAccessor` and claims. Used by interceptor and controllers to set UpdatedBy/DeletedBy and ActorId on AuditRecord.
**Acceptance**:
- Service returns user id for requests with authenticated principal
- Service returns null for unauthenticated contexts (tests must handle)
**Dependencies**: none
**Status**: completed

**Decision / Notes**:
- Reuse existing `ICurrentUserService` rather than adding a new `IUserContextProvider` to avoid duplication. The repository already contains:
	- `src/BloodThinnerTracker.Data.Shared/ICurrentUserService.cs`
	- `src/BloodThinnerTracker.Api/Services/CurrentUserService.cs`

- Rationale: `ICurrentUserService` is already registered and used by API services. For the audit/interceptor design chosen in this feature we do not need a separate provider: higher-level controllers/services will set `UpdatedBy` / `DeletedBy` (PublicId GUIDs) on entities prior to SaveChanges. `ICurrentUserService` can still be used where callers need the internal DB id (int?) — keep it as-is.

**Acceptance (updated)**:
- No new provider added. Existing `ICurrentUserService` remains available for services that need the internal DB id.
- Controllers/services will continue to obtain the user's PublicId from claims (or map as needed) and populate entity `UpdatedBy`/`DeletedBy` prior to SaveChanges. The interceptor reads those fields when creating `AuditRecord` entries.

**Implication**: T009-003 is considered complete for this feature's scope. If later we need a separate provider returning PublicId as a Guid, we can introduce an adapter service (thin wrapper) that composes `ICurrentUserService` and `IHttpContextAccessor`.

### T009-004: Wire UpdatedBy/DeletedBy population in DbContext or repositories [P1]
**Status**: completed
**Estimate**: 1.5 hours
**Files/Locations**:
- `src/BloodThinnerTracker.Api/Data/BloodThinnerDbContext.cs` (optional)
 - NOTE: Do NOT add or expect a `BloodThinnerDbContext` in the API surface project. The concrete DbContext implementations live in DB-specific projects (Postgres/SQLite/SQLServer) and a shared ContextBase may exist in a Data.Shared project. Make any DbContext wiring changes in the DB-specific projects so the provider registrations and migrations remain colocated.
- `src/BloodThinnerTracker.Api/Controllers/INRController.cs` (small changes)
- `src/BloodThinnerTracker.Api/Services/INRService.cs` (if exists)
**Description**: Ensure UpdatedAt/UpdatedBy are set when an INRTest is modified and DeletedAt/DeletedBy/IsDeleted are set when Delete is invoked. Implementation approach: set UpdatedBy/DeletedBy to the current user's PublicId in controller/service layer before SaveChanges so the data layer remains decoupled from HTTP concerns. Interceptor reads these fields when creating AuditRecord entries.
**Acceptance**:
- After edits, UpdatedAt and UpdatedBy are set
- After delete, IsDeleted=true and DeletedAt/DeletedBy set
**Dependencies**: T009-001

### T009-005: Implement PATCH /api/inr/{id} endpoint [P1]
**Status**: completed
**Estimate**: 3 hours
**Files/Locations**:
- `src/BloodThinnerTracker.Api/Controllers/INRController.cs` (add PATCH handler)
- `src/BloodThinnerTracker.Api/Models/Requests/UpdateINRTestRequest.cs` (new or reuse)
- `src/BloodThinnerTracker.Api/Services/INRService.cs` (update)
**Description**: Implement the API per `specs/009-bug-fix-editing/contracts/inr-audit.yaml`. Validate inputs (value in range, date-time sanity), enforce ownership (only owner may edit), apply changes in-place and return 200 with updated resource or 400/403 accordingly. Rely on AuditInterceptor to create audit record.

Important UI note (root cause): The Web UI list historically passed the internal database `Id` into edit/delete handlers which is not intended for public API calls. The UI should pass the stable `PublicId` value. Ensure the PATCH endpoint documents and accepts a public identifier or provide a lightweight mapping endpoint so the client can resolve `PublicId` to the internal id. Coordinate this behavior with T009-008 (UI wiring).
**Acceptance**:
- Valid edit returns 200 and updated INRTest
- Invalid values return 400 and no change/audit
- Unauthorized attempts return 403 and no change/audit
**Dependencies**: T009-001, T009-003, T009-004, T009-002

### T009-006: Implement DELETE /api/inr/{id} endpoint (soft-delete) [P1]
**Status**: completed
**Estimate**: 2 hours
**Files/Locations**:
- `src/BloodThinnerTracker.Api/Controllers/INRController.cs` (add DELETE handler)
- `src/BloodThinnerTracker.Api/Services/INRService.cs` (update)
**Description**: Implement soft-delete that sets IsDeleted=true, DeletedAt, DeletedBy and returns 204. Enforce owner-only permission. AuditInterceptor must create corresponding AuditRecord.

Important UI note (root cause): The Delete action from the Web UI must pass `PublicId` (the stable public identifier) rather than the internal `Id`. Either accept `PublicId` directly in the DELETE endpoint or expose a mapping endpoint so the UI can call delete with the public identifier. Document the chosen approach and ensure T009-008 implements the matching client behavior.
**Acceptance**:
- Successful delete returns 204 and INRTest.IsDeleted=true
- Unauthorized delete returns 403 and no change/audit
**Dependencies**: T009-001, T009-003, T009-004, T009-002

### T009-007: Update read queries to exclude soft-deleted entries by default [P1]
**Status**: completed
**Estimate**: 1.5 hours
**Files/Locations**:
- `src/BloodThinnerTracker.Api/Repositories/INRRepository.cs` (or DbContext queries)
- `src/BloodThinnerTracker.Api/Controllers/INRController.cs` (list endpoints)
- `src/BloodThinnerTracker.Web/Services/INRService.cs` (client-side filtering - if applicable)
**Description**: Ensure all read endpoints and EF queries exclude IsDeleted=true rows by default. Add explicit IncludeDeleted query option if needed for admins/audit.
**Acceptance**:
- Normal list and trend endpoints do not include soft-deleted rows
- Admin/audit endpoints can request deleted rows explicitly
**Dependencies**: T009-001

### T009-008: Update Web UI (MudBlazor) - Edit form and Delete confirmation wiring [P1]
**Status**: not-started
**Estimate**: 3 hours
**Files/Locations**:
- `src/BloodThinnerTracker.Web/Components/Pages/INREdit.razor` (update form to PATCH)
- `src/BloodThinnerTracker.Web/Components/Pages/INRList.razor` (delete action)
- `src/BloodThinnerTracker.Web/Services/INRService.cs` (ensure PATCH/DELETE calls)
**Description**: Update UI to call the new PATCH and DELETE endpoints. Use existing MudDialog confirmation for delete. Handle 403 by showing an access denied snackbar and prevent UI state drift. After success, refresh lists/charts and close dialogs.

Root cause note: The current INR list implementation used the internal `Id` in OnClick handlers (for example `EditINRTest(test.Id.ToString())`). That caused failures when the API expected a public identifier. The UI must expose and pass `PublicId` to edit/delete flows (e.g., `EditINRTest(test.PublicId.ToString())`). If the API will continue to accept internal ids, add a documented mapping endpoint; otherwise prefer accepting `PublicId` in PATCH/DELETE for safety.

**Acceptance**:
- UI edit saves and updates list/chart without creating duplicate entries
- UI delete removes entry from lists/charts
- UI passes `PublicId` to PATCH/DELETE calls (or calls a documented mapping endpoint)
- Authorization errors surfaced to user
**Dependencies**: T009-005, T009-006, T009-007

### T009-009: Add unit/integration tests for API and interceptor [P1]
**Status**: not-started
**Estimate**: 4 hours
**Files/Locations**:
- `tests/BloodThinnerTracker.Api.Tests/INRControllerTests.cs` (new tests)
- `tests/BloodThinnerTracker.Api.Tests/AuditInterceptorTests.cs` (new tests)
- `tests/BloodThinnerTracker.Web.Tests/INRUiTests.cs` (BUnit) (optional)
**Description**: Add tests covering:
- Successful edit creates AuditRecord and updates INRTest
- Edit validation rejects out-of-range values and creates no AuditRecord
- Soft-delete sets IsDeleted and creates AuditRecord
- Unauthorized edits/deletes are forbidden and create no AuditRecord
**Acceptance**:
- Tests pass locally on `dotnet test`
**Dependencies**: T009-001..T009-006

### T009-010: Update quickstart and docs, migration notes [P2]
**Status**: completed
**Estimate**: 1 hour
**Files/Locations**:
- `specs/009-bug-fix-editing/quickstart.md` (update)
- `docs/api/` (if applicable)
- `docs/deployment/` (if migration steps needed)
**Description**: Document the database migration step and how to verify edit/delete flows locally.
**Acceptance**:
- Quickstart contains migration instructions and API examples for PATCH/DELETE
**Dependencies**: T009-001, T009-005, T009-006

### T009-011: Run build, tests, and fix issues [P1]
**Status**: not-started
**Estimate**: 1-2 hours
**Files/Locations**:
- root solution
**Description**: Run `dotnet build` and `dotnet test`, fix any compilation, lint, or test failures introduced by the changes.
**Acceptance**:
- Build passes and tests pass
**Dependencies**: all previous tasks

### T009-012: Suppress CS1591 compiler warnings and create follow-up API docs issue [P2]
**Status**: completed
**Notes**: Temporarily suppressed CS1591 in `Directory.Build.props` with documentation and TTL. Created issue draft `specs/009-bug-fix-editing/issue-improve-api-docs.md` to track restoring XML documentation. Suppression is timeboxed and must be removed per T009-014.
**Estimate**: 0.5 hours
**Files/Locations**:
- `Directory.Build.props` (recommended location to add <NoWarn>CS1591</NoWarn> or to append to existing NoWarn)
- `specs/009-bug-fix-editing/issue-improve-api-docs.md` (new GH issue draft)
**Description**: Reduce noise from missing XML comments by suppressing CS1591 in the short term, and create a GitHub issue to track improving public API XML documentation (so CS1591 can be removed later). This keeps the build output readable while we implement features.
**Acceptance**:
- Add an entry to `Directory.Build.props` to suppress CS1591 (or equivalent) and confirm build warnings count for CS1591 reduces.
- A GitHub issue draft exists in the feature folder describing the documentation improvements needed and an initial prioritization plan.
 - Suppression MUST be temporary: include an inline comment in the chosen file documenting who approved it and a removal condition (for example: remove when `specs/009-bug-fix-editing/issue-improve-api-docs.md` is closed or after 14 days). CI should report suppression age.
 - A follow-up task (T009-014) to remove suppression is required and must be scheduled.
**Dependencies**: none

### T009-013: Test coverage plan & CI gate [P1]
**Status**: not-started
**Estimate**: 4 hours
**Files/Locations**:
- `specs/009-bug-fix-editing/tasks.md` (update)
- `tests/` (new/updated tests)
- CI configuration (e.g., .github/workflows/coverage.yml)
**Description**: Create a concrete test plan to achieve the constitution-required 90% coverage for code introduced and modified by this feature. Break tests into unit, integration, BUnit, and Playwright categories. Add a CI gate that fails the build if coverage falls below 90% for the modified projects.
**Acceptance**:
- Test plan documented and added to `tasks.md`.
- CI workflow updated to collect and enforce coverage for impacted projects.
**Dependencies**: T009-009

### T009-014: Ensure CS1591 suppression is temporary and removed [P2]
**Status**: not-started
**Estimate**: 0.5 hours
**Files/Locations**:
- `Directory.Build.props` (change to remove NoWarn)
- `specs/009-bug-fix-editing/issue-improve-api-docs.md` (close issue)
**Description**: Remove the temporary CS1591 suppression once API XML documentation backlog (issue) is closed or after the agreed TTL. Ensure CI warns if suppression persists longer than allowed.
**Acceptance**:
- CS1591 suppression removed and build re-run showing restored CS1591 checks.
- Issue `specs/009-bug-fix-editing/issue-improve-api-docs.md` closed or assigned to backlog with owner.
**Dependencies**: T009-012

---

## Task Dependencies & Parallelization

- Can run in parallel: T009-001 (models/migration) and T009-003 (user context provider).
- T009-002 (interceptor) depends on T009-001 and T009-003.
- T009-004 (metadata wiring) depends on T009-001 and T009-003.
- API endpoints (T009-005, T009-006) depend on T009-001..T009-004.
- UI changes (T009-008) depend on API endpoints and read-query exclusions (T009-005..T009-007).
- Tests (T009-009) should be added incrementally as each backend piece is implemented.

---

## Estimates Summary

- Total: ~19–22 hours (implementation + tests + docs) for a single developer.

---

## How to run locally (verification checklist)

1. Checkout branch `009-bug-fix-editing`.
2. Generate and apply migrations in each DB-specific project. For example:

	- Postgres project: `dotnet ef migrations add AddAuditAndSoftDelete --project src/BloodThinnerTracker.Api.Postgres --startup-project src/BloodThinnerTracker.Api && dotnet ef database update --project src/BloodThinnerTracker.Api.Postgres --startup-project src/BloodThinnerTracker.Api`
	- SQLite project: `dotnet ef migrations add AddAuditAndSoftDelete --project src/BloodThinnerTracker.Api.Sqlite --startup-project src/BloodThinnerTracker.Api && dotnet ef database update --project src/BloodThinnerTracker.Api.Sqlite --startup-project src/BloodThinnerTracker.Api`
	- SQLServer project: `dotnet ef migrations add AddAuditAndSoftDelete --project src/BloodThinnerTracker.Api.SqlServer --startup-project src/BloodThinnerTracker.Api && dotnet ef database update --project src/BloodThinnerTracker.Api.SqlServer --startup-project src/BloodThinnerTracker.Api`

	Note: The model changes should be made in the shared model area; migrations must be created per DB provider project so provider-specific SQL is generated and tracked in the respective Migrations folders.
3. Start API and Web projects (or use `dotnet run` tasks provided).
4. Use the Web UI to edit an INR entry and confirm update.
5. Use the Web UI to delete an INR entry and confirm it disappears from lists and an AuditRecord exists in DB.

---

## Notes

- Keep commits small and include the T009-xxx ID in the summary line (max 72 chars). Follow the project's commit message guidance in `.github/copilot-instructions.md`.
- Prefer non-destructive, test-first edits. Add unit tests for each FR.

```
