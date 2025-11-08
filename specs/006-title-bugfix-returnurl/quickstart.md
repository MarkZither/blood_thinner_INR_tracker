# Quickstart: Validate and honour ReturnUrl after login

1. Checkout branch `006-title-bugfix-returnurl`.
2. Run unit tests for authentication module: `dotnet test src/BloodThinnerTracker.Api.Tests` (adjust project path as needed).
3. Run integration tests for web login flow using Playwright (.NET) or local browser tests.
   - Playwright tests live under `tests/playwright/returnurl` and use the .NET Playwright harness (Microsoft.Playwright).
   - Example (from repo root) to run the C# Playwright/e2e tests (scaffolded project `tests/BloodThinnerTracker.Web.e2e.Tests`):

```powershell
dotnet test tests/BloodThinnerTracker.Web.e2e.Tests --filter Category=ReturnUrl
```
   - If the repository uses a dedicated Playwright test project path (e.g., `tests/Playwright/ReturnUrlTests`), adjust the `dotnet test` target accordingly.
4. Verify ReturnUrl behaviour:
   - Access `/medications/123` while logged out → redirected to `/account/login?ReturnUrl=%2Fmedications%2F123`
   - After successful login, you should land on `/medications/123`.
5. Negative tests:
   - Try `/account/login?ReturnUrl=https://malicious.example.com` → should redirect to dashboard after login.
   - Try encoded payloads and protocol-relative URLs → should be rejected.

Notes:
- The immediate bugfix uses the querystring-only approach and a local safe default route in code; configuration-based whitelisting is out-of-scope for the MVP and should follow repository conventions if added later.
- Playwright tests use the `_test/logs` endpoint to assert that blocked ReturnUrl attempts are logged deterministically. The test scaffold queries `http://localhost:5000/_test/logs` after exercising the login flow to assert logged security events.

Playwright specifics:

- Test cases to include:
   1. Positive redirect: unauthenticated GET to `/medications/123` → login → complete login → lands on `/medications/123` with content present
   2. Query params preserved: `/inr-tests?filter=recent` preserved after login
   3. Fragment preserved: `/settings#notifications` preserved after login
   4. External URL blocked: `ReturnUrl=https://malicious.example.com` redirects to dashboard
   5. Protocol-relative and double-encoded payloads rejected
   6. Token replay protection: used token should not redirect again

- Playwright expectations:
   - Tests should assert final URL and presence of expected page content
   - Tests should assert that dashboard is used as fallback for blocked ReturnUrl values
   - Negative tests should assert no external navigation occurs during the login flow

If you want, I can scaffold the Playwright tests under `specs/006-title-bugfix-returnurl/tests` next.
