# research.md

## Goal

Confirm shape of `manifest.webmanifest`, icon sizing and formats, registration approach for a minimal service worker in a Blazor Server app, and a safe caching strategy that explicitly excludes medical/personal API data.

## Decisions

- Decision: Implement a minimal `manifest.webmanifest` and a lightweight `service-worker.js` that caches only static assets required for installability (icons, manifest, app shell static files). Rationale: minimal change, low risk, enables install icon on most devices without offline medical functionality.

- Decision: Register service worker client-side in `_Host.cshtml` (server-rendered entry) using a small script that registers `service-worker.js` only on supporting browsers. Rationale: Blazor Server does not need a complex SW lifecycle; a simple registration suffices.

- Decision: Do NOT cache API responses or user-specific content. Rationale: Security & compliance — medical data must not be cached in browser storage in this phase.

## Alternatives considered

- Adding full offline PWA (service worker caching of data + offline sync) — rejected for Phase 1 due to security, complexity, and constitution requirements.
- Client-side only Blazor WASM PWA — rejected for Phase 1 because the project remains server-rendered for now.

## Implementation notes

- Manifest fields: `name`, `short_name`, `start_url` (use "/"), `display: standalone`, `background_color`, `theme_color`, `icons` with maskable purpose for best cross-platform support.
- Icons: provide 192x192 and 512x512 PNG, include `purpose: any maskable` for Android.
- Service worker: Cache static assets (icons, manifest, main CSS/JS), use cache-first for static files, network-first for anything else (but don't cache API calls).
- Registration: Use a small script that checks `if ('serviceWorker' in navigator)` and registers `service-worker.js` asynchronously.

## Tests to run

- Manual: Install via Chrome/Edge on desktop and Android; Add to Home Screen on iOS (verify manual flow) — confirm icon appears and opens app to landing page.
- Automated: Playwright smoke test to request manifest and confirm service worker registration endpoint reachable and that icons are present.

## Implementation findings (Phase 2)

- Implemented `manifest.webmanifest` and runtime service worker registration in `MainLayout.razor`.
- Created conservative `service-worker.js` that whitelists only static assets (icons, CSS, JS). See reviewer checklist in the file header.
- Populated `quickstart.md` with the current manifest shape and noted SVG icons are used in the repository as placeholders.
- Added Playwright smoke test placeholder at `tests/Playwright/installability.spec.ts` to validate manifest availability and SW registration in environments that support it.

## Risks & Mitigations

- Risk: Service worker accidentally caching API responses → Mitigation: Service worker explicitly filters requests by pathname and only caches whitelisted static paths.
- Risk: iOS does not allow service worker-driven install prompts — Mitigation: provide clear inline instructions and manifest icons; rely on Safari manual Add to Home Screen.

## Outcome

Proceed with Phase 1 design artifacts: `data-model.md` (N/A), `quickstart.md`, and `contracts/README.md`.
