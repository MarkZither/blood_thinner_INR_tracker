# Feature Specification: Enable edit and delete of INR entries

**Feature Branch**: `009-bug-fix-editing`  
**Created**: 2025-11-16  
**Status**: Draft  
**Input**: User description: "bug fix, editing existing INR and deleting and INR entry both fail. It should be possible to edit and delete INR entries to allow for corrections."

## Clarifications

### Session 2025-11-16

- Q: Deletion semantics — selected option: Soft-delete (mark entries as deleted; exclude from user-facing views but retain for audit/undo).
- Q: Soft-delete retention policy — selected option: Never purge (retain soft-deleted entries indefinitely).
- Q: Edit semantics — selected option: In-place edits with audit log (update canonical row; record Before/After for audit and potential undo).
 - Q: Who may edit/delete entries — selected option: Owner only (only the user who created the entry may edit or soft-delete it).

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Edit an existing INR entry (Priority: P1)

As a user who records INR test results, I want to edit an existing INR entry so I can correct mistakes (e.g., wrong INR value, wrong date/time, or notes) without creating duplicate entries.

**Why this priority**: Correcting INR records is critical for clinical accuracy and user confidence. This reduces support requests and prevents incorrect medical decisions based on wrong data.

**Independent Test**: Open the INR log list, select an existing entry, choose Edit, change the INR value and save. Verify the updated values appear in the list and that the original entry ID remains the same.

**Acceptance Scenarios**:

1. **Given** an existing INR entry, **When** the user edits the INR value and saves, **Then** the entry is updated and the change is persisted and reflected in all views and reports.
2. **Given** an existing INR entry, **When** the user edits the date/time and saves, **Then** the entry date/time is updated and any schedule reminders or trend calculations use the new date/time.

---

### User Story 2 - Delete an INR entry (Priority: P1)

As a user, I want to delete an incorrect INR entry so my records remain accurate and my trend charts reflect only valid tests.

**Why this priority**: Deleting erroneous entries prevents misleading trends and supports data hygiene. This is as important as editing and should be P1 as well.

**Independent Test**: From the INR list, delete an entry. Confirm the entry is removed from lists, charts, and reports and that deletion is recorded in audit/logs (if present).

**Acceptance Scenarios**:

1. **Given** an existing INR entry, **When** the user chooses Delete and confirms, **Then** the entry is removed and no longer appears in lists, trends, or export data.
2. **Given** an entry referenced by other derived data (e.g., dosage variance report), **When** the entry is deleted, **Then** derived reports reflect the change and do not error.

---

### User Story 3 - Undo or audit trail for edits/deletes (Priority: P2)

As an administrator or power user, I want an audit trail (or an undo within a short window) of edits/deletes so accidental or malicious changes can be reviewed and, if necessary, reversed.

**Why this priority**: Provides accountability and safety for medical data. If full audit storage is out of scope, at minimum provide an in-memory undo within the current session.

**Independent Test**: Edit an entry, then view audit logs or perform undo. Verify that previous values are available in the audit trail and that undo restores the prior state within allowed constraints.

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

- Editing an entry referenced by other reports (variance reports, schedules). Ensure recalculation occurs and UI does not crash.
- Attempting to delete the last remaining INR entry for a medication or patient. Confirm UX warns user and dependent features handle zero-entry datasets.
- Concurrent edits: two devices editing the same entry. Prefer last-write-wins and surface a conflict warning if the server supports it.
- Editing with invalid values (e.g., INR outside allowable range 0.5–8.0). Validate input and show user-friendly errors.
- Timezone changes: editing date/time must normalize to the user's current timezone and preserve temporal ordering.

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: The UI MUST provide an Edit action for each INR entry in lists and detail views.
  - Acceptance: Editing an entry updates the displayed values and persists the change (see User Story 1 acceptance scenarios).
- **FR-002**: The UI MUST provide a Delete action for each INR entry, protected by a confirmation dialog. Deletion MUST implement soft-delete semantics (mark entries as deleted rather than immediate permanent removal).
  - Acceptance: Deleting an entry marks it as deleted (fields such as IsDeleted, DeletedAt, DeletedBy) and the entry is excluded from normal user-facing lists, charts, and exports after user confirmation. Admins or scheduled jobs can permanently purge soft-deleted entries per retention policy.
- **FR-003**: The system MUST validate edited INR values are numeric and within 0.5–8.0; out-of-range values must be rejected with a clear error message.
  - Acceptance: Attempting to save an out-of-range value returns a validation error and the entry is not changed.
- **FR-004**: The system MUST persist edits and deletions atomically so that partial updates do not leave inconsistent state.
  - Acceptance: Under simulated partial-failure (e.g., interrupted save), the entry is either fully updated or left unchanged; no partial state is visible.
 - **FR-004**: The system MUST persist edits and deletions atomically so that partial updates do not leave inconsistent state.
  - Acceptance: Under simulated partial-failure (e.g., interrupted save), the entry is either fully updated or left unchanged; no partial state is visible.
 - **FR-011**: Edits MUST be applied in-place to the canonical INRTest row while creating a corresponding AuditRecord that stores BeforeJson and AfterJson.
  - Acceptance: After an edit, the canonical row reflects the new values and an AuditRecord exists with the original values in BeforeJson and new values in AfterJson.
 - **FR-012**: INRTest rows MUST include UpdatedAt and UpdatedBy metadata to indicate the last editor.
  - Acceptance: After an edit, UpdatedAt is set and UpdatedBy references the editing user.
 - **FR-013**: Permission: Only the entry owner (the user who created the INR entry) may edit or soft-delete the entry.
  - Acceptance: Attempts by other users to edit or delete an entry return an authorization error and no changes or audit records are created.
- **FR-005**: The system MUST provide a backend capability to update and delete INR entries for the authenticated user (no UI-only workaround).
  - Acceptance: Programmatic or backend operations for update/delete complete successfully for authorized users and produce corresponding persisted changes.
- **FR-006**: After edit or delete, dependent calculations (trends, averages, dosage variance reports, schedules) MUST be recalculated or refreshed within the UI.
  - Acceptance: After updating or deleting an entry, at next view or refresh the derived data reflects the change without errors.
- **FR-007**: The system SHOULD record an audit record for each edit and delete containing: who performed the action, timestamp, original values, and new values (for edits). If full audit storage is not available, at minimum log the action server-side.
  - Acceptance: Audit log entry or server-side log exists for a sample edit/delete event.
- **FR-008**: Input forms MUST perform client-side validation and show contextual error messages; server-side validation MUST also enforce the same constraints.
  - Acceptance: Client rejects invalid input before submission; server rejects invalid input if client validation is bypassed.
- **FR-009**: When deleting, if the entry is referenced by derived data, the system MUST ensure reports either exclude the deleted entry or flag the report for recalculation; deletion must not cause crashes or unhandled exceptions.
  - Acceptance: Deleting referenced entries does not cause application errors; reports are either recalculated or show an accurate state.

- **FR-010**: The system SHOULD provide a controlled purge mechanism (manual or scheduled) to permanently remove soft-deleted entries after an agreed retention period.
  - Acceptance: Purged entries no longer appear in storage; purge operations create an audit record.

### Acceptance tests (concrete)

- Edit success: edit an INR value -> canonical row Value updated, UpdatedAt/UpdatedBy set, AuditRecord exists containing original Value and new Value.
- Edit validation: edit to out-of-range -> validation blocks update and no AuditRecord is created.
- Delete (soft): delete an entry -> IsDeleted true, DeletedAt/DeletedBy set, AuditRecord created with BeforeJson and AfterJson showing IsDeleted=true.
- Reports resilient: delete does not crash trend/variance reports and deleted rows are excluded by default.
- Audit access control: only authorized roles can access full audit content.

### Dependencies & Assumptions

- The system already has authentication and per-user data isolation in place; edits/deletes are limited to the entry owner.
- Server-side validation and persistence mechanisms exist and will be used; this feature does not introduce new persistence technologies.
- INR valid range is assumed to be 0.5–8.0 for validation purposes; business may adjust these bounds.
- Time values are stored in UTC; UI will present and accept local times and convert to UTC.
- Full audit storage may be optional; if unavailable, server-side logs are sufficient initially.

### Acceptance Criteria Mapping

- FR-001 -> User Story 1 acceptance scenarios 1-2
- FR-002 -> User Story 2 acceptance scenarios 1-2
- FR-003 -> Edge Cases and User Story 1 acceptance scenarios
- FR-004 -> Simulated failure tests described under Edge Cases
- FR-005 -> Backend integration tests (authenticated update/delete flows)
- FR-006 -> UI refresh/recalculation tests after edits/deletes
- FR-007 -> Audit verification test in User Story 3
- FR-008 -> Form validation tests
- FR-009 -> Report recalculation tests

### Key Entities *(include if feature involves data)*

- **INRTest (INR Entry)**: Represents a recorded INR test. Key attributes: Id, UserId, MedicationId (optional), Value (decimal), Units, TestDateTime (UTC), Notes (string), CreatedAt, UpdatedAt, Source (device/manual), Audit metadata.
 - **INRTest (INR Entry)**: Represents a recorded INR test. Canonical attributes include:
  - Id: GUID (PK)
  - UserId: GUID (owner)
  - MedicationId: GUID? (optional)
  - Value: decimal (precision 3, scale 2) - validation 0.5..8.0
  - Units: string (e.g., "INR")
  - TestDateTimeUtc: DateTime (UTC)
  - Notes: string (nullable)
  - CreatedAt: DateTimeUtc
  - UpdatedAt: DateTimeUtc (nullable)
  - UpdatedBy: GUID (nullable)
  - IsDeleted: bool (default false)
  - DeletedAt: DateTimeUtc? (nullable)
  - DeletedBy: GUID? (nullable)

  - Soft-delete support: IsDeleted (bool), DeletedAt (timestamp, nullable), DeletedBy (user id, nullable). Audit metadata is stored in `AuditRecord`.
- **User**: Owner of INR entries (Id, DisplayName, Timezone).
- **Medication**: (Id, Name) - optional relationship when test is associated with a medication.
- **AuditRecord**: (Id, TargetEntityId, ActionType [Edit/Delete], ActorId, Timestamp, BeforeJson, AfterJson)

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: 100% of edit and delete actions initiated by authenticated users result in success (HTTP 2xx) or a user-visible validation error; no unhandled exceptions are returned to the client.
- **SC-002**: Edited INR values are reflected in the UI and persisted to storage within 3 seconds of save in normal conditions.
- **SC-003**: 0 critical regressions introduced by this change (no new unhandled exceptions in logs related to INR edits/deletes) over a 7-day smoke period.
- **SC-004**: Reduce reported support incidents about INR entry corrections by 90% for issues caused by inability to edit/delete.

