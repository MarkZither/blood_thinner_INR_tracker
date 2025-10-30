# Feature Specification: Local Development Orchestration with .NET Aspire

**Feature Branch**: `004-as-a-developer`  
**Created**: October 30, 2025  
**Status**: Draft  

**Input**: User description: "as a developer i should be able to press f5 and the solution will run locally, running the services needed from the solution and spinning up any containers needed and taking care of connectionstrings and service discovery, i should also see a dashboard showing the state of the services and logging and telemetry"

---

## User Scenarios & Testing

### User Story 1 - One-Click Local Development Environment (Priority: P1)

As a developer, I want to press F5 in Visual Studio/VS Code and have the entire solution automatically start with all required services and dependencies, so I can immediately begin developing and debugging without manual setup steps.

**Why this priority**: This is the core developer experience requirement - without this working, developers cannot effectively work on the application. It provides immediate value by eliminating manual setup time and reducing onboarding friction.

**Independent Test**: Can be fully tested by opening the solution in an IDE, pressing F5, and verifying that all services start successfully and the application is accessible at expected URLs. Delivers complete local development environment.

**Acceptance Scenarios**:

1. **Given** a developer has cloned the repository, **When** they press F5 in Visual Studio or run `dotnet run` on the AppHost project, **Then** all services (API, Web) start successfully and are accessible

2. **Given** the solution requires Docker containers (PostgreSQL database), **When** the developer starts the application, **Then** all required containers are automatically pulled, created, and started without manual intervention

3. **Given** services need to communicate with each other, **When** the application starts, **Then** service discovery is configured automatically and services can resolve each other's addresses

4. **Given** services need database connections, **When** the application starts, **Then** connection strings are automatically generated and injected into the appropriate services

---

### User Story 2 - Real-Time Observability Dashboard (Priority: P1)

As a developer, I want to see a dashboard that shows the real-time state of all running services, their logs, metrics, and traces, so I can quickly diagnose issues and understand system behavior during development.

**Why this priority**: Observability is critical for effective debugging and development. Without visibility into service state and logs, developers waste significant time troubleshooting issues. This delivers immediate diagnostic capabilities.

**Independent Test**: Can be fully tested by starting the application and accessing the Aspire dashboard at http://localhost:15000. Delivers complete visibility into running services.

**Acceptance Scenarios**:

1. **Given** the application is running, **When** the developer accesses the Aspire dashboard, **Then** they see all services listed with their current status (running, stopped, error)

2. **Given** services are generating logs, **When** the developer views the dashboard, **Then** they can see real-time log streams for each service with filtering capabilities

3. **Given** services are instrumented with OpenTelemetry, **When** the developer views the dashboard, **Then** they can see distributed traces showing request flows across services

4. **Given** services are emitting metrics, **When** the developer views the dashboard, **Then** they can see real-time metrics (CPU, memory, request rates) for each service

---

### User Story 3 - Container Lifecycle Management (Priority: P2)

As a developer, I want Docker containers (databases, caching services) to be automatically managed by the development orchestration, so I don't have to manually start, stop, or configure containers.

**Why this priority**: Container management is essential for local development but is a lower priority than the core F5 experience (P1). It's a supporting capability that enhances the primary workflow.

**Independent Test**: Can be fully tested by verifying that required containers (PostgreSQL) are automatically started when the application runs. Delivers hands-free container management.

**Acceptance Scenarios**:

1. **Given** the application requires a PostgreSQL database, **When** the developer starts the application, **Then** a PostgreSQL container is automatically started with appropriate configuration

2. **Given** containers are running, **When** the developer stops the application, **Then** containers persist for next run (configurable behavior)

3. **Given** a container fails to start, **When** the developer views the dashboard, **Then** they see clear error messages explaining why the container failed

4. **Given** the developer needs to reset local data, **When** they run the reset script, **Then** containers are recreated with fresh state

---

### User Story 4 - Service Configuration and Discovery (Priority: P2)

As a developer, I want services to automatically discover each other's endpoints and have their configuration injected at runtime, so I don't have to manually maintain URLs, ports, and connection strings in configuration files.

**Why this priority**: Service discovery is important for multi-service development but builds on the core F5 experience (P1). It reduces configuration burden but is secondary to getting services running.

**Independent Test**: Can be fully tested by verifying that services can successfully call each other using service names (not hardcoded URLs) and that configuration values are correctly injected.

**Acceptance Scenarios**:

1. **Given** the Web project needs to call the API, **When** the application starts, **Then** the Web project automatically receives the correct API endpoint URL via configuration

2. **Given** services need environment-specific configuration (dev, staging), **When** the developer changes the environment, **Then** appropriate configuration is automatically applied

3. **Given** a service endpoint changes (different port), **When** the developer restarts the application, **Then** all dependent services automatically receive the updated endpoint

---

### User Story 5 - Integrated Debugging Experience (Priority: P3)

As a developer, I want to set breakpoints across multiple services and debug them simultaneously in my IDE, so I can trace issues that span service boundaries without switching tools.

**Why this priority**: Multi-service debugging is valuable but less critical than basic observability (P1-P2). Developers can use logs/traces initially and add debugging as needed.

**Independent Test**: Can be fully tested by setting breakpoints in multiple projects, making a request that flows across services, and verifying that debugger stops at each breakpoint in sequence.

**Acceptance Scenarios**:

1. **Given** the developer has set breakpoints in both Web and API projects, **When** they make a request from the browser, **Then** the debugger stops at breakpoints in both projects in the correct sequence

2. **Given** the developer is debugging one service, **When** they want to inspect another service's state, **Then** they can view logs and traces from the Aspire dashboard without stopping the debugger

3. **Given** the developer modifies code while debugging, **When** they use hot reload, **Then** changes are applied without restarting all services

4. **Given** an exception occurs in a service, **When** the developer views the dashboard, **Then** they can see the exception details

---

## Requirements

### Functional Requirements

- **FR-001**: System MUST automatically start all services defined in the AppHost project when F5 is pressed or `dotnet run` is executed
- **FR-002**: System MUST automatically pull and start required Docker containers (PostgreSQL initially; Redis deferred to future iterations) before starting dependent services
- **FR-003**: System MUST provide automatic service discovery so services can reference each other by logical name rather than hardcoded URLs
- **FR-004**: System MUST automatically generate and inject connection strings for databases into service configuration
- **FR-005**: System MUST provide a web-based dashboard accessible at http://localhost:15000 showing service status
- **FR-006**: Dashboard MUST display real-time logs from all running services with filtering and search capabilities
- **FR-007**: Dashboard MUST display distributed traces showing request flows across service boundaries using OpenTelemetry standards
- **FR-008**: Dashboard MUST display metrics (CPU, memory, request rates, error rates) for each service in real-time
- **FR-009**: System MUST support debugging multiple services simultaneously with breakpoints across service boundaries
- **FR-010**: System MUST handle graceful shutdown of all services and containers when the developer stops the application
- **FR-011**: System MUST detect and report port conflicts with clear error messages
- **FR-012**: System MUST support environment-specific configuration (Development, Staging) with automatic environment variable injection
- **FR-013**: System MUST provide health check endpoints for all services and display health status in the dashboard
- **FR-014**: System MUST support hot reload for code changes without requiring full application restart
- **FR-015**: System MUST log container startup failures with actionable error messages in the dashboard
- **FR-016**: System MUST support both Windows (PowerShell) and macOS/Linux (bash) development environments
- **FR-017**: System MUST persist container data between runs (databases retain data unless explicitly reset)
- **FR-018**: System MUST provide a mechanism to reset/clean local data (reset-database.ps1 script)
- **FR-019**: System MUST start all services and containers in a single "full stack" profile
- **FR-020**: Dashboard MUST provide direct links to service endpoints and API documentation (Swagger UI)

### Non-Functional Requirements

- **NFR-001**: Application startup time MUST be under 30 seconds on subsequent runs with warm containers
- **NFR-002**: Dashboard response time MUST be under 1 second for log queries
- **NFR-003**: Hot reload MUST apply code changes within 2 seconds
- **NFR-004**: System MUST work with .NET 10 RC2 and Aspire 10.0.0-rc.2
- **NFR-005**: All services MUST use OpenTelemetry standard for observability

---

## Success Criteria

- **SC-001**: Developer can clone the repository and have a fully functional local development environment running within 5 minutes of pressing F5 (excluding initial Docker image downloads)
- **SC-002**: All services start successfully with zero manual configuration or setup steps required beyond cloning the repository
- **SC-003**: Dashboard is accessible immediately after services start and displays accurate real-time status for all services
- **SC-004**: Service discovery resolves endpoint addresses correctly 100% of the time during local development
- **SC-005**: Container failures are detected and reported in the dashboard within 3 seconds with actionable error messages

---

## Assumptions

1. Developers are using Visual Studio 2025, Visual Studio Code with C# Dev Kit, or Rider on Windows, macOS, or Linux
2. Docker Desktop (Windows/macOS) or Docker Engine (Linux) version 20.10+ is installed and running
3. .NET 10 SDK with .NET Aspire workload installed
4. Developer machine has sufficient resources (8GB+ RAM, 20GB+ disk space, 4+ CPU cores)
5. PostgreSQL is the primary database for this feature
6. OpenTelemetry is the standard for logs, metrics, and traces

---

## Non-Goals

- Production deployment orchestration (use Kubernetes, Azure Container Apps, or AWS ECS for production)
- Multi-developer environment synchronization (each developer has isolated local environment)
- Automatic schema migrations (developers run migrations explicitly via CLI tool)
- Performance profiling tools (use dedicated profilers like dotMemory or PerfView)
- Load testing (use separate tools like k6 or JMeter)
- CI/CD pipeline configuration (handled separately in GitHub Actions)
- Redis container support (deferred to future iterations; this feature focuses on PostgreSQL only)

---

## Technical Constraints

1. MUST use .NET Aspire 10.x for orchestration (aligned with .NET 10 requirement)
2. MUST support F5 debugging experience in Visual Studio
3. MUST be cross-platform (Windows, macOS, Linux)
4. MUST use existing project structure (integrates with src/BloodThinnerTracker.AppHost/)
5. MUST use Docker for containerized dependencies
6. MUST use OpenTelemetry standard for observability
7. Dashboard MUST be accessible only on localhost (security constraint)

---

## Dependencies

### External Dependencies
- .NET 10 SDK with .NET Aspire workload (`dotnet workload install aspire`)
- Docker Desktop (Windows/macOS) or Docker Engine (Linux) version 20.10+
- Visual Studio 2025, VS Code with C# Dev Kit, or JetBrains Rider
- Container images: `postgres:16-alpine`

### Internal Dependencies
- Existing BloodThinnerTracker.Api project
- Existing BloodThinnerTracker.Web project
- Existing BloodThinnerTracker.ServiceDefaults project

---

## Edge Cases

- What happens when a required Docker image cannot be pulled (network offline)?
- How does the system handle port conflicts when default ports are already in use?
- What happens when a service fails to start but others continue running?
- What happens when .NET Aspire is not installed or is the wrong version?
- What happens when a container runs out of disk space or memory?

---

## Revision History

| Date       | Version | Changes                                      | Author          |
|------------|---------|----------------------------------------------|-----------------|
| 2025-10-30 | 0.1     | Initial draft                                | GitHub Copilot  |
| 2025-10-30 | 0.2     | Manually cleaned corruption                  | User            |
| 2025-10-30 | 0.3     | Expanded with all 5 user stories and reqs    | GitHub Copilot  |

