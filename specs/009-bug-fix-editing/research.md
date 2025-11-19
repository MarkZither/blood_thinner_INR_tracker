# research.md

Decision: Use existing audit mechanism with an EF Core SaveChanges interceptor pattern to record audit records for edits and soft-deletes. UI remains MudBlazor-based and the audit wiring will be implemented as a reusable interceptor/service so it can be applied to multiple entities.

Rationale:
- Centralized auditing at the persistence layer (EF Core) ensures all updates (API, background jobs, admin tools) are captured consistently and atomically with the data change.
- Using an interceptor minimizes changes to existing controllers/services: services perform normal updates and the interceptor captures Before/After and persists AuditRecord in the same DbContext transaction.
- This approach follows the project's "minimal invasive change" preference and aligns with existing architecture (EF Core + ASP.NET Core Web API).

Alternatives considered:
- API Action Filter / Controller Middleware: captures the request/response at controller boundary. Pros: good for HTTP-specific metadata. Cons: harder to capture precise BeforeJson (requires reading DB state before change) and may miss changes from background services.
- Domain Event + Outbox pattern: emit domain events and write audits via background worker. Pros: decouples audit writes and can scale. Cons: more work and violates the requirement to persist audit atomically with the edit unless outbox is transactional and processed immediately.
- Versioned / Append-only model: create a new row per edit. Pros: immutable history. Cons: larger schema and read complexity; not in scope — user chose in-place edits with audit log.

Implementation notes:
- Implement an EF Core SaveChangesInterceptor (or override SaveChangesAsync) that inspects changed entries for INRTest entity and for Update/Delete operations:
  - For Update: load the original values (OriginalValues or tracked entity snapshot) to produce BeforeJson, and the new values (CurrentValues) as AfterJson.
  - For Delete (soft-delete): detect IsDeleted set to true and include Before/After reflecting IsDeleted change.
  - Insert AuditRecord in same DbContext transaction so persistence is atomic.
- Provide a reusable AuditInterceptor that is registered in DI and added to DbContextOptionsBuilder.AddInterceptors(...).
- Provide a small AuditService interface to format BeforeJson/AfterJson and to record actor information (UpdatedBy / DeletedBy) from the current request context (IHttpContextAccessor or passed in by services).
- Keep MudBlazor UI unchanged except wiring the existing edit/delete actions to call existing services; ensure the edit flow passes the current user identity and shows validation/errors.

Test plan (research-level):
- Unit test for AuditInterceptor that simulates context with tracked INRTest entity, makes an update, and asserts an AuditRecord is added with correct Before/After.
- Integration test that performs API edit and delete (soft) and asserts canonical row updated and AuditRecord persisted.
- Smoke test for reports that ensures soft-deleted rows are excluded by default.
