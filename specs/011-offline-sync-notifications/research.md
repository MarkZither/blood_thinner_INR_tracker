# Phase 0 — Research: Offline Sync & Notifications

Decision summary
- Client mode: Read-only for this phase — server is canonical (confirmed).  
- Sync triggers: Combined strategy — foreground refresh on open, periodic background sync, manual "Sync Now".  
- Background interval: Default 1 hour; 15 minutes when charging + on Wi‑Fi.  
- Notifications: Platform-specific scheduling (Android JobService/WorkManager, Windows background task). Basic single-reminder notifications for scheduled medicine times in this phase.

Questions investigated

1) Which API endpoints exist for canonical data?
- Expected: `GET /api/medications`, `GET /api/medication-logs?days=...`, `GET /api/inr-tests?days=...`, `GET /api/health`.
- Action: Validate these endpoints exist and return the canonical payload shape (see `contracts/openapi.yaml`).

2) Does mobile background worker persist across reboot and can it schedule notifications?
- The repo already contains Android JobService and BroadcastReceiver implementations. Confirmed code paths exist to schedule jobs and post notifications. Will add unit/integration checks to ensure startup re-registration after reboot.

3) Retry/backoff and failure handling
- Use exponential backoff with capped retries for network errors (configurable in options). Foreground refresh provides immediate retry opportunities. Background worker should log failures and expose diagnostic telemetry.

4) Security & privacy
- All transport over HTTPS. Local data stored in SQLite with file-level encryption if available (platform keychain). Ensure no plaintext secrets in templates.

Rationale
- Read-only phase reduces complexity and avoids conflict resolution. Combined sync strategy balances freshness and battery impact. Wi‑Fi+charging fast path allows quick updates when device is idle and power is available.

Next actions from research
- Implement small API contract checks and add integration tests asserting endpoints and payload shapes.  
- Add options configuration for background interval and fast-path triggers (Wi‑Fi + charging).  
- Add telemetry/logging for sync outcomes and failure reasons.
