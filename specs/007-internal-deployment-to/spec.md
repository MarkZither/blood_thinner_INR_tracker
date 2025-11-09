# Feature Specification: Internal deployment to RPi and Windows bare metal

**Feature Branch**: `007-internal-deployment-to`  
**Created**: 2025-11-08  
**Status**: Draft  
**Input**: User description: "Internal deployment to rpi and windows bare metal. It should be possible to quickly deploy the api and web to an internal raspberry pi or windows host for internal usage and limited external access via a secure vpn"

## Clarifications

### Session 2025-11-08

- Q: API authentication posture → A: Option A — Require application-level authentication; Azure/Google auth with JWT for all API calls.

- Q: Transport security for internal deployments → A: Option D (refined) — Use Tailscale (Tailnet + magic DNS) for network isolation and require TLS for traffic addressed via the Tailnet hostname. Certificate issuance for Tailnet-hosted endpoints is expected to be handled via the operator's Tailscale/Tailnet workflow (manual).

- Q: Observability & logging baseline → A: Option A — Health endpoint + local rotating log files (basic). Prometheus-style metrics and structured logging planned for a future iteration.

- Q: Admin access model → A: Option A for Linux (SSH key-based admin access required). For Windows, local operator deployment on the owner's laptop is acceptable and remote admin is not required for now.

- Q: Rollback retention policy → A: Retain one previous published artifact and its logs for rollback (single previous version retained).


## User Scenarios & Testing *(mandatory)*

### User Story 1 - Deploy API to Raspberry Pi (Priority: P1)

An operator (developer or internal IT) needs to deploy the backend API to a Raspberry Pi on the internal network so that local devices can access the API without relying on cloud infrastructure.

**Why this priority**: Provides the core functionality (server-side API) required for local/internal use and testing. Without the API deployed, the system is not usable locally.

**Independent Test**: Produce a host-compatible published artifact for the target device, install it on the RPi, start the service, and confirm the health endpoint returns 200 from a client on the same LAN.

**Acceptance Scenarios**:

1. **Given** a Raspberry Pi on the internal LAN with OS updated and .NET 10 runtime available, **When** the operator runs the provided deployment script, **Then** the API is installed as a systemd service and the health endpoint (GET /health) responds with 200 within 30 seconds of service start.
2. **Given** the API is running, **When** an internal client (web or mobile) makes an authenticated request, **Then** the API responds and logs show successful request handling.

---

### User Story 2 - Deploy Web (Blazor) to Raspberry Pi (Priority: P1)

An operator wants to host the web front-end on the same or another Raspberry Pi to provide a self-hosted web UI reachable from the internal network.

**Why this priority**: Users need the UI to interact with the API locally; hosting the web app completes a fully local deployment.

**Independent Test**: Publish the Web app for linux-arm64, install behind a reverse proxy (optional) or run as a static site, and confirm the UI loads in a browser on the LAN and can successfully call the local API.

**Acceptance Scenarios**:

1. **Given** the web app is published and the webserver/reverse proxy is configured, **When** a browser on the LAN opens the web app URL, **Then** the page loads and the primary dashboard shows live data after successful API calls.

---

### User Story 3 - Deploy to Windows bare metal (Priority: P1)

An operator must be able to deploy both the API and Web to a Windows host (internal server or desktop) for internal usage and testing.

**Why this priority**: Windows is a common internal host; supporting it ensures flexibility for internal IT and developer machines.

**Independent Test**: Produce host-compatible published artifacts for the target Windows host, deploy to the target machine, register the API with the host's service manager, configure firewall rules, and verify endpoints and UI load over the LAN.

**Acceptance Scenarios**:

1. **Given** a Windows host with .NET 10 installed, **When** the operator runs the PowerShell deployment script, **Then** services are registered, start successfully, and the health endpoint returns 200.

---

### User Story 4 - Secure limited external access via VPN (Priority: P2)

Operators must be able to enable limited external access to the internal hosts through a secure VPN so remote users can connect without exposing services directly to the public internet.

**Why this priority**: Security and privacy: avoid public exposure while allowing remote maintenance and limited usage.

**Independent Test**: Join the Tailnet (Tailscale) and verify that a client using the Tailnet hostname (magic DNS) can access the API and Web over TLS while unauthenticated public probing cannot reach the services.

**Acceptance Scenarios**:

1. **Given** a Tailscale client authenticated to the Tailnet, **When** the client connects, **Then** it can reach the API and web services via the Tailnet hostname over TLS, and external probing from the public internet cannot reach the services.

---

### User Story 5 - Quick rollback and update (Priority: P3)

Operators need a simple way to update or rollback deployments on the host to a previous published artifact.

**Why this priority**: Enables safe updates and rapid recovery after problematic releases.

**Independent Test**: Use provided scripts to deploy a new version and then rollback to a prior published artifact; verify health endpoint and primary functionality after rollback.

**Acceptance Scenarios**:

1. **Given** a previous published artifact exists, **When** the operator executes the rollback command, **Then** the previous artifact is restored and services restart successfully.

---

### Edge Cases

- Network partition between RPi and internal clients (LAN outage): document recovery and offline behavior.
- Limited disk space on RPi: deployment script must check available disk and fail gracefully if insufficient.
- Host already running conflicting service on configured ports: deployment should detect and surface clear error messages.
- VPN misconfiguration leading to partial connectivity: provide diagnostics steps.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Provide a documented, repeatable deployment process to publish and install the API and Web on Raspberry Pi (ARM64) hosts.
- **FR-002**: Provide a documented, repeatable deployment process to publish and install the API and Web on Windows (x64) hosts.
- **FR-003**: Deployment scripts MUST validate prerequisites (OS version, required runtime, disk, ports) and fail with actionable messages.
- **FR-004**: API MUST be installable as a background service appropriate to the host platform and start automatically on host boot.
- **FR-005**: Web app MUST be hostable on the same machine and be able to connect to the local API via configurable base URL (env or config file).
- **FR-006**: Provide optional reverse proxy configuration and TLS configuration guidance for internal certificates; include examples for common platforms but keep guidance platform-agnostic.
- **FR-007**: Provide sample VPN configuration and guidance to restrict exposure to internal-only networks; the VPN guidance MUST include key rotation notes and minimal security hardening steps.
- **FR-008**: Provide rollback/update commands to switch between published artifacts with minimal downtime.
- **FR-009**: Deployment must include basic health checks and logging paths and commands to view logs.
 - **FR-009**: Provide a health endpoint and local rotating log files as the observability baseline for internal deployments. Deployment documentation and scripts MUST include explicit health-check commands, log paths, and commands to view logs (e.g., `journalctl` for systemd). Prometheus-style metrics and structured logs are out-of-scope for this iteration and may be added in a future iteration.
 - **FR-010**: Scripts and docs MUST include clear steps for firewall rules and port configuration to allow internal LAN and VPN traffic while preventing public exposure by default.
- **FR-012**: Linux deployments MUST assume admin-capable SSH access (key-based) to targets for installation, service registration, and firewall configuration; document required user account and permissions.
- **FR-013**: Windows deployments MAY be performed locally by the operator (laptop) without requiring remote admin access; documentation should include steps for local install and manual registration where applicable.
- **FR-014**: Deployment artifacts MUST retain the immediately previous published artifact and its logs to enable a single-step rollback; document the storage path and rollback command.

 - **FR-015**: All stored deployment artifacts, persisted logs that contain sensitive information, and any local data introduced by this feature MUST be protected at rest. Protection SHALL be implemented using AES-256 encryption for files or platform-equivalent full-disk encryption (e.g., LUKS on Linux, BitLocker on Windows). Deployment documentation MUST include instructions and verification steps to confirm encryption is enabled on target hosts.

 - **FR-016**: Configuration keys introduced by this feature (for example, deployment paths, ports, TLS certificate locations, and service base URLs) MUST be bound to strongly-typed options classes and consumed via IOptions<T> (or equivalent) through dependency injection. Code MUST avoid magic-string configuration access; tasks will include a mapping document (`specs/007-internal-deployment-to/config-alignment.md`) and unit-test scaffolds that validate option binding.

**Assumptions** (documented defaults used when unspecified):

- Default ports: API HTTP on 5000 (or configured), API HTTPS on 5001; Web served on 80/443 or via reverse proxy. These are configurable.
- RPi target OS: a modern ARM64-capable Linux distribution with a service manager.
- Required runtime: the appropriate runtime for the published artifact will be installed by the operator as a prerequisite.
- VPN technology: recommend a modern, secure VPN solution; the guide will include a recommended example but remain vendor-agnostic.

### Key Entities *(include if feature involves data)*

- **Host**: A physical or virtual machine where services are deployed (attributes: OS, arch, IP, hostname, role).
- **Artifact**: Published build outputs for API or Web (attributes: runtime identifier, version/timestamp, path).
- **DeploymentConfig**: Minimal configuration describing service ports, base URLs, TLS cert locations, and rollback targets.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An operator can deploy the API and Web to a Raspberry Pi from the repository root in under 15 minutes (first-time with prerequisites installed).
- **SC-002**: An operator can deploy the API and Web to a Windows host from the repository root in under 15 minutes (first-time with prerequisites installed).
- **SC-003**: Health endpoints for API return success within 30 seconds of service start in at least 95% of deployments executed according to this guide.
- **SC-004**: VPN-based remote access allows authorized clients to reach services while blocking unauthenticated external requests; verification steps demonstrate blocking from public IP.
- **SC-005**: Rollback to a previous artifact succeeds and restores full functionality within 5 minutes in at least 90% of test runs.

## Notes and Next Steps

- Provide example scripts: `tools/deploy-to-pi.sh` (PowerShell that runs on operator host), `tools/deploy-to-pi.ps1`, `tools/deploy-windows-baremetal.ps1`.
- Provide Tailnet/Tailscale quickstart and a short diagnostic checklist (Tailnet hostname + TLS guidance).
- Provide optional Docker multi-arch guidance if operators prefer containerized deployment.
 - **Transport posture (REQUIRED)**: All network endpoints exposed by this feature MUST be configured to use HTTPS and support TLS 1.3. VPN-only transport is NOT a substitute for endpoint TLS. Operators are responsible for obtaining and installing valid TLS certificates (for example via `tailscale cert <hostname>` when using Tailscale) and validating TLS negotiation (TLS 1.3) during installation. Documentation and tasks will include explicit steps to acquire, install, and verify TLS certificates and cipher negotiation.

### Testing approach (aligned with Constitution)

Testing for this feature must align with the project constitution (see `.specify/memory/constitution.md`) which mandates a combination of unit, integration, and end-to-end testing with a minimum ~90% coverage for functional code.

- Unit tests for PowerShell deployment scripts: use Pester to exercise script logic and error handling locally. External interactions (SSH, SCP, remote service registration, network calls) must be mocked in unit tests so they run without remote hosts.
- Integration tests: design lightweight integration tests that exercise end-to-end deployment flows against a controlled test environment (VM or container with SSH access). These tests validate copying artifacts, service registration, and health checks on a real host. Integration tests should be automated where practical and documented in the plan/tasks.
- End-to-end (E2E) tests for UI flows: where the Web/UI is affected, use Playwright for E2E browser automation; use BUnit for Blazor component tests and xUnit for .NET backend unit/integration tests as applicable.

Objective: reach ~90% coverage for the code changed/introduced by this feature across unit + integration tests. If any part of this requirement cannot be satisfied in the current iteration, a documented waiver signed by product/security leads is required and must be added to the feature folder (`waiver-testing-exception.md`).
