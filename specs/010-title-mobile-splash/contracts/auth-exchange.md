# Auth Token Exchange Contract

Purpose: describe the contract for exchanging an external OIDC `id_token` for an internal bearer token used by the API.

Endpoint: `POST /auth/exchange`

Request (application/json):

{
  "id_token": "<JWT id_token from external provider>",
  "provider": "azure|google",
  "client_info": { "platform": "android|windows", "app_version": "1.0.0" }
}

Response (201 Created or 200 OK):

{
  "access_token": "<internal bearer token>",
  "token_type": "Bearer",
  "expires_in": 3600,
  "issued_at": "2025-11-21T12:00:00Z"
}

Validation rules (server-side):
- Validate `id_token` signature, issuer, audience, and expiry.
- Validate that the subject (sub) maps to an active user account.
- Optionally validate required scopes/claims or MFA status before issuing internal token.
- Record token issuance audit/event for security tracing.

Security notes:
- Transport MUST be TLS 1.3.
- Do not log raw `id_token` in production logs. Use masked logging for PII-sensitive fields.
- Use short-lived internal tokens and support revocation.

Test guidance:
- Integration tests should provide a signed `id_token` fixture (or a mocked validation layer) and verify the server returns an internal token and that subsequent API calls succeed when using that token.

## Token lifecycle & refresh

- **Access token TTL**: The backend SHOULD issue short-lived internal `access_token`s (recommended TTL: 15 minutes). The exact `expires_in` value MUST be returned in the response body.
- **Refresh strategy**: For long-lived sessions the backend MAY issue a `refresh_token` (opaque) or support repeated `id_token` re-exchange. If a `refresh_token` is issued it MUST be revocable server-side, and clients MUST store it securely (platform `SecureStorage`). The server MUST document refresh endpoints (e.g., `POST /auth/refresh`) or allow `POST /auth/exchange` to accept a fresh `id_token` to re-issue an access token.
- **Token revocation**: The backend MUST support token revocation and provide an administrative or user-initiated revocation endpoint (e.g., `POST /auth/revoke`) and an audit trail for revocations.

## Error responses and status codes

Successful responses:
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

