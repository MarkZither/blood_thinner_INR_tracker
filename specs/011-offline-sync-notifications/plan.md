# Implementation Plan: Offline Sync & Notifications

**Branch**: `011-offline-sync-notifications` | **Date**: 2025-12-05 | **Spec**: `spec.md`
**Input**: `specs/011-offline-sync-notifications/spec.md`

## Summary

Add read-only offline sync for medication, INR tests, and medication logs with platform-specific background scheduling and basic local reminders. The existing architecture already synchronizes INRTests and provides platform background jobs; this work extends those pieces to align medication and INR data, provide a combined sync strategy (foreground refresh + periodic background), and schedule platform notifications triggered by background jobs.

## Technical Context

**Language/Version**: .NET 10 / C# 13  
**Primary Dependencies**: .NET MAUI, Entity Framework Core, SQLite (local), Npgsql/Postgres (server), Microsoft.Extensions.Hosting, Playwright (e2e)  
**Storage**: Local SQLite for mobile; Postgres for cloud canonical data  
**Testing**: xUnit (unit/integration), Playwright (e2e), AppHost integration fixtures  
**Target Platform**: Android (primary), Windows (secondary MAUI), Web/Blazor unaffected by this phase  
**Project Type**: Mobile client (`src/BloodThinnerTracker.Mobile`) + API (`src/BloodThinnerTracker.Api`)  
**Performance Goals**: Background sync default interval 1hr (15min while charging+Wi‑Fi). Foreground refresh completes within 60s for datasets <=100 items. Notification delivery target within 1 minute of scheduled time on device.  
**Constraints**: Read-only client for this phase; no client-side create/update/delete. Follow Constitution: options pattern for config, testing coverage, OWASP security rules.  
**Scale/Scope**: Feature scoped for single 2-week iteration; changes limited to mobile client sync worker, background job scheduling, notification scheduling, and small API contract verification (read-only endpoints only).

## Constitution Check

GATE: Must pass before Phase 0 research; re-check after Phase 1 design

- Configuration: Uses options pattern for sync settings (intervals, endpoints) — OK  
- Testing: Add unit/integration tests and a Playwright scenario for sync visibility — OK (tests required before merge)  
- Security: Read-only operation for this phase; ensure transport TLS and secure storage for local health data — OK  
- Performance: Foreground refresh and background jobs must respect device resource limits — OK (monitoring required)

No constitution violations identified that block Phase 0 research. Justifications and tests will be added to tasks.

## Project Structure (selected)

This feature lives inside existing projects: `src/BloodThinnerTracker.Mobile` (extend sync worker and background job registration) and `src/BloodThinnerTracker.Api` (verify read-only endpoints exist). No new top-level projects required.

## Phase 0 — Research (deliverable: `research.md`)

Goals:
- Verify read-only API contract for medications, medication logs, and INR tests.  
- Confirm current background job implementations persist across reboots and can schedule notifications.  
- Establish retry/backoff policy and health-check endpoints used by worker.  

Deliverable: `research.md` (created next)

## Phase 1 — Design & Contracts (deliverables: `data-model.md`, `contracts/openapi.yaml`, `quickstart.md`)

Goals:
- Define the data model for mobile local storage (SQLite) for read-only sync.  
- Produce minimal OpenAPI contract snippets for read endpoints the mobile app will call.  
- Create quickstart instructions for running API + mobile locally and exercising sync flows.

## Phase 2 — Tasks & Implementation (deliverable: `tasks.md`)

High-level tasks will include: extend background sync worker, wire foreground refresh on startup, schedule platform notifications from background job, unit/integration tests, and e2e Playwright test to validate sync + notification flow.

## Acceptance Criteria (from spec)

- Read-only sync delivers canonical medication and INR data to mobile within defined intervals.  
- Foreground refresh aligns local data if background job hasn't synced recently.  
- Basic local notifications trigger at scheduled times when device is offline.

## Artifacts created by this plan

- `research.md` — Phase 0 research and decisions  
- `data-model.md` — Entity definitions and validation rules  
- `contracts/openapi.yaml` — Minimal read-only API contract  
- `quickstart.md` — How to run and validate locally  
- `tasks.md` — Prioritized implementation tasks and estimates

---

Proceeding to Phase 0: create `research.md` and resolve any remaining clarifications.
