# data-model.md — Entities for ReturnUrl handling

## Entities

### ReturnUrlToken (transient)
- id: GUID
- raw_value: string (original ReturnUrl from request)
- normalized_path: string (validated, normalized path starting with `/`)
- created_at: datetime
- expires_at: datetime (short-lived, e.g., 15 minutes)
- used: boolean (prevent replay)
- user_id: GUID (optional, if we store before authentication)

## Notes
- ReturnUrlToken is optional; implementation can use session or server-side transient store.
- Validation rules: normalized_path must start with `/` and match URL path char whitelist; reject double-encoding.
- Tokens must be short-lived and single-use to mitigate replay attacks.

```
