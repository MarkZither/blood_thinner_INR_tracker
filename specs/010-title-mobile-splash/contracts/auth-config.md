# Auth Config Contract

Endpoint: `GET /api/auth/config`

Response: 200 OK
Content-Type: application/json

Schema: `BloodThinnerTracker.Shared.Models.Authentication.OAuthConfig`

Fields:
- `fetchedAt` (string, RFC3339) - timestamp when API generated the configuration
- `providers` (array) - list of provider configurations
  - `provider` (string) - provider id (e.g., "azure", "google")
  - `clientId` (string) - public client id for mobile/native use
  - `redirectUri` (string) - platform redirect URI or scheme
  - `authority` (string) - OAuth authority URL (e.g., `https://login.microsoftonline.com/{tenant}`)
  - `scopes` (string) - space separated scopes to request

Behaviour and guarantees:
- The API is the single authoritative source for OAuth provider configuration and MUST include the `authority` value used by clients to build authorization URLs and validate tokens.
- Clients (Mobile / Web) should fetch the config at startup and cache it locally for offline/startup performance.
- Clients MUST retry to fetch the config if HTTP fetch fails and use the last-known cached configuration if available.
- Cache lifetime is implementation-specific, but the API MAY update config infrequently (e.g., when tenant/authority changes).

Security:
- No secrets (client secrets) should be returned to clients. Only public client IDs and authority endpoints are returned.
- The API should only return values that are safe for public clients.

Acceptance criteria:
- Mobile and Web clients fetch `/api/auth/config` at startup and cache the response.
- If API is unavailable during startup, clients use cached configuration and continue startup.
- Changing `Authentication:AzureAd:Instance` or `TenantId` in API configuration results in new `authority` returned by the endpoint; clients pick it up after next successful fetch.
