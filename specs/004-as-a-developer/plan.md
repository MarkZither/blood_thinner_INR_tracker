# Implementation Plan: Local Development Orchestration with .NET Aspire# Implementation Plan: [FEATURE]



**Branch**: `004-as-a-developer` | **Date**: October 30, 2025 | **Spec**: [spec.md](./spec.md)**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]

**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

## Summary

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

Implement one-press (F5) local development orchestration using .NET Aspire to automatically start all services, manage containers, provide service discovery, and present a unified dashboard for logs, metrics, and distributed traces. The feature eliminates manual setup steps and provides complete observability for the multi-service Blood Thinner Tracker application during local development.

## Summary

**Technical Approach**: Leverage .NET Aspire 10 RC2's built-in orchestration capabilities with Aspire.Hosting for service topology definition, OpenTelemetry for observability, Serilog for structured logging integration with external sinks (InfluxDB), and Polly for resilient HTTP communication between services.

[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context

## Technical Context

**Language/Version**: C# 13 / .NET 10 RC2  

**Primary Dependencies**: <!--

- Aspire.Hosting 10.0.0-rc.2 (orchestration engine)  ACTION REQUIRED: Replace the content in this section with the technical details

- Aspire.Hosting.PostgreSQL (container management)  for the project. The structure here is presented in advisory capacity to guide

- Aspire.ServiceDefaults (shared configuration)  the iteration process.

- OpenTelemetry.Exporter.OpenTelemetryProtocol 1.9.0 (OTLP exporter)-->

- Serilog.AspNetCore 8.0.2 (structured logging)

- Serilog.Sinks.InfluxDB 1.2.0 (external log storage, optional)**Language/Version**: [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION]  

- Microsoft.Extensions.Http.Resilience 8.10.0 (Polly integration)**Primary Dependencies**: [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION]  

**Storage**: [if applicable, e.g., PostgreSQL, CoreData, files or N/A]  

**Storage**: **Testing**: [e.g., pytest, XCTest, cargo test or NEEDS CLARIFICATION]  

- Local: SQLite (in-process, no container)**Target Platform**: [e.g., Linux server, iOS 15+, WASM or NEEDS CLARIFICATION]

- Cloud-like dev: PostgreSQL 16 (Docker container)**Project Type**: [single/web/mobile - determines source structure]  

- Logs: InfluxDB 2.7 (Docker container, optional for advanced log querying)**Performance Goals**: [domain-specific, e.g., 1000 req/s, 10k lines/sec, 60 fps or NEEDS CLARIFICATION]  

**Constraints**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]  

**Testing**: **Scale/Scope**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]

- xUnit 2.9.0 for unit tests

- Aspire.Hosting.Testing for integration tests## Constitution Check

- Testcontainers.NET for container-based tests

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Target Platform**: 

- Windows 10/11 with Docker Desktop[Gates determined based on constitution file]

- macOS with Docker Desktop

- Linux with Docker Engine 20.10+## Project Structure



**Project Type**: Web application (multi-service: API + Web + AppHost orchestrator)### Documentation (this feature)



**Performance Goals**: ```

- Application startup <30 seconds (warm containers)specs/[###-feature]/

- Dashboard response time <1 second for log queries├── plan.md              # This file (/speckit.plan command output)

- Service discovery resolution <100ms├── research.md          # Phase 0 output (/speckit.plan command)

- Hot reload apply changes <2 seconds├── data-model.md        # Phase 1 output (/speckit.plan command)

├── quickstart.md        # Phase 1 output (/speckit.plan command)

**Constraints**: ├── contracts/           # Phase 1 output (/speckit.plan command)

- Must work with existing project structure (no major refactoring)└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)

- Must support F5 debugging experience in Visual Studio 2025```

- Must handle offline scenarios gracefully (cached images)

- Dashboard must be accessible without authentication (local dev only)### Source Code (repository root)

- Must not break existing API/Web functionality<!--

  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout

**Scale/Scope**:   for this feature. Delete unused options and expand the chosen structure with

- 3-4 services initially (API, Web, future Mobile backend)  real paths (e.g., apps/admin, packages/something). The delivered plan must

- Support 1-2 containerized dependencies (PostgreSQL, InfluxDB optional)  not include Option labels.

- Single developer local environment (not multi-developer sync)-->



## Constitution Check```

# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*src/

├── models/

### Principle I: Code Quality & .NET Standards├── services/

✅ **PASS** - Using .NET 10 RC2 with C# 13, following Aspire conventions for orchestration. Aspire provides built-in patterns for DI, configuration, and lifecycle management.├── cli/

└── lib/

### Principle II: Testing Discipline & Coverage

✅ **PASS** - Will use xUnit for unit tests, Aspire.Hosting.Testing for integration tests testing the orchestration itself. Target 90% coverage for AppHost configuration logic and service startup validation.tests/

├── contract/

### Principle III: User Experience Consistency & Pure .NET UI├── integration/

✅ **PASS** - Aspire Dashboard is provided by Microsoft as a web-based UI (no custom UI needed for this feature). Focus is on developer experience (DX) not patient-facing UI.└── unit/



### Principle IV: Performance & Responsiveness# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)

✅ **PASS** - Aspire optimizes startup time through parallel service launches. Dashboard provides real-time updates (<1s). Hot reload supported by default.backend/

├── src/

### Principle V: Security & OWASP Compliance│   ├── models/

✅ **PASS** - Local development only (localhost, no external exposure). Dashboard accessible only from local machine. No patient data exposed (development environment). Production security handled separately.│   ├── services/

│   └── api/

### Principle VI: Cloud Deployment & Container Strategy└── tests/

⚠️ **NOTE** - This feature is for LOCAL DEVELOPMENT ONLY. Production deployment (Azure Container Apps) remains unchanged and follows source-based deployment strategy from Feature 002. No Docker files will be created for this feature.

frontend/

### Principle VII: Configuration Access & Options Pattern├── src/

✅ **PASS** - Aspire uses strongly-typed configuration via `IOptions<T>` pattern throughout. Service discovery endpoints are injected via DI. Connection strings are generated and injected automatically.│   ├── components/

│   ├── pages/

### Principle VIII: Feature Sizing & Scope Management│   └── services/

✅ **PASS** - Feature scoped to 2-3 weeks maximum. Single primary concern: local development orchestration. Clear non-goals defined (no production deployment, no multi-developer sync). Feature can be delivered independently and deployed behind feature flag if needed.└── tests/



**GATE STATUS**: ✅ PASS - All constitution principles satisfied. Proceed to Phase 0 research.# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)

api/

## Project Structure└── [same as backend above]



### Documentation (this feature)ios/ or android/

└── [platform-specific structure: feature modules, UI flows, platform tests]

``````

specs/004-as-a-developer/

├── spec.md              # Feature specification (complete)**Structure Decision**: [Document the selected structure and reference the real

├── plan.md              # This file (Phase 0 output)directories captured above]

├── research.md          # Phase 0 output (to be created)

├── data-model.md        # Phase 1 output (to be created)## Complexity Tracking

├── quickstart.md        # Phase 1 output (to be created)

├── contracts/           # Phase 1 output (to be created)*Fill ONLY if Constitution Check has violations that must be justified*

│   └── aspire-app-model.md  # Aspire application model contract

└── checklists/| Violation | Why Needed | Simpler Alternative Rejected Because |

    └── requirements.md  # Quality checklist (complete, 100%)|-----------|------------|-------------------------------------|

```| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |

| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

### Source Code (repository root)

This feature MODIFIES existing projects and CREATES new AppHost project:

```
src/
├── BloodThinnerTracker.AppHost/         # NEW - Aspire orchestration project
│   ├── Program.cs                       # Aspire app model definition
│   ├── BloodThinnerTracker.AppHost.csproj
│   ├── appsettings.json                 # Optional local overrides
│   └── Properties/
│       └── launchSettings.json          # F5 configuration
│
├── BloodThinnerTracker.ServiceDefaults/ # EXISTING - Add Aspire integration
│   ├── ServiceDefaults.cs               # ADD: Aspire service defaults
│   ├── BloodThinnerTracker.ServiceDefaults.csproj  # MODIFY: Add Aspire packages
│   └── appsettings.json                 # ADD: OpenTelemetry/Serilog config
│
├── BloodThinnerTracker.Api/             # EXISTING - Add Aspire client
│   ├── Program.cs                       # MODIFY: Add Aspire service defaults
│   ├── appsettings.Development.json     # MODIFY: Remove hardcoded URLs
│   └── BloodThinnerTracker.Api.csproj   # MODIFY: Add Aspire packages
│
├── BloodThinnerTracker.Web/             # EXISTING - Add Aspire client
│   ├── Program.cs                       # MODIFY: Add Aspire service defaults
│   ├── appsettings.Development.json     # MODIFY: Use service discovery
│   └── BloodThinnerTracker.Web.csproj   # MODIFY: Add Aspire packages
│
└── BloodThinnerTracker.Shared/          # EXISTING - No changes needed
    └── (unchanged)

tests/
├── BloodThinnerTracker.AppHost.Tests/   # NEW - Test orchestration
│   ├── AppHostTests.cs                  # Verify service startup
│   ├── ServiceDiscoveryTests.cs         # Verify endpoint resolution
│   └── BloodThinnerTracker.AppHost.Tests.csproj
│
└── (existing test projects remain unchanged)

global.json                              # MODIFY: Specify .NET 10 RC2 SDK
Directory.Build.props                    # EXISTING - Already configured for .NET 10
```

**Structure Decision**: Using Option 2 (Web application) with modifications. The existing structure has separate API and Web projects. This feature adds a new AppHost project that orchestrates both, plus modifies ServiceDefaults to provide shared Aspire configuration. The AppHost project becomes the new startup project for local development (F5 launches this instead of individual projects).

## Complexity Tracking

*No constitution violations. This section is left empty per template instructions.*

---

## Phase 0: Research & Technology Validation

**Objective**: Resolve all technical uncertainties and validate technology choices before design phase.

### Research Tasks

1. **Aspire 10 RC2 Compatibility Investigation**
   - **Question**: Does .NET Aspire 10.0.0-rc.2 work with .NET 10 RC2?
   - **Research**: Check official Aspire documentation, NuGet package compatibility, known issues
   - **Output**: Confirm compatible version combinations for global.json

2. **Serilog Integration with Aspire Dashboard**
   - **Question**: How to configure Serilog to work with both Aspire Dashboard AND external sinks (InfluxDB)?
   - **Research**: Aspire logging architecture, Serilog sink configuration, OpenTelemetry integration
   - **Output**: Configuration pattern for dual logging (dashboard + external)

3. **Polly Resilience Patterns for Service-to-Service Calls**
   - **Question**: Which Polly policies are recommended for local development service calls?
   - **Research**: Microsoft.Extensions.Http.Resilience best practices, retry strategies, circuit breaker configuration
   - **Output**: Standard Polly policy configuration for HTTP clients

4. **Service Discovery Endpoint Resolution**
   - **Question**: How does Aspire service discovery resolve service names to URLs?
   - **Research**: Aspire.Hosting service reference patterns, environment variable injection, configuration binding
   - **Output**: Code patterns for calling services by name

5. **Hot Reload Compatibility**
   - **Question**: Does hot reload work across multiple projects in Aspire orchestration?
   - **Research**: .NET hot reload support in Aspire, known limitations, workarounds
   - **Output**: Hot reload configuration and limitations documentation

6. **Container Volume Management**
   - **Question**: How to persist PostgreSQL data between AppHost restarts?
   - **Research**: Aspire.Hosting.PostgreSQL volume configuration, container lifecycle options
   - **Output**: Configuration for persistent vs. ephemeral containers

7. **Port Conflict Handling**
   - **Question**: How does Aspire handle port conflicts when default ports are occupied?
   - **Research**: Aspire dynamic port allocation, port configuration options, error handling
   - **Output**: Port management strategy and error message patterns

8. **Dashboard Authentication (or lack thereof)**
   - **Question**: Does Aspire Dashboard require authentication for local development?
   - **Research**: Dashboard security model, access control options, localhost restrictions
   - **Output**: Security posture for local development dashboard

### Research Output Artifact

All research findings will be consolidated into **`research.md`** with the following structure:

```markdown
# Feature 004: Technology Research

## R-001: Aspire 10 RC2 Compatibility
**Decision**: Use Aspire.Hosting 10.0.0-rc.2 with .NET 10.0.0-rc.2
**Rationale**: [findings from investigation]
**Alternatives Considered**: [other version combinations tested]

## R-002: Serilog Integration
**Decision**: [logging configuration approach]
**Rationale**: [why this approach chosen]
**Alternatives Considered**: [other logging strategies]

[... continue for all 8 research tasks]
```

**Phase 0 Exit Criteria**: 
- All 8 research tasks have documented decisions
- No "NEEDS CLARIFICATION" markers remain
- research.md file created and reviewed
- Technology stack validated and confirmed working

---

## Phase 1: Design & Contracts

**Prerequisites**: research.md complete with all decisions documented

### Artifacts to Generate

#### 1. data-model.md

Since this feature is about orchestration (not domain entities), the "data model" is the **Aspire Application Model** - how services, containers, and dependencies are defined.

**Content**:
- Service topology (API → PostgreSQL, Web → API)
- Container definitions (PostgreSQL, optional InfluxDB)
- Service references and dependencies
- Environment variables and configuration injection
- Health check endpoints
- Resource naming conventions

#### 2. contracts/ directory

**File: contracts/aspire-app-model.md**

Document the Aspire application model as a contract:
- Service registration API (`builder.AddProject<T>()`)
- Container resource API (`builder.AddPostgres()`)
- Service reference API (`.WithReference()`)
- Environment variable contract (what gets injected where)
- Service discovery URL format (`http://{servicename}`)

**File: contracts/service-defaults.md**

Document the ServiceDefaults integration contract:
- What services must call (`builder.AddServiceDefaults()`)
- OpenTelemetry configuration provided
- Serilog configuration provided
- Health check configuration provided
- Resilience policies provided (Polly)

#### 3. quickstart.md

Developer onboarding guide for using the Aspire orchestration:

```markdown
# Quick Start: Local Development with Aspire

## Prerequisites
- .NET 10 RC2 SDK installed
- Docker Desktop running
- Visual Studio 2025 or VS Code with C# Dev Kit

## Steps
1. Clone repository
2. Open solution in Visual Studio
3. Set `BloodThinnerTracker.AppHost` as startup project
4. Press F5

## What Happens
- AppHost starts and reads Program.cs application model
- PostgreSQL container pulls/starts (if not cached)
- API service starts with injected connection string
- Web service starts with injected API endpoint
- Dashboard opens at http://localhost:15000

## Troubleshooting
[common issues and solutions]

## Advanced Configuration
[how to switch between SQLite and PostgreSQL]
[how to enable InfluxDB logging]
[how to customize ports]
```

### Agent Context Update

After generating design artifacts, run:

```powershell
.\.specify\scripts\powershell\update-agent-context.ps1 -AgentType copilot
```

This will update `.github/copilot-instructions.md` with:
- Aspire orchestration patterns
- Serilog configuration examples
- Polly resilience policy patterns
- Service discovery usage patterns

**Phase 1 Exit Criteria**:
- data-model.md created and reviewed
- contracts/ directory created with all contract files
- quickstart.md created and reviewed
- Agent context updated with new patterns
- All artifacts reference concrete code examples (not pseudo-code)

---

## Phase 2: Task Breakdown

**This phase is NOT executed by `/speckit.plan` command.**

Phase 2 is handled by the separate `/speckit.tasks` command which will:
- Generate tasks.md with implementation tasks
- Break down work into <8 hour increments
- Define acceptance criteria for each task
- Assign task IDs (T004-001, T004-002, etc.)

The planning phase ends here. Report completion and generated artifacts to the user.

---

## Notes

- This feature is LOCAL DEVELOPMENT ONLY - no production deployment changes
- Aspire Dashboard is ephemeral (not persisted, starts fresh each time)
- PostgreSQL data CAN be persisted if configured (research task R-006)
- Hot reload supported but has limitations (research task R-005)
- Multi-developer sync is explicitly a non-goal
- Feature can be delivered independently of other features
- Existing API/Web functionality must not be broken
