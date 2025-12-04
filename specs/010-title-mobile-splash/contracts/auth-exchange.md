# Auth Exchange Contract

POST /api/auth/exchange

Description: Exchange a provider-issued ID token (e.g. Azure AD id_token) for an internal bearer token used by the BloodThinnerTracker API. The API validates the incoming id_token and returns an internal access token + refresh token pair for the client to use.

Request
- Content-Type: application/json
- Body:

```json
{
  "provider": "azuread",
  "id_token": "<id_token_jwt>",
  "deviceId": "optional-device-id"
}
```

Responses
- 200 OK: { accessToken, refreshToken, expiresIn, user }
- 400 Bad Request: invalid payload
- 401 Unauthorized: token validation failed
- 500 Internal Server Error: validation or server error

Validation rules
- The server will validate the id_token signature and issuer using the configured authority keys.
- External user id will be derived using the server-side claim precedence implementation (`IdTokenValidationService`): prefer `oid` claim, fallback to `sub` if `oid` is missing or empty.
- Email claim is optional but used to match existing users when available.

Notes
- Clients should cache the authority configuration (T046) provided by the API and not hard-code differing authority values.
- This contract is intentionally minimal — see `specs/010-title-mobile-splash/tasks.md` for test and integration requirements.
# Auth Token Exchange Contract

## Purpose

Exchange an external OIDC `id_token` (obtained from Azure AD or Google via MSAL) for an internal bearer token used for API authorization.

## Endpoint

```
POST /api/auth/exchange
```

**Authentication**: None (public endpoint; validates id_token signature instead)

## Request

### Body (application/json)

```json
{
  "id_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "provider": "azure"
}
```

### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id_token` | string | Yes | OIDC id_token from external provider (Azure AD or Google). Must be a valid JWT. |
| `provider` | string | Yes | OAuth provider identifier: `"azure"` or `"google"`. Selects the correct validation key and issuer. |

## Response

### Success (200 OK)

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 900,
  "issued_at": "2025-11-21T12:00:00Z"
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `access_token` | string | Internal opaque bearer token for API authorization. Mobile clients store securely and include in `Authorization: Bearer <token>` header on API requests. |
| `token_type` | string | Always `"Bearer"`. |
| `expires_in` | integer | Token lifetime in seconds (recommended: 900 for 15 minutes). |
| `issued_at` | string (ISO8601) | UTC timestamp when token was issued. |

## Validation Rules (Server-Side)

The backend MUST validate the following before issuing an internal bearer token:

1. **Request Format**: Valid JSON with required `id_token` and `provider` fields
2. **JWT Structure**: `id_token` must be a valid JWT (3 parts separated by dots)
3. **Signature**: JWT signature must be valid using the provider's public keys (JWKS endpoint)
4. **Issuer (iss)**: Must match the configured issuer for the provider
   - Azure AD: `https://login.microsoftonline.com/{tenantId}/v2.0` or `https://login.microsoftonline.com/{tenantId}`
   - Google: `https://accounts.google.com`
5. **Audience (aud)**: Must match the configured OAuth client ID
6. **Expiry (exp)**: Token must not be expired (current time < exp)
7. **Issued At (iat)**: Token must not be from the future (allow clock skew: ±30 seconds)
8. **Subject (sub)**: Must be present and non-empty (unique user identifier)
9. **Account Status**: User account must be active and authorized to access application

## Error Responses

### 400 Bad Request

Invalid request format or missing required fields.

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Field 'id_token' is required.",
  "traceId": "0HMVGN8J8P4PI:00000001"
}
```

### 401 Unauthorized

Token validation failed (invalid signature, expired, wrong issuer/audience, or user disabled).

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "id_token signature validation failed for provider 'azure'.",
  "traceId": "0HMVGN8J8P4PI:00000002"
}
```

### 500 Internal Server Error

Server error during validation or token generation.

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred during token exchange.",
  "traceId": "0HMVGN8J8P4PI:00000003"
}
```

## Security Considerations

- **Transport**: HTTPS/TLS 1.2+ required for all requests
- **Logging**: Do NOT log raw `id_token` in production. Use masked logging for sensitive claims (email, names)
- **Token Storage**: Internal bearer token MUST be stored securely (platform `SecureStorageService`)
- **Token Lifetime**: Short-lived tokens (15 min) reduce impact of token compromise
- **Signature Validation**: Always validate JWT signature against provider's JWKS endpoint (cache public keys for performance)
- **Clock Skew**: Allow ±30 seconds tolerance for token expiry (system clock differences)
- **Rate Limiting**: Implement rate limiting to prevent brute-force or abuse
- **Audit Trail**: Log token issuance events (user, provider, timestamp) for security audit

## Token Lifecycle & Refresh

### Access Token Lifetime

- **TTL**: 15 minutes (900 seconds) recommended for security
- **Refresh Strategy (MVP)**: 
  - When token expires, mobile client initiates fresh OAuth sign-in flow
  - Repeats MSAL `AcquireTokenInteractive()` → POST to `/auth/exchange` workflow
- **Refresh Strategy (Future)**:
  - Issue refresh tokens for automatic silent re-exchange without user interaction
  - Implement `POST /auth/refresh` endpoint
  - Support token revocation (`POST /auth/revoke`)

## Client-Side Implementation

**Mobile Flow**:
```
1. Invoke MSAL.AcquireTokenInteractive() → obtains id_token from Azure AD / Google
2. POST id_token to /api/auth/exchange → receives internal bearer token
3. Store access_token securely via ISecureStorageService (AES-256 + platform keystore)
4. Use access_token in all API calls: Authorization: Bearer <token>
5. On token expiry: repeat from step 1
```

## Testing Strategy

### Unit Test Scenarios

✅ Valid id_token with valid signature → returns 200 with bearer token
✅ Invalid id_token signature → returns 401 Unauthorized
✅ Expired id_token (exp < now) → returns 401 Unauthorized
✅ Missing id_token field → returns 400 Bad Request
✅ Invalid provider value → returns 400 Bad Request
✅ Mismatched issuer (iss claim) → returns 401 Unauthorized
✅ Mismatched audience (aud claim) → returns 401 Unauthorized
✅ User account disabled → returns 401 Unauthorized

### Integration Test Scenarios

1. End-to-end: MSAL sign-in → token exchange → authenticated API call using bearer token
2. Multiple providers (Azure AD + Google) with different key material
3. Token expiry behavior (should reject expired tokens)
4. Clock skew handling (token with iat in near future should succeed)
5. Error scenarios: malformed JWT, wrong key, wrong tenant, etc.

## Example Curl Request

```bash
curl -X POST https://api.bloodthinner.local/api/auth/exchange \
  -H "Content-Type: application/json" \
  -d '{
    "id_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyXzEyMyIsImlhdCI6MTcwMDY0NDYwMCwiZXhwIjoxNzAwNjQ1NTAwfQ.signature",
    "provider": "azure"
  }'
```

## Implementation Checklist

| Item | Status |
|------|--------|
| Endpoint created in AuthController | ✅ |
| Request deserialization (AuthExchangeRequest DTO) | ✅ |
| JWT signature validation (JWKS caching) | ✅ |
| Claims validation (iss, aud, exp, iat, sub) | ✅ |
| User account lookup and status check | ✅ |
| Internal bearer token generation | ✅ |
| Response serialization (AuthTokenResponse DTO) | ✅ |
| Error handling and HTTP status codes | ✅ |
| Structured logging (audit trail) | ✅ |
| Unit tests (>= 8 scenarios) | ✅ |
| Integration tests (end-to-end flow) | ⏳ Future |
| Rate limiting / DDoS protection | ⏳ Future |
| Token revocation support | ⏳ Future |
- `200 OK` or `201 Created` with the JSON body containing `access_token`, `token_type`, and `expires_in`.

Client error responses (examples):
- `400 Bad Request` — malformed request (missing `id_token` or invalid JSON). Response body:

```json
{ "error": "invalid_request", "error_description": "id_token is required" }
```

- `401 Unauthorized` — `id_token` validation failed (signature, issuer, audience, or expiry). Response body:

```json
{ "error": "invalid_token", "error_description": "id_token signature invalid or expired" }
```

- `403 Forbidden` — account exists but is blocked or lacks required status (e.g., suspended). Response body:

```json
{ "error": "account_forbidden", "error_description": "User account is suspended" }
```

- `422 Unprocessable Entity` — token structurally valid but fails business validation (e.g., missing required claims).

- `429 Too Many Requests` — rate limiting triggered for token-exchange attempts.

Server error responses:
- `500 Internal Server Error` — unexpected failure. Response body SHOULD include a request `trace_id` for debugging (do not return sensitive details).

Security note: Do not include raw `id_token` contents or PII in error responses. Use masked logging with a `trace_id` to correlate logs.

