# Issue: Improve API XML documentation and remove CS1591 suppression

**Summary**

We see many CS1591 warnings in the API build indicating missing XML comments on public types and members. To reduce noise while implementing the INR edit/delete fix, add a temporary suppression for CS1591 and create this issue to track improving the public API documentation so the suppression can be removed later.

**Why**

- CS1591 warnings clutter build output and hide more important warnings/errors.
- Public API documentation (XML comments) is important for maintainability and for generating API docs (Swashbuckle/OpenAPI) accurately.

**Proposed short-term action**

1. Add CS1591 to the `NoWarn` list in `Directory.Build.props` (or project-level property) to suppress these warnings during the current work. Example:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

Note: Use `1591` or `CS1591` depending on project style; both map to the same warning.

**Proposed long-term work (this issue)**

- Audit all public controllers, DTOs, response/request models and add XML documentation comments where appropriate.
- Prioritize documentation for public API controllers and models used by clients (Web, Mobile, CLI).
- Add tests or CI validation to ensure new public API surface has XML comments (or at least key controllers).
- Remove the suppression and re-run builds to ensure no CS1591 warnings remain.

**Acceptance criteria**

- `Directory.Build.props` contains a temporary suppression for CS1591 (or equivalent) and the number of CS1591 warnings in a build is reduced.
- A documented backlog of files/areas needing XML comments exists in this issue and includes initial owners and estimates.

**Labels**: docs, tech-debt, backlog

**Assignees**: @team (needs triage)

**References**
- Specs: `specs/009-bug-fix-editing/spec.md`
- Build output shows multiple CS1591 warnings on controllers and DTOs

---

*Created automatically as part of feature 009 work.*
