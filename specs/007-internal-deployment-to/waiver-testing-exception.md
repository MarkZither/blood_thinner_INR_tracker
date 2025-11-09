# Waiver: Integration / E2E testing exception

Use this document only when the project cannot practically implement the required integration/E2E automation for a feature during the current iteration. A waiver must be explicit, limited in scope, and approved by the product owner and security lead.

Feature: `007-internal-deployment-to`
Requested by: [name]
Date: [YYYY-MM-DD]

Scope of waiver
---------------
- Systems covered: deployment scripts and operator workflows for Raspberry Pi and Windows bare metal.
- Temporarily waived requirements: automated integration tests and E2E Playwright runs that require physical or VM hosts.

Reason for waiver
-----------------
- [Explain the technical or operational constraint preventing automation this iteration — e.g., lack of ephemeral Windows VM in CI, cost constraints, policy temporarily preventing cloud-hosted test VMs.]

Mitigation steps
----------------
1. Provide a detailed manual runbook (`specs/007-internal-deployment-to/quickstart.md`) with explicit verification checklists and smoke scripts to be run by an operator.
2. Implement comprehensive unit tests (Pester for scripts, xUnit/BUnit for code) that cover the logic of changed components.
3. Schedule the integration/E2E automation as concrete tasks in the next sprint and track progress in the feature tasks (add T050..T053).

Approvals
---------
- Product owner: [name]  Date: [signature/date]
- Security lead:  [name]  Date: [signature/date]
- Engineering lead: [name] Date: [signature/date]

Notes
-----
- This waiver is valid only for this feature and must be re-reviewed if the feature scope changes.
