# Phase 1 — Data Model

This document defines the minimal domain model needed by the mobile client (read-only recent INR list) and the local cache metadata.

## Entities

### INRTest
- **Id**: `string` (UUID) — unique identifier for the test record.
- **UserId**: `string` (UUID) — owner of the test (server-side); mobile may ignore if token-scoped.
- **Value**: `decimal` — measured INR value (e.g., 2.7).
- **Units**: `string` — typically `INR`.
- **CollectedAt**: `DateTimeOffset` — when sample was collected (UTC).
- **ReportedAt**: `DateTimeOffset?` — when result was reported by lab (optional; UTC).
- **Notes**: `string?` — optional clinician note or comment.

Validation rules:
- `Value` required; recommended range 0.5 .. 8.0 (client validates and shows warning above/below safe range).
- `CollectedAt` and `ReportedAt` use `DateTimeOffset`/ISO8601 UTC strings.

### CachedInrBundle

The mobile app writes a single or small number of encrypted cache blobs containing an array of recent `INRTest` objects. The encrypted blob is stored as bytes (or Base64 string) in local storage; encryption keys are managed via secure storage.

- **CacheId**: `string` (UUID)
- **EncryptedPayload**: `byte[]` (or Base64) — AES-256 authenticated ciphertext containing JSON array of `INRTest` items
- **Iv**: `byte[]` — initialization vector / nonce used for encryption (store alongside payload)
- **Tag**: `byte[]` — authentication tag when using `AesGcm` (if applicable)
- **CachedAt**: `DateTimeOffset` — when the cache was created (UTC)
- **ExpiresAt**: `DateTimeOffset` — `CachedAt + 7 days`
- **Source**: `string` — e.g., `"api"` or `"mock"`

Cache semantics:
- When reading cache, compute `Age = Now - CachedAt`. Show a staleness warning when `Age > 1 hour` but allow reading until `ExpiresAt`.
- On sign-out or explicit cache clear, delete `CachedInrBundle` and remove AES key from `SecureStorage`.

## Mobile DTOs (app-side)

INRTestDto (transport-friendly):
- `id: string`
- `value: number` (decimal)
- `collectedAt: string` (ISO8601)
- `reportedAt?: string` (ISO8601)
- `notes?: string`

CachedBundleDto:
- `cacheId: string`
- `cachedAt: string` (ISO8601)
- `expiresAt: string` (ISO8601)
- `source: string`

## Notes on encryption/key management
- Generate a random AES-256 key on first-run; store the AES key encrypted by platform `SecureStorage` (which uses the OS keystore / Keychain).
- Use `AesGcm` for authenticated encryption where available; store IV and Tag with the ciphertext.
- Do not persist raw keys in plain files or logs.
