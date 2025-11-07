# research.md — ReturnUrl validation policy and safe redirect handling

## Decision: ReturnUrl validation policy — Relative-only

Rationale:
- Relative-only ReturnUrl (paths starting with `/`) is the simplest, lowest-risk policy to prevent open-redirect abuses. It ensures the application never performs a client-side redirect to an external domain based on untrusted input, removing the attack surface for token theft or phishing redirects.
- Matches the user's instruction that this is a bugfix within the current architecture and should not introduce new auth flows.
- Future extension: a whitelist approach can be introduced later to allow specific absolute URLs (for example, `http://localhost:PORT` for native app redirects) under controlled configuration and with additional safety checks.

Alternatives considered:
- Same-origin (host-only): slightly more permissive but requires correct origin detection and risk of port mismatches; more complex to validate consistently across environments and proxies.
- Same-origin (host+port): strict but more brittle; local dev ports and native redirects often vary.
- Whitelist of hosts: flexible and allows localhost/native, but requires secure configuration and ops process; deferred for future enhancement.

Security implications:
- Relative-only policy eliminates open-redirect attacks that could exfiltrate tokens via redirect-to-external-site flows.
- Tests should include attempts to use absolute URLs, protocol-relative URLs, double-encoded payloads, and XSS payloads to ensure they are rejected.

Operational notes:
- If whitelist extension is later required, it must be implemented with explicit configuration bound to options POCOs and feature-flag controlled rollout.

## Research tasks produced
- Verify existing login redirect codepath in `BloodThinnerTracker.Web` and `BloodThinnerTracker.Api` to find where ReturnUrl is captured and applied.
- Identify tests covering login redirect flow and add negative tests for external ReturnUrl patterns.
- Determine whether ReturnUrl is passed via query param or via state/cookie in the current implementation; prefer storing normalized ReturnUrl in session/state server-side rather than trusting client-side values on POST.

## Findings summary
- Adopt relative-only policy now; plan for configurable whitelist in a later minor feature.

```
