# Implementation Plan: Install web app shortcut (Phase 1)

**Branch**: `008-install-web-app` | **Date**: 2025-11-12 | **Spec**: `specs/008-install-web-app/spec.md`
**Input**: Feature specification from `specs/008-install-web-app/spec.md`

## Summary

Implement Phase 1 PWA installability for the Blazor Web (server-rendered) application: add a web app manifest and a minimal service worker that enables installation (add-to-home/desktop) and caches only non-sensitive static assets required for the install UX (icons, manifest, app shell assets). No offline application logic, medical data caching, or background sync will be implemented in this phase.

This work is intentionally small and limited: it enables users to "install" the web app as an icon on supported devices and browsers while preserving the existing server-rendered architecture and security posture.

## Technical Context

**Language/Version**: .NET 10 (C# 13) server-rendered Blazor Web app (InteractiveServer)  
**Primary Dependencies**: MudBlazor (existing), ASP.NET Core, static assets pipeline  
**Storage**: N/A for Phase 1 — no new persisted data or DB changes  
**Testing**: Manual device/browser installation tests; Playwright for automated install verification (smoke), BUnit not required for this change  
**Target Platform**: Modern browsers on Android, iOS (Safari), and Windows (Edge/Chrome) that support web app installation  
**Project Type**: Web application (Blazor Server)  
**Performance Goals**: Minimal impact — service worker should not increase first-load time significantly; installability must succeed under typical network conditions  
**Constraints**: No offline functionality; do not cache or store any personal/medical data in service worker or IndexedDB; manifest and icons must meet platform size/format expectations  
**Scale/Scope**: Small feature — estimated 4-8 hours of work (add manifest, icons, service worker registration, tests, docs)

## Constitution Check

All Constitution gates apply. Key checks for this feature:

- Principle I (Code Quality & .NET Standards): Changes are limited to static files and minimal server-side registration; follow repository conventions. PASS (no code changes to core logic).
- Principle II (Testing Discipline & Coverage): Add Playwright smoke tests for installability. PASS (tests added as smoke/integration, not affecting 90% coverage requirement).
- Principle V (Security & OWASP): MUST NOT cache or persist sensitive medical data. Service worker must explicitly exclude API calls and personal data. PASS if implemented as documented.
- Principle VII (Configuration Access & Options): No new secrets or config entries needed. PASS.

GATE RESULT: No Constitution violations expected. Proceed to Phase 0 research and Phase 1 design.

## Project Structure

### Documentation (this feature)

```
specs/008-install-web-app/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── README.md
└── tasks.md (created later by /speckit.tasks)
```

### Source Code (repository root)

Add the following static assets and small registration snippet in the Blazor project:

```
src/BloodThinnerTracker.Web/wwwroot/manifest.webmanifest
src/BloodThinnerTracker.Web/wwwroot/service-worker.js
src/BloodThinnerTracker.Web/wwwroot/icons/icon-192.png
src/BloodThinnerTracker.Web/wwwroot/icons/icon-512.png
src/BloodThinnerTracker.Web/Components/App.razor - main HTML document with manifest link in head
src/BloodThinnerTracker.Web/Components/Layout/MainLayout.razor - layout component with service worker registration script
```

**Structure Decision**: Keep existing modern Blazor project layout; add static assets under `wwwroot` and minimal registration in `MainLayout.razor` (layout component) with manifest reference in `App.razor` (main HTML document).

## Complexity Tracking

No complex architectural changes. This is a small, low-risk change limited to static assets, a tiny service worker, and documentation/tests.

## Phase 0: Research (deliverable: research.md)

Research goal: confirm manifest shape, icon requirements, service worker registration approach for Blazor Server, and safe caching strategy that excludes user/medical data.

## Phase 1: Design & Contracts

- Data model: none required (no new persisted entities) — `data-model.md` will indicate N/A
- API Contracts: none required — create `/contracts/README.md` noting no API changes
- Quickstart: installation steps and code snippets to add manifest, icons, service worker and registration in `MainLayout.razor` (deliverable: `quickstart.md`)

