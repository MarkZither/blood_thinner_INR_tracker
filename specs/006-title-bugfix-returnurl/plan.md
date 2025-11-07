# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Fix ReturnUrl handling so that, after authentication, valid relative ReturnUrl values are honoured and users are redirected to their intended destination. The fix must be applied within the current architecture (ASP.NET Core / Blazor / API projects) and must not introduce open-redirect vulnerabilities or new authentication flows. Testing must include negative security tests for token exfiltration attempts, double-encoding, protocol-relative URLs, and XSS payloads.

Scope clarification: The implementation recommended here is Web-focused and limited to `BloodThinnerTracker.Web` (Blazor pages and navigation flows). No API controller changes are required. Do NOT conflate this ReturnUrl (an internal, relative page path) with the OAuth/OIDC redirect URI configured in Azure — those are separate and outside the scope of this bugfix.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (project is .NET-based per repository conventions)  
**Primary Dependencies**: ASP.NET Core, Entity Framework Core, MudBlazor (frontend), xUnit/BUnit/Playwright for tests  
**Storage**: ReturnUrl will be passed as a validated query string parameter in the Web flow for this bugfix. This implementation is querystring-only: server-side ReturnUrl storage or short-lived tokens are NOT used. If a future extension requires server-side storage, it MUST be single-use, short-lived, and fully documented in the plan and tasks.
**Testing**: xUnit for unit tests, Playwright for integration browser tests, BUnit for Blazor component tests  
**Target Platform**: Web (Blazor Server / WASM client interactions) and API endpoints hosted on .NET services  
**Project Type**: Web app + API (existing repository structure)  
**Performance Goals**: No perceptible user latency increase; redirect validation must be constant-time relative to URL length and not introduce blocking IO on the hot path  
**Constraints**: Must follow Constitution (logging, test coverage >= 90%, OWASP security posture). ReturnUrl will be handled as a relative querystring value in this fix; configuration changes (if any) will be scoped to future work and documented separately.
**Scale/Scope**: Small bugfix scoped to a single sprint (2-3 days dev + CI tests); changes limited to authentication redirect codepath and test suites

## Constitution Check

Gates to verify (all must pass):

- Configuration access uses strongly-typed options (Constitution VII) → PASS (plan will use IOptions for any new config)
- Code quality and .NET standards (Constitution I) → PASS (unit tests and analyzers will be run)
- Testing discipline (Constitution II) → PASS (tests added; aim to keep coverage)
- Security & OWASP compliance (Constitution V) → PASS (open redirect prevention is primary objective; security tests included)
- Feature sizing (Constitution VIII) → PASS (bugfix scoped small)

All gates are satisfied by the proposed approach; no constitutional violations detected.

## Project Structure

Documentation for this feature (created by this plan):

```
specs/006-title-bugfix-returnurl/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── login.yaml
└── checklists/
    └── requirements.md
```

Source changes (scoped):

```
src/
├── BloodThinnerTracker.Web/          # adjust login page handling and redirect token flow here (Web-only)
└── tests/
    ├── BloodThinnerTracker.Web.Tests/    # add unit tests for validation logic
    └── playwright/                        # Playwright test harness and test specs
```

**Structure Decision**: Use existing web + API projects; no new projects added. Changes limited to authentication redirect flow and tests.

## Complexity Tracking

No constitution violations requiring justification were identified. Changes are minimal and localized to the authentication codepath.
