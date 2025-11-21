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

### Key management guidance (implementation MUST follow these constraints)

- **Per-device key derivation**: Derive a per-device data-encryption key (DEK) using a platform-protected root secret (e.g., Keychain/Keystore). Use a KDF such as HKDF or PBKDF2 to derive the DEK from the root secret and a device-unique salt. Do NOT store raw DEKs in plain text.

- **Authenticated encryption**: Use `AesGcm` (or equivalent AEAD) with Associated Authenticated Data (AAD). Include AAD fields such as `version` and `deviceId` to bind ciphertext to a specific app version and device.

- **IV/Nonce & Tag handling**: Generate a unique nonce/IV per encryption operation (recommended 12 bytes for AesGcm) and store the IV and authentication tag alongside the ciphertext in `CachedInrBundle`.

- **Key rotation & migration**: Implement key versioning. When rotating keys, the app MUST be able to re-encrypt existing cached bundles with the new key or keep previous keys available to decrypt until migration completes. Add metadata in the cache blob indicating `keyVersion` and the algorithm used.

- **Storage of wrapping keys**: The DEK may be wrapped (encrypted) by a key stored in platform `SecureStorage` or protected by the OS keystore. Wrapping reduces exposure of the raw DEK.

- **Testing requirements**: Unit tests MUST include encrypt/decrypt roundtrips, tamper-detection (invalid tag/iv), and rotation/migration scenarios. Integration tests SHOULD verify cross-version compatibility when keyVersion changes.

- **Operational notes**: On sign-out or device reset, ensure wrapped keys are removed from platform secure storage and cached blobs are deleted to prevent unauthorized access.
