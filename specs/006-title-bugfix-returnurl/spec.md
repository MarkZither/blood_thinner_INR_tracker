```markdown
# Feature Specification: BugFix: ReturnUrl honoured on login

**Feature Branch**: `006-title-bugfix-returnurl`  
**Created**: 2025-11-07  
**Status**: Draft  
**Input**: User description: "When logging in, the return URL should be honoured and the user taken to that page after login, not always redirected to the dashboard. Fix behavior so ReturnUrl parameter (and any safe local URLs) are respected after authentication. Include tests and spec update."

## Summary

When users attempt to access a protected resource while unauthenticated, they are redirected to the login page with a ReturnUrl parameter indicating where they originally intended to go. Currently, after successful authentication, users are always redirected to the dashboard regardless of the ReturnUrl value. This bugfix ensures that safe, valid ReturnUrl values are honoured after login, returning users to their intended destination while blocking potentially malicious external redirects (open redirect vulnerability prevention).

NOTE (scope): This fix is intentionally scoped to the Web project (`BloodThinnerTracker.Web`) and Blazor navigation flows (client-side/server-side). It does NOT require changes to API controllers or the API project. The OAuth/OIDC redirect URI used in Azure app registrations is a separate concern and must not be conflated with the application-level ReturnUrl value used for post-login navigation.

Implementation note: For the immediate fix, the implementation will use a local safe default route value defined in the Web project (for example a code constant). If additional settings are required in a future extension (for example a whitelist), those will be designed and documented separately.

## Actors

- **End User**: Person attempting to access protected resources in the application
- **Authentication System**: Component that handles login flow and post-authentication redirects
- **Protected Resource**: Any page or feature requiring authentication

## Clarifications

### Session 2025-11-07

- Q: Which ReturnUrl validation policy should the system accept as "safe" for post-login redirects? → A: Option A (Relative-only). Note: future extension to support a whitelist (for native app localhost redirects) is intended.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Return to intended page after login (Priority: P1)

A user follows a deep link to a specific page (e.g., medication details, INR test record) while logged out. After successful authentication, the user is returned to that specific page rather than the dashboard.

**Why this priority**: This is the core bug fix and directly impacts user experience for accessing bookmarked links and deep navigation flows.

**Independent Test**: Access a protected URL while logged out, complete login, and verify landing on the originally requested page with all query parameters and fragments preserved.

**Acceptance Scenarios**:

1. **Given** user is logged out, **When** user clicks link to `/medications/123` and completes login, **Then** user lands on `/medications/123` page showing the specific medication
2. **Given** user is logged out, **When** user accesses `/inr-tests?filter=recent` and logs in, **Then** user lands on `/inr-tests?filter=recent` with query parameters intact
3. **Given** user is logged out, **When** user follows link `/settings#notifications` and authenticates, **Then** user lands on settings page scrolled to notifications section (fragment preserved)

---

### User Story 2 - Block unsafe redirect attempts (Priority: P1)

When a ReturnUrl contains an external domain or malicious redirect pattern, the system must reject it and redirect to the safe default (dashboard) to prevent open redirect vulnerabilities.

**Why this priority**: Security issue - prevents phishing attacks and malicious redirects that could compromise user accounts.

**Independent Test**: Attempt login with ReturnUrl pointing to external domain; verify redirect goes to dashboard and attempt is logged.

**Acceptance Scenarios**:

1. **Given** login page has ReturnUrl=`https://malicious.com`, **When** user logs in, **Then** user is redirected to dashboard (not external site) and security event is logged
2. **Given** ReturnUrl contains double-encoded malicious payload, **When** user authenticates, **Then** system detects invalid encoding and redirects to dashboard
3. **Given** ReturnUrl is `//evil.example.com` (protocol-relative), **When** login succeeds, **Then** system treats as external and redirects to safe default

---

### User Story 3 - Handle missing or empty ReturnUrl gracefully (Priority: P2)

When ReturnUrl parameter is missing, empty, or null, users are redirected to the default landing page (dashboard) after login.

**Why this priority**: Common scenario for direct login page access; ensures consistent user experience.

**Independent Test**: Access login page directly without ReturnUrl parameter and verify redirect to dashboard after successful authentication.

**Acceptance Scenarios**:

1. **Given** user navigates directly to `/login`, **When** user successfully authenticates, **Then** user is redirected to dashboard
2. **Given** ReturnUrl is empty string, **When** authentication completes, **Then** user lands on dashboard

---

### Edge Cases

- **ReturnUrl points to page requiring higher permissions**: User authenticates successfully but lacks permission for ReturnUrl target → redirect to dashboard or show permission denied page
- **ReturnUrl contains XSS payload in query string**: System validates and sanitizes URL; if validation fails, redirect to safe default
- **Multiple sequential redirects**: User clicks link while logged out, gets redirected to login with ReturnUrl, abandons login, later returns via another protected link → most recent ReturnUrl should be used
- **ReturnUrl exceeds maximum length**: System truncates or rejects overly long URLs and redirects to safe default
- **Same-origin but different port**: Current policy is relative-only; absolute URLs are rejected. Future extension: whitelist may be used to allow localhost/native app redirects (e.g., `http://localhost:PORT`) when safe.
- **Case sensitivity in URL validation**: System handles case-insensitive domain matching while preserving case in path/query for final redirect

### Fragment handling note

- URL fragments (the portion after `#`) are not sent to servers by browsers. For this bugfix:
	- For client-side (Blazor WebAssembly) navigation flows, fragments are preserved automatically by the browser and client NavigationManager; the `returnUrl` query parameter may include a fragment portion and the client should navigate preserving it.
	- For server-side flows (Blazor Server or server redirects), servers cannot read fragments; implementers must preserve path+query server-side and, if fragment restoration is required, rely on client-side logic to append the fragment after redirect or include an explicit fragment round-trip mechanism (out of scope). Tests should assert fragment behavior appropriate to the hosting model.

### Double-encoding detection examples

- Reject (example): `returnUrl=%252F%2Fevil.com` — decodes once to `%2F%2Fevil.com` which decodes again to `//evil.com` (protocol-relative) → reject.
- Accept (example): `returnUrl=%2Fsettings%3Ftab%3Dnotifications` — decodes once to `/settings?tab=notifications` → accept if relative.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST capture and preserve ReturnUrl parameter when redirecting unauthenticated users to login page
- **FR-002**: System MUST validate ReturnUrl before using it for post-authentication redirect, checking: URL is well-formed and is a relative path (starts with `/`). Absolute URLs are rejected by default; future whitelist support can be added to permit specific absolute hosts (e.g., localhost for native app redirects) but is out of scope for this fix.

### Canonical validation rule (authoritative)

- The authoritative validation rule for this feature is:
	1. Decode the incoming `returnUrl` value exactly once.
	2. Accept the decoded value only if it is a relative path that begins with a single forward slash (`/`) and does not include a scheme or authority component.
	3. Reject and treat as invalid any value that:
		 - parses as an absolute URI with a scheme (e.g., `https://`, `http://`, `mailto:`),
		 - begins with `//` (protocol-relative),
		 - contains `javascript:` or `data:` schemes, or
		 - after one decode contains percent-encoded sequences that would yield a leading `/` or a scheme when decoded again (see examples below).

This canonical rule is the single source of truth for validators and tests. The plan and tasks MUST reference this rule rather than restating variant language.
- **FR-003**: System MUST redirect authenticated users to validated ReturnUrl destination, preserving query parameters and URL fragments
- **FR-004**: System MUST redirect to configured safe default (dashboard) when ReturnUrl is missing, invalid, or fails security validation
- **FR-005**: System MUST log security events when invalid or potentially malicious ReturnUrl values are detected and blocked (use existing logging infrastructure)
- **FR-006**: ReturnUrl in this implementation is passed as a validated query string parameter and does not require server-side token storage. If server-side storage is used in future, the system MUST ensure single-use invalidation; for this bugfix no TTL or token lifecycle is required.
- **FR-007**: System MUST include automated tests covering: valid relative URLs, external URLs (blocked), malformed URLs (blocked), missing ReturnUrl, XSS payloads (blocked), double-encoded URLs (blocked)

### Key Entities

- **ReturnUrl Parameter**: URL string passed to login page indicating post-authentication destination; attributes include raw value, validated/normalized form, validation result (safe/unsafe)
- **Authentication Session**: User session created upon successful login. NOTE: For this bugfix the implementation is explicitly querystring-only — no server-side ReturnUrl storage or short-lived token will be used. Implementers MUST read and validate the `returnUrl` query parameter at post-login time and not rely on persisted ReturnUrl state.
- **Safe Default Route**: Configured fallback destination (dashboard) used when ReturnUrl is absent or invalid

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of test cases with valid local ReturnUrl successfully redirect to intended destination in automated test suite
- **SC-002**: 0% of test cases with external or malicious ReturnUrl bypass security validation (all blocked attempts redirect to safe default)
- **SC-003**: Automated tests achieve coverage of at least 7 ReturnUrl scenarios: valid relative, valid absolute same-origin (if whitelist enabled), external, malformed, missing, XSS payload, double-encoded
- **SC-004**: Post-authentication redirect time remains within 10% of current baseline (no performance regression)
- **SC-005**: Security logging captures 100% of blocked ReturnUrl attempts with sufficient detail for security audit

## Assumptions

- Application has a defined safe default route (dashboard) for fallback redirects
- Security logging infrastructure exists to record blocked redirect attempts
- ReturnUrl validation can leverage existing URL parsing and validation utilities
- Relative URLs (starting with `/`) are considered safe by default
- Current policy: relative-only for ReturnUrl validation; whitelist extension may be added later to support localhost/native app redirects
- Query parameters and URL fragments do not require additional sanitization beyond URL validation

## Tests and Validation

### Unit Tests
- URL validation logic for all ReturnUrl patterns (relative, absolute same-origin if whitelist enabled, external, malformed)
- URL normalization and sanitization functions
- ReturnUrl parameter extraction and storage

### Integration Tests
- End-to-end flows: access protected resource → redirect to login with ReturnUrl → authenticate → verify landing page
- Security tests: attempt logins with malicious ReturnUrl values and verify blocks
- Edge case scenarios: missing ReturnUrl, permission mismatches, URL length limits

### Security Tests
- Open redirect vulnerability tests with external domains
- XSS payload injection attempts in ReturnUrl
- Double-encoding attack patterns
- Protocol-relative URL attack vectors

## Logging Schema (for security events)

When invalid or potentially malicious ReturnUrl values are detected and blocked (FR-005), the system MUST emit an Error-level security log event with the following minimal schema to support deterministic test assertions and security audits:

- EventId: string (unique identifier for event type, e.g., ReturnUrlBlocked)
- Timestamp: ISO-8601 timestamp
- TraceId: string (correlation id from request context if available)
- UserId: string|null (if authenticated) or null for anonymous
- RawReturnUrl: string (the raw `returnUrl` parameter value observed)
- ValidationResult: string (e.g., "invalid-scheme", "protocol-relative", "double-encoded", "malformed")
- RequestPath: string (the path of the request that carried the returnUrl)

Notes:
- Do not log secrets; if RawReturnUrl contains sensitive tokens (unexpected), redact or truncate per repository redaction policy.
- Tests should assert on the presence of the EventId and ValidationResult and may assert that RawReturnUrl contains the blocked value (or redacted placeholder) depending on test environment.

## Fragment & Hosting Model Notes (clarified)

- Blazor WebAssembly (client-side): Browser preserves URL fragments; when client collects `returnUrl` from querystring and navigates after login, the fragment should be preserved by client NavigationManager. Tests for WASM should assert final URL includes expected fragment.
- Blazor Server / server redirects: Browsers do not send fragments to the server. Implementations must preserve path+query server-side and, if fragment restoration is required, rely on client-side logic to append the fragment after redirect (out of scope). Tests for Server must assert that path+query are preserved; fragment restoration assertions are optional and must be gated behind an explicit client-side implementation note.
```
