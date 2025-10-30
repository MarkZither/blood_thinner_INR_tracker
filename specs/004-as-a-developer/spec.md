# Feature Specification: Local Development Orchestration with .NET Aspire# Feature Specification: Local Development Orchestration with .NET Aspire# Feature Specification: Local Development Orchestration with .NET Aspire# Feature Specification: Local Development Orchestration with .NET Aspire# Feature Specification: Local Development Orchestration with .NET Aspire



**Feature Branch**: `004-as-a-developer`  

**Created**: October 30, 2025  

**Status**: Draft  **Feature Branch**: `004-as-a-developer`  



**Input**: User description: "as a developer i should be able to press f5 and the solution will run locally, running the services needed from the solution and spinning up any containers needed and taking care of connectionstrings and service discovery, i should also see a dashboard showing the state of the services and logging and telemetry"**Created**: October 30, 2025  



---**Status**: Draft  **Feature Branch**: `004-as-a-developer`  



## User Scenarios & Testing**Input**: User description: "as a developer i should be able to press f5 and the solution will run locally, running the services needed from the solution and spinning up any containers needed and taking care of connectionstrings and service discovery, i should also see a dashboard showing the state of the services and logging and telemetry"



### User Story 1 - One-Click Local Development Environment (Priority: P1)**Created**: October 30, 2025  



As a developer, I want to press F5 in Visual Studio/VS Code and have the entire solution automatically start with all required services and dependencies, so I can immediately begin developing and debugging without manual setup steps.## User Scenarios & Testing



**Why this priority**: This is the core developer experience requirement - without this working, developers cannot effectively work on the application. It provides immediate value by eliminating manual setup time and reducing onboarding friction.**Status**: Draft  

**Feature Branch**: `004-as-a-developer`

**Independent Test**: Can be fully tested by opening the solution in an IDE, pressing F5, and verifying that all services start successfully and the application is accessible at expected URLs. Delivers complete local development environment.

### User Story 1 - One-Click Local Development Environment (Priority: P1)

**Acceptance Scenarios**:

**Input**: User description: "as a developer i should be able to press f5 and the solution will run locally, running the services needed from the solution and spinning up any containers needed and taking care of connectionstrings and service discovery, i should also see a dashboard showing the state of the services and logging and telemetry"

1. **Given** a developer has cloned the repository, **When** they press F5 in Visual Studio or run `dotnet run` on the AppHost project, **Then** all services (API, Web) start successfully and are accessible

As a developer, I want to press F5 in Visual Studio/VS Code and have the entire solution automatically start with all required services and dependencies, so I can immediately begin developing and debugging without manual setup steps.

2. **Given** the solution requires Docker containers (PostgreSQL database), **When** the developer starts the application, **Then** all required containers are automatically pulled, created, and started without manual intervention

**Created**: 2025-01-30  **Created**: October 30, 2025  

3. **Given** services need to communicate with each other, **When** the application starts, **Then** service discovery is configured automatically and services can resolve each other's addresses

**Why this priority**: This is the core developer experience requirement - without this working, developers cannot effectively work on the application. It provides immediate value by eliminating manual setup time and reducing onboarding friction.

4. **Given** services need database connections, **When** the application starts, **Then** connection strings are automatically generated and injected into the appropriate services

## User Scenarios & Testing

---

**Independent Test**: Can be fully tested by opening the solution in an IDE, pressing F5, and verifying that all services start successfully and the application is accessible at expected URLs. Delivers complete local development environment.

### User Story 2 - Real-Time Observability Dashboard (Priority: P1)

**Status**: Draft  **Status**: Draft  

As a developer, I want to see a dashboard that shows the real-time state of all running services, their logs, metrics, and traces, so I can quickly diagnose issues and understand system behavior during development.

**Acceptance Scenarios**:

**Why this priority**: Observability is critical for effective debugging and development. Without visibility into service state and logs, developers waste significant time troubleshooting issues. This delivers immediate diagnostic capabilities.

### User Story 1 - One-Click Local Development Environment (Priority: P1)

**Independent Test**: Can be fully tested by starting the application and accessing the Aspire dashboard at the expected URL (typically http://localhost:15000). Delivers complete visibility into running services.

1. **Given** a developer has cloned the repository, **When** they press F5 in Visual Studio or run `dotnet run` on the AppHost project, **Then** all services (API, Web, Mobile backend services) start successfully and are accessible

**Acceptance Scenarios**:

2. **Given** the solution requires Docker containers (databases, caching), **When** the developer starts the application, **Then** all required containers are automatically pulled, created, and started without manual intervention**Input**: User description: "as a developer i should be able to press f5 and the solution will run locally, running the services needed from the solution and spinning up any containers needed and taking care of connectionstrings and service discovery, i should also see a dashboard showing the state of the services and logging and telemetry"**Input**: User description: "as a developer i should be able to press f5 and the solution will run locally, running the services needed from the solution and spinning up any containers needed and taking care of connectionstrings and service discovery, i should also see a dashboard showing the state of the services and logging and telemetry"

1. **Given** the application is running, **When** the developer accesses the Aspire dashboard, **Then** they see all services listed with their current status (running, stopped, error)

3. **Given** services need to communicate with each other, **When** the application starts, **Then** service discovery is configured automatically and services can resolve each other's addresses

2. **Given** services are generating logs, **When** the developer views the dashboard, **Then** they can see real-time log streams for each service with filtering capabilities

4. **Given** services need database connections, **When** the application starts, **Then** connection strings are automatically generated and injected into the appropriate servicesAs a developer, I want to press F5 in Visual Studio/VS Code and have the entire solution automatically start with all required services and dependencies, so I can immediately begin developing and debugging without manual setup steps.

3. **Given** services are instrumented with OpenTelemetry, **When** the developer views the dashboard, **Then** they can see distributed traces showing request flows across services



4. **Given** services are emitting metrics, **When** the developer views the dashboard, **Then** they can see real-time metrics (CPU, memory, request rates) for each service

---

---



### User Story 3 - Container Lifecycle Management (Priority: P2)

### User Story 2 - Real-Time Observability Dashboard (Priority: P1)**Why this priority**: This is the core developer experience requirement - without this working, developers cannot effectively work on the application. It provides immediate value by eliminating manual setup time and reducing onboarding friction.

As a developer, I want Docker containers (databases, caching services, message queues) to be automatically managed by the development orchestration, so I don't have to manually start, stop, or configure containers.



**Why this priority**: Container management is essential for local development but is a lower priority than the core F5 experience (P1). It's a supporting capability that enhances the primary workflow.

As a developer, I want to see a dashboard that shows the real-time state of all running services, their logs, metrics, and traces, so I can quickly diagnose issues and understand system behavior during development.## User Scenarios & Testing## User Scenarios & Testing *(mandatory)*

**Independent Test**: Can be fully tested by verifying that required containers (PostgreSQL) are automatically started when the application runs and stopped when it terminates. Delivers hands-free container management.



**Acceptance Scenarios**:

**Why this priority**: Observability is critical for effective debugging and development. Without visibility into service state and logs, developers waste significant time troubleshooting issues. This delivers immediate diagnostic capabilities.**Independent Test**: Can be fully tested by opening the solution in an IDE, pressing F5, and verifying that all services start successfully and the application is accessible at expected URLs. Delivers complete local development environment.

1. **Given** the application requires a PostgreSQL database, **When** the developer starts the application, **Then** a PostgreSQL container is automatically started with appropriate configuration



2. **Given** containers are running, **When** the developer stops the application, **Then** containers are either stopped or left running based on configuration

**Independent Test**: Can be fully tested by starting the application and accessing the Aspire dashboard at the expected URL (typically http://localhost:15000 or similar). Delivers complete visibility into running services.

3. **Given** a container fails to start, **When** the developer views the dashboard, **Then** they see clear error messages explaining why the container failed



4. **Given** the developer needs to reset local data, **When** they trigger a "clean start" command, **Then** containers are recreated with fresh state

**Acceptance Scenarios**:**Acceptance Scenarios**:

---



### User Story 4 - Service Configuration and Discovery (Priority: P2)

1. **Given** the application is running, **When** the developer accesses the Aspire dashboard, **Then** they see all services listed with their current status (running, stopped, error)### User Story 1 - One-Click Local Development Environment (Priority: P1)### User Story 1 - One-Click Local Development Startup (Priority: P1)

As a developer, I want services to automatically discover each other's endpoints and have their configuration injected at runtime, so I don't have to manually maintain URLs, ports, and connection strings in configuration files.

2. **Given** services are generating logs, **When** the developer views the dashboard, **Then** they can see real-time log streams for each service with filtering capabilities

**Why this priority**: Service discovery is important for multi-service development but builds on the core F5 experience (P1). It reduces configuration burden but is secondary to getting services running.

3. **Given** services are instrumented with OpenTelemetry, **When** the developer views the dashboard, **Then** they can see distributed traces showing request flows across services1. **Given** a developer has cloned the repository, **When** they press F5 in Visual Studio or run `dotnet run` on the AppHost project, **Then** all services (API, Web, Mobile backend services) start successfully and are accessible

**Independent Test**: Can be fully tested by verifying that services can successfully call each other using service names (not hardcoded URLs) and that configuration values are correctly injected. Delivers configuration-free inter-service communication.

4. **Given** services are emitting metrics, **When** the developer views the dashboard, **Then** they can see real-time metrics (CPU, memory, request rates) for each service

**Acceptance Scenarios**:

2. **Given** the solution requires Docker containers (databases, caching), **When** the developer starts the application, **Then** all required containers are automatically pulled, created, and started without manual intervention

1. **Given** the Web project needs to call the API, **When** the application starts, **Then** the Web project automatically receives the correct API endpoint URL via configuration

---

2. **Given** multiple instances of a service might be running, **When** a client service calls it, **Then** the request is routed to an available instance

3. **Given** services need to communicate with each other, **When** the application starts, **Then** service discovery is configured automatically and services can resolve each other's addresses

3. **Given** services need environment-specific configuration (dev, staging), **When** the developer changes the environment, **Then** appropriate configuration is automatically applied

### User Story 3 - Container Lifecycle Management (Priority: P2)

4. **Given** a service endpoint changes (different port), **When** the developer restarts the application, **Then** all dependent services automatically receive the updated endpoint

4. **Given** services need database connections, **When** the application starts, **Then** connection strings are automatically generated and injected into the appropriate servicesAs a developer, I want to press F5 in Visual Studio/VS Code and have the entire solution automatically start with all required services and dependencies, so I can immediately begin developing and debugging without manual setup steps.As a developer, I want to press F5 in Visual Studio and have all necessary services start automatically so that I can begin development immediately without manual infrastructure setup.

---

As a developer, I want Docker containers (databases, caching services, message queues) to be automatically managed by the development orchestration, so I don't have to manually start, stop, or configure containers.

### User Story 5 - Integrated Debugging Experience (Priority: P3)



As a developer, I want to set breakpoints across multiple services and debug them simultaneously in my IDE, so I can trace issues that span service boundaries without switching tools.

**Why this priority**: Container management is essential for local development but is a lower priority than the core F5 experience (P1). It's a supporting capability that enhances the primary workflow.

**Why this priority**: Multi-service debugging is valuable but less critical than basic observability (P1-P2). Developers can use logs/traces initially and add debugging as needed.

---

**Independent Test**: Can be fully tested by setting breakpoints in multiple projects, making a request that flows across services, and verifying that debugger stops at each breakpoint in sequence. Delivers full-stack debugging capability.

**Independent Test**: Can be fully tested by verifying that required containers (SQLite/PostgreSQL, Redis if needed) are automatically started when the application runs and stopped when it terminates. Delivers hands-free container management.

**Acceptance Scenarios**:



1. **Given** the developer has set breakpoints in both Web and API projects, **When** they make a request from the browser, **Then** the debugger stops at breakpoints in both projects in the correct sequence

**Acceptance Scenarios**:

2. **Given** the developer is debugging one service, **When** they want to inspect another service's state, **Then** they can view logs and traces from the Aspire dashboard without stopping the debugger

### User Story 2 - Real-Time Observability Dashboard (Priority: P1)**Why this priority**: This is the core developer experience requirement - without this working, developers cannot effectively work on the application. It provides immediate value by eliminating manual setup time and reducing onboarding friction.**Why this priority**: This is the core value proposition - eliminating the friction of local development setup. Without this, the feature provides no value.

3. **Given** the developer modifies code while debugging, **When** they use hot reload, **Then** changes are applied without restarting all services

1. **Given** the application requires a PostgreSQL database, **When** the developer starts the application, **Then** a PostgreSQL container is automatically started with appropriate configuration

4. **Given** an exception occurs in a background service, **When** the developer views the dashboard, **Then** they can see the exception details and navigate to the source code

2. **Given** containers are running, **When** the developer stops the application, **Then** containers are either stopped or left running based on configuration

---

3. **Given** a container fails to start, **When** the developer views the dashboard, **Then** they see clear error messages explaining why the container failed

### Edge Cases

4. **Given** the developer needs to reset local data, **When** they trigger a "clean start" command, **Then** containers are recreated with fresh stateAs a developer, I want to see a dashboard that shows the real-time state of all running services, their logs, metrics, and traces, so I can quickly diagnose issues and understand system behavior during development.

- What happens when a required Docker image is not available locally and cannot be pulled (network offline)?

- How does the system handle port conflicts when default ports (5000, 15000, etc.) are already in use by other applications?

- What happens when a service fails to start but others continue running?

- How does the developer restart a single service without restarting the entire solution?---

- What happens when .NET Aspire is not installed or is the wrong version?

- How does the system handle configuration conflicts between local appsettings.json and Aspire-injected configuration?

- What happens when a container runs out of disk space or memory?

- How does the developer switch between using real containers vs. in-memory test doubles for dependencies?### User Story 4 - Service Configuration and Discovery (Priority: P2)**Why this priority**: Observability is critical for effective debugging and development. Without visibility into service state and logs, developers waste significant time troubleshooting issues. This delivers immediate diagnostic capabilities.**Independent Test**: Can be fully tested by opening the solution in an IDE, pressing F5, and verifying that all services start successfully and the application is accessible at expected URLs. Delivers complete local development environment.**Independent Test**: Can be fully tested by pressing F5 in Visual Studio and verifying all services start and the application runs. Delivers immediate developer productivity improvement.



---



## RequirementsAs a developer, I want services to automatically discover each other's endpoints and have their configuration injected at runtime, so I don't have to manually maintain URLs, ports, and connection strings in configuration files.



### Functional Requirements



- **FR-001**: System MUST automatically start all services defined in the AppHost project when F5 is pressed or `dotnet run` is executed**Why this priority**: Service discovery is important for multi-service development but builds on the core F5 experience (P1). It reduces configuration burden but is secondary to getting services running.**Independent Test**: Can be fully tested by starting the application and accessing the Aspire dashboard at the expected URL (typically http://localhost:15000 or similar). Delivers complete visibility into running services.



- **FR-002**: System MUST automatically pull and start required Docker containers (PostgreSQL initially; Redis deferred to future iterations) before starting dependent services



- **FR-003**: System MUST provide automatic service discovery so services can reference each other by logical name rather than hardcoded URLs**Independent Test**: Can be fully tested by verifying that services can successfully call each other using service names (not hardcoded URLs) and that configuration values are correctly injected. Delivers configuration-free inter-service communication.



- **FR-004**: System MUST automatically generate and inject connection strings for databases and caching services into service configuration



- **FR-005**: System MUST provide a web-based dashboard accessible at a predictable URL (e.g., http://localhost:15000) showing service status**Acceptance Scenarios**:**Acceptance Scenarios**:**Acceptance Scenarios**:**Acceptance Scenarios**:



- **FR-006**: Dashboard MUST display real-time logs from all running services with filtering and search capabilities



- **FR-007**: Dashboard MUST display distributed traces showing request flows across service boundaries using OpenTelemetry standards1. **Given** the Web project needs to call the API, **When** the application starts, **Then** the Web project automatically receives the correct API endpoint URL via configuration



- **FR-008**: Dashboard MUST display metrics (CPU, memory, request rates, error rates) for each service in real-time2. **Given** multiple instances of a service might be running, **When** a client service calls it, **Then** the request is routed to an available instance



- **FR-009**: System MUST support debugging multiple services simultaneously with breakpoints across service boundaries3. **Given** services need environment-specific configuration (dev, staging), **When** the developer changes the environment, **Then** appropriate configuration is automatically applied1. **Given** the application is running, **When** the developer accesses the Aspire dashboard, **Then** they see all services listed with their current status (running, stopped, error)



- **FR-010**: System MUST handle graceful shutdown of all services and containers when the developer stops the application4. **Given** a service endpoint changes (different port), **When** the developer restarts the application, **Then** all dependent services automatically receive the updated endpoint



- **FR-011**: System MUST detect and report port conflicts with clear error messages suggesting alternative ports2. **Given** services are generating logs, **When** the developer views the dashboard, **Then** they can see real-time log streams for each service with filtering capabilities



- **FR-012**: System MUST support environment-specific configuration (Development, Staging) with automatic environment variable injection---



- **FR-013**: System MUST provide health check endpoints for all services and display health status in the dashboard3. **Given** services are instrumented with OpenTelemetry, **When** the developer views the dashboard, **Then** they can see distributed traces showing request flows across services1. **Given** a developer has cloned the repository, **When** they press F5 in Visual Studio or run `dotnet run` on the AppHost project, **Then** all services (API, Web, Mobile backend services) start successfully and are accessible1. **Given** a developer has the solution open in Visual Studio, **When** they press F5, **Then** the orchestrator project starts and launches all configured services



- **FR-014**: System MUST support hot reload for code changes without requiring full application restart### User Story 5 - Integrated Debugging Experience (Priority: P3)



- **FR-015**: System MUST log container startup failures with actionable error messages in the dashboard4. **Given** services are emitting metrics, **When** the developer views the dashboard, **Then** they can see real-time metrics (CPU, memory, request rates) for each service



- **FR-016**: System MUST support both Windows (PowerShell) and macOS/Linux (bash) development environmentsAs a developer, I want to set breakpoints across multiple services and debug them simultaneously in my IDE, so I can trace issues that span service boundaries without switching tools.



- **FR-017**: System MUST persist container data between runs (databases retain data unless explicitly reset)2. **Given** the solution requires Docker containers (databases, caching), **When** the developer starts the application, **Then** all required containers are automatically pulled, created, and started without manual intervention2. **Given** services are starting, **When** any required containers are not running, **Then** the orchestrator automatically pulls and starts the container images



- **FR-018**: System MUST provide a mechanism to reset/clean local data (recreate containers with fresh state)**Why this priority**: Multi-service debugging is valuable but less critical than basic observability (P1-P2). Developers can use logs/traces initially and add debugging as needed.



- **FR-019**: System MUST start all services and containers in a single "full stack" profile (multiple profiles can be added in future iterations if needed)---



- **FR-020**: Dashboard MUST provide direct links to service endpoints and API documentation (Swagger UI)**Independent Test**: Can be fully tested by setting breakpoints in multiple projects, making a request that flows across services, and verifying that debugger stops at each breakpoint in sequence. Delivers full-stack debugging capability.



### Key Entities3. **Given** services need to communicate with each other, **When** the application starts, **Then** service discovery is configured automatically and services can resolve each other's addresses3. **Given** services are running, **When** a developer navigates to the application URL, **Then** the application loads successfully with all dependencies available



- **AppHost Project**: .NET Aspire orchestration project that defines service topology and dependencies**Acceptance Scenarios**:

- **Service Projects**: Individual ASP.NET Core API, Blazor Web services that are orchestrated

- **Container Resources**: Docker containers for PostgreSQL or other infrastructure dependencies### User Story 3 - Container Lifecycle Management (Priority: P2)

- **Service Discovery Configuration**: Automatically generated endpoint mappings between services

- **Dashboard**: Web-based UI for observing service state, logs, metrics, and traces1. **Given** the developer has set breakpoints in both Web and API projects, **When** they make a request from the browser, **Then** the debugger stops at breakpoints in both projects in the correct sequence

- **OpenTelemetry Instrumentation**: Distributed tracing and metrics collection across services

- **Environment Configuration**: Development, staging settings with automatic injection2. **Given** the developer is debugging one service, **When** they want to inspect another service's state, **Then** they can view logs and traces from the Aspire dashboard without stopping the debugger4. **Given** services need database connections, **When** the application starts, **Then** connection strings are automatically generated and injected into the appropriate services4. **Given** services have been started, **When** the developer stops debugging (Shift+F5), **Then** all services and containers stop gracefully

- **Health Checks**: Service health monitoring endpoints for status tracking

3. **Given** the developer modifies code while debugging, **When** they use hot reload, **Then** changes are applied without restarting all services

---

4. **Given** an exception occurs in a background service, **When** the developer views the dashboard, **Then** they can see the exception details and navigate to the source codeAs a developer, I want Docker containers (databases, caching services, message queues) to be automatically managed by the development orchestration, so I don't have to manually start, stop, or configure containers.

## Success Criteria



### Measurable Outcomes

---

- **SC-001**: Developer can clone the repository and have a fully functional local development environment running within 5 minutes of pressing F5 (excluding initial Docker image downloads)



- **SC-002**: All services start successfully with zero manual configuration or setup steps required beyond cloning the repository

### Edge Cases**Why this priority**: Container management is essential for local development but is a lower priority than the core F5 experience (P1). It's a supporting capability that enhances the primary workflow.

- **SC-003**: Dashboard is accessible immediately after services start and displays accurate real-time status for all services



- **SC-004**: Developer can view logs from all services in the dashboard with response time under 1 second for log queries

- What happens when a required Docker image is not available locally and cannot be pulled (network offline)?------

- **SC-005**: Distributed traces appear in the dashboard within 5 seconds of request completion showing complete request flow

- How does the system handle port conflicts when default ports (5000, 15000, etc.) are already in use by other applications?

- **SC-006**: Service discovery resolves endpoint addresses correctly 100% of the time during local development

- What happens when a service fails to start but others continue running?**Independent Test**: Can be fully tested by verifying that required containers (SQLite/PostgreSQL, Redis if needed) are automatically started when the application runs and stopped when it terminates. Delivers hands-free container management.

- **SC-007**: Container failures are detected and reported in the dashboard within 3 seconds with actionable error messages

- How does the developer restart a single service without restarting the entire solution?

- **SC-008**: Developer can set breakpoints in multiple services and debug cross-service requests without tool switching

- What happens when .NET Aspire is not installed or is the wrong version?

- **SC-009**: Application startup time (from F5 to all services healthy) is under 30 seconds on subsequent runs with warm containers

- How does the system handle configuration conflicts between local appsettings.json and Aspire-injected configuration?

- **SC-010**: Hot reload applies code changes to running services within 2 seconds without requiring full restart

- What happens when a container runs out of disk space or memory?**Acceptance Scenarios**:

---

- How does the developer switch between using real containers vs. in-memory test doubles for dependencies?

## Assumptions

### User Story 2 - Real-Time Observability Dashboard (Priority: P1)### User Story 2 - Automatic Connection String Management (Priority: P1)

1. **Development Environment**: Developers are using Visual Studio 2025, Visual Studio Code with C# Dev Kit, or Rider on Windows, macOS, or Linux

## Requirements

2. **Docker Installation**: Docker Desktop (Windows/macOS) or Docker Engine (Linux) version 20.10+ is installed and running on the developer machine

1. **Given** the application requires a PostgreSQL database, **When** the developer starts the application, **Then** a PostgreSQL container is automatically started with appropriate configuration

3. **.NET Version**: Project targets .NET 10 which includes .NET Aspire 10.x tooling

### Functional Requirements

4. **Network Access**: Developer machine has internet access to pull container images from Docker Hub on first run

2. **Given** containers are running, **When** the developer stops the application, **Then** containers are either stopped or left running based on configuration

5. **Resource Availability**: Developer machine has sufficient resources (8GB+ RAM, 20GB+ disk space, 4+ CPU cores) to run multiple services and containers

- **FR-001**: System MUST automatically start all services defined in the AppHost project when F5 is pressed or `dotnet run` is executed

6. **Solution Structure**: Existing solution follows standard structure with API, Web, and shared projects

- **FR-002**: System MUST automatically pull and start required Docker containers (PostgreSQL, Redis, etc.) before starting dependent services3. **Given** a container fails to start, **When** the developer views the dashboard, **Then** they see clear error messages explaining why the container failed

7. **Database Choice**: PostgreSQL is the primary database for this feature; SQLite used for lightweight scenarios where applicable

- **FR-003**: System MUST provide automatic service discovery so services can reference each other by logical name rather than hardcoded URLs

8. **Service Communication**: Services communicate via HTTP REST APIs

- **FR-004**: System MUST automatically generate and inject connection strings for databases and caching services into service configuration4. **Given** the developer needs to reset local data, **When** they trigger a "clean start" command, **Then** containers are recreated with fresh stateAs a developer, I want to see a dashboard that shows the real-time state of all running services, their logs, metrics, and traces, so I can quickly diagnose issues and understand system behavior during development.As a developer, I want connection strings and service endpoints to be automatically configured so that I don't need to manually edit configuration files or remember port numbers.

9. **Authentication**: Local development uses simplified auth (no real OAuth providers required for this feature)

- **FR-005**: System MUST provide a web-based dashboard accessible at a predictable URL (e.g., http://localhost:15000) showing service status

10. **Port Range**: System can use ports in range 5000-6000 for services and 15000-16000 for infrastructure

- **FR-006**: Dashboard MUST display real-time logs from all running services with filtering and search capabilities

11. **Telemetry Stack**: OpenTelemetry is the standard for logs, metrics, and traces

- **FR-007**: Dashboard MUST display distributed traces showing request flows across service boundaries using OpenTelemetry standards

12. **Dashboard Technology**: .NET Aspire includes a built-in dashboard (no custom dashboard implementation needed)

- **FR-008**: Dashboard MUST display metrics (CPU, memory, request rates, error rates) for each service in real-time---

---

- **FR-009**: System MUST support debugging multiple services simultaneously with breakpoints across service boundaries

## Non-Goals

- **FR-010**: System MUST handle graceful shutdown of all services and containers when the developer stops the application

- **Production Deployment**: This feature is for local development only, not production orchestration (use Kubernetes, Azure Container Apps, or AWS ECS for production)

- **Multi-Developer Synchronization**: Each developer has isolated local environment (no shared dev environments)- **FR-011**: System MUST detect and report port conflicts with clear error messages suggesting alternative ports

- **Automatic Schema Migrations**: Developers should run migrations explicitly via CLI tool

- **Performance Profiling Tools**: Use dedicated profilers like dotMemory or PerfView for deep profiling- **FR-012**: System MUST support environment-specific configuration (Development, Staging) with automatic environment variable injection### User Story 4 - Service Configuration and Discovery (Priority: P2)**Why this priority**: Observability is critical for effective debugging and development. Without visibility into service state and logs, developers waste significant time troubleshooting issues. This delivers immediate diagnostic capabilities.**Why this priority**: Manual connection string management is a major source of configuration errors and time waste. This is essential for the "it just works" experience.

- **Load Testing**: Use separate tools like k6 or JMeter for load/stress testing

- **Security Scanning**: Use dedicated security tools for vulnerability assessment- **FR-013**: System MUST provide health check endpoints for all services and display health status in the dashboard

- **CI/CD Pipeline**: Build and deployment automation (handled separately in GitHub Actions)

- **Secrets Management for Production**: Use Azure Key Vault or similar for production (local dev uses simplified approach)- **FR-014**: System MUST support hot reload for code changes without requiring full application restart

- **Redis Container Support**: Deferred to future iterations; this feature focuses on PostgreSQL only

- **FR-015**: System MUST log container startup failures with actionable error messages in the dashboard

---

- **FR-016**: System MUST support both Windows (PowerShell) and macOS/Linux (bash) development environmentsAs a developer, I want services to automatically discover each other's endpoints and have their configuration injected at runtime, so I don't have to manually maintain URLs, ports, and connection strings in configuration files.

## Technical Constraints

- **FR-017**: System MUST persist container data between runs (databases retain data unless explicitly reset)

1. **Must use .NET Aspire 10.x**: Project requirements specify .NET Aspire as the orchestration framework (aligned with .NET 10 requirement)

- **FR-018**: System MUST provide a mechanism to reset/clean local data (recreate containers with fresh state)

2. **Must support F5 experience**: Primary interaction model is Visual Studio debugging experience

- **FR-019**: System MUST start all services and containers in a single "full stack" profile (multiple profiles can be added in future iterations if needed)

3. **Must be cross-platform**: Solution must work on Windows, macOS, and Linux

- **FR-020**: Dashboard MUST provide direct links to service endpoints and API documentation (Swagger UI)**Why this priority**: Service discovery is important for multi-service development but builds on the core F5 experience (P1). It reduces configuration burden but is secondary to getting services running.**Independent Test**: Can be fully tested by starting the application and accessing the Aspire dashboard at the expected URL (typically http://localhost:15000 or similar). Delivers complete visibility into running services.**Independent Test**: Can be tested by examining service configuration before and after startup - no manual configuration changes should be required. Services should discover each other automatically.

4. **Must use existing project structure**: Cannot require major solution restructuring (integrates with src/BloodThinnerTracker.AppHost/)



5. **Must preserve existing functionality**: All current API and Web features must continue to work

### Key Entities

6. **Must use Docker**: Container runtime must be Docker-compatible (not Podman or other alternatives)



7. **Must be version-controlled**: All orchestration configuration must be in source control (no local-only config)

- **AppHost Project**: .NET Aspire orchestration project that defines service topology and dependencies**Independent Test**: Can be fully tested by verifying that services can successfully call each other using service names (not hardcoded URLs) and that configuration values are correctly injected. Delivers configuration-free inter-service communication.

8. **Must be zero-configuration**: Default settings must work out-of-the-box for 90% of developers

- **Service Projects**: Individual ASP.NET Core API, Blazor Web, MAUI backend services that are orchestrated

9. **Must use OpenTelemetry standard**: For observability (logs, metrics, traces)

- **Container Resources**: Docker containers for PostgreSQL, Redis, or other infrastructure dependencies

10. **Must work with Visual Studio 2025, VS Code with C# Dev Kit, and JetBrains Rider**

- **Service Discovery Configuration**: Automatically generated endpoint mappings between services

11. **Dashboard must be web-based**: Accessible via modern browsers (Chrome, Edge, Firefox, Safari)

- **Dashboard**: Web-based UI for observing service state, logs, metrics, and traces**Acceptance Scenarios**:**Acceptance Scenarios**:**Acceptance Scenarios**:

12. **Service discovery must be built-in**: No external service mesh or infrastructure required

- **OpenTelemetry Instrumentation**: Distributed tracing and metrics collection across services

---

- **Environment Configuration**: Development, staging, production settings with automatic injection

## Dependencies

- **Health Checks**: Service health monitoring endpoints for status tracking

### External Dependencies

1. **Given** the Web project needs to call the API, **When** the application starts, **Then** the Web project automatically receives the correct API endpoint URL via configuration

- .NET 10 SDK with .NET Aspire workload installed (`dotnet workload install aspire`)

- Docker Desktop (Windows/macOS) or Docker Engine (Linux) version 20.10 or later## Success Criteria

- Visual Studio 2025, VS Code with C# Dev Kit extension, or JetBrains Rider

- OpenTelemetry SDK packages for .NET (`OpenTelemetry`, `OpenTelemetry.Instrumentation.AspNetCore`, etc.)2. **Given** multiple instances of a service might be running, **When** a client service calls it, **Then** the request is routed to an available instance

- Aspire.Hosting NuGet packages for AppHost project

- Aspire.ServiceDefaults NuGet packages for service projects### Measurable Outcomes

- Container images: `postgres:16-alpine` (Redis deferred to future iterations)

3. **Given** services need environment-specific configuration (dev, staging), **When** the developer changes the environment, **Then** appropriate configuration is automatically applied1. **Given** the application is running, **When** the developer accesses the Aspire dashboard, **Then** they see all services listed with their current status (running, stopped, error)1. **Given** multiple services require database connections, **When** services start, **Then** each service receives correct connection strings automatically

### Internal Dependencies

- **SC-001**: Developer can clone the repository and have a fully functional local development environment running within 5 minutes of pressing F5 (excluding initial Docker image downloads)

- Existing BloodThinnerTracker.Api project

- Existing BloodThinnerTracker.Web project- **SC-002**: All services start successfully with zero manual configuration or setup steps required beyond cloning the repository4. **Given** a service endpoint changes (different port), **When** the developer restarts the application, **Then** all dependent services automatically receive the updated endpoint

- Existing BloodThinnerTracker.Shared project

- Existing BloodThinnerTracker.ServiceDefaults project- **SC-003**: Dashboard is accessible immediately after services start and displays accurate real-time status for all services

- PostgreSQL database schema and migrations

- **SC-004**: Developer can view logs from all services in the dashboard with response time under 1 second for log queries2. **Given** services are generating logs, **When** the developer views the dashboard, **Then** they can see real-time log streams for each service with filtering capabilities2. **Given** services need to communicate with each other, **When** a service makes a request to another service, **Then** the request routes correctly using service discovery

### Prerequisite Features

- **SC-005**: Distributed traces appear in the dashboard within 5 seconds of request completion showing complete request flow

- None (this is a foundational development infrastructure feature)

- **SC-006**: Service discovery resolves endpoint addresses correctly 100% of the time during local development---

---

- **SC-007**: Container failures are detected and reported in the dashboard within 3 seconds with actionable error messages

## Questions

- **SC-008**: Developer can set breakpoints in multiple services and debug cross-service requests without tool switching3. **Given** services are instrumented with OpenTelemetry, **When** the developer views the dashboard, **Then** they can see distributed traces showing request flows across services3. **Given** connection strings are environment-specific, **When** running locally, **Then** local development connection strings are used without requiring configuration changes

None at this time. All requirements are clear and actionable. The specification follows the prioritized user story approach with independently testable journeys (P1-P3).

- **SC-009**: Application startup time (from F5 to all services healthy) is under 30 seconds on subsequent runs with warm containers

---

- **SC-010**: Hot reload applies code changes to running services within 2 seconds without requiring full restart### User Story 5 - Integrated Debugging Experience (Priority: P3)

## Related Documentation



- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)

- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)## Assumptions4. **Given** services are emitting metrics, **When** the developer views the dashboard, **Then** they can see real-time metrics (CPU, memory, request rates) for each service4. **Given** a new service is added to the solution, **When** the orchestrator restarts, **Then** the new service is automatically registered and discoverable

- [Docker Documentation](https://docs.docker.com/)

- Project README: `README.md`

- Copilot Instructions: `.github/copilot-instructions.md`

- Developers have Docker Desktop (Windows/macOS) or Docker Engine (Linux) installed and runningAs a developer, I want to set breakpoints across multiple services and debug them simultaneously in my IDE, so I can trace issues that span service boundaries without switching tools.

---

- Developers have .NET 10 SDK installed

## Revision History

- Developers are using Visual Studio 2025, VS Code with C# Dev Kit, or Rider

| Date       | Version | Changes                                      | Author          |

|------------|---------|----------------------------------------------|-----------------|- Project will use .NET Aspire 10.x (compatible with .NET 10)

| 2025-10-30 | 0.1     | Initial draft                                | GitHub Copilot  |

| 2025-10-30 | 0.2     | Fixed file corruption, clarified Redis scope | GitHub Copilot  |- SQLite is used for local development databases (lightweight, no container needed)**Why this priority**: Multi-service debugging is valuable but less critical than basic observability (P1-P2). Developers can use logs/traces initially and add debugging as needed.


- PostgreSQL containers are used for cloud-like development scenarios (optional)

- Services will use HTTP (not HTTPS) for local development unless specifically testing TLS------

- Docker images for required services (PostgreSQL, Redis) are available in public registries

- Developers have at least 8GB RAM and 20GB free disk space for containers**Independent Test**: Can be fully tested by setting breakpoints in multiple projects, making a request that flows across services, and verifying that debugger stops at each breakpoint in sequence. Delivers full-stack debugging capability.

- Network connectivity is available for initial Docker image downloads

- Developers understand basic Docker concepts (containers, images, volumes)

- Application uses OpenTelemetry SDK for distributed tracing instrumentation

**Acceptance Scenarios**:

## Non-Goals

### User Story 3 - Container Lifecycle Management (Priority: P2)### User Story 3 - Real-Time Service Health Dashboard (Priority: P2)

- Production deployment orchestration (use Kubernetes, Azure Container Apps, or AWS ECS instead)

- Multi-developer environment synchronization (each developer has isolated local environment)1. **Given** the developer has set breakpoints in both Web and API projects, **When** they make a request from the browser, **Then** the debugger stops at breakpoints in both projects in the correct sequence

- Automatic schema migrations (developers should run migrations explicitly via CLI tool)

- Performance profiling tools (use dedicated profilers like dotMemory or PerfView)2. **Given** the developer is debugging one service, **When** they want to inspect another service's state, **Then** they can view logs and traces from the Aspire dashboard without stopping the debugger

- Load testing or stress testing capabilities (use separate tools like k6 or JMeter)

- Security scanning or vulnerability assessment (use dedicated security tools)3. **Given** the developer modifies code while debugging, **When** they use hot reload, **Then** changes are applied without restarting all services

- CI/CD pipeline configuration (handled separately in GitHub Actions)

- Secrets management for production credentials (use Azure Key Vault or similar for production)4. **Given** an exception occurs in a background service, **When** the developer views the dashboard, **Then** they can see the exception details and navigate to the source codeAs a developer, I want Docker containers (databases, caching services, message queues) to be automatically managed by the development orchestration, so I don't have to manually start, stop, or configure containers.As a developer, I want to see a dashboard showing the state of all running services, their logs, and telemetry so that I can quickly identify and debug issues.



## Technical Constraints



- Must use .NET Aspire 10.x for orchestration (aligned with .NET 10 requirement)---

- Must target .NET 10 (net10.0) for all projects

- Must use OpenTelemetry standard for observability (logs, metrics, traces)

- Must support Windows, macOS, and Linux development environments

- Must use Docker for containerized dependencies (not Podman or other alternatives)### Edge Cases**Why this priority**: Container management is essential for local development but is a lower priority than the core F5 experience (P1). It's a supporting capability that enhances the primary workflow.**Why this priority**: While not strictly required for services to run, observability is critical for effective debugging and understanding system behavior. This significantly improves developer experience.

- Must work with Visual Studio 2025, VS Code with C# Dev Kit, and JetBrains Rider

- Dashboard must be web-based and accessible via modern browsers (Chrome, Edge, Firefox, Safari)

- Service discovery must not require external service mesh or infrastructure (built-in .NET Aspire mechanism)

- Must support both SQLite (in-process) and PostgreSQL (containerized) database scenarios- What happens when a required Docker image is not available locally and cannot be pulled (network offline)?

- Must integrate with existing project structure (src/BloodThinnerTracker.AppHost/)

- How does the system handle port conflicts when default ports (5000, 15000, etc.) are already in use by other applications?

## Dependencies

- What happens when a service fails to start but others continue running?**Independent Test**: Can be fully tested by verifying that required containers (SQLite/PostgreSQL, Redis if needed) are automatically started when the application runs and stopped when it terminates. Delivers hands-free container management.**Independent Test**: Can be tested by accessing the dashboard URL while services are running and verifying all services appear with their current state, logs, and metrics.

- .NET 10 SDK with .NET Aspire workload installed (`dotnet workload install aspire`)

- Docker Desktop (Windows/macOS) or Docker Engine (Linux) version 20.10 or later- How does the developer restart a single service without restarting the entire solution?

- Visual Studio 2025, VS Code with C# Dev Kit extension, or JetBrains Rider

- OpenTelemetry SDK packages for .NET (`OpenTelemetry`, `OpenTelemetry.Instrumentation.AspNetCore`, etc.)- What happens when .NET Aspire is not installed or is the wrong version?

- Aspire.Hosting NuGet packages for AppHost project

- Aspire.ServiceDefaults NuGet packages for service projects- How does the system handle configuration conflicts between local appsettings.json and Aspire-injected configuration?

- Container images: postgres:latest, redis:latest (if caching is added)

- Existing BloodThinnerTracker solution projects (API, Web, Mobile, Shared)- What happens when a container runs out of disk space or memory?**Acceptance Scenarios**:**Acceptance Scenarios**:

- Docker Compose files in samples/ directory for reference configuration

- How does the developer switch between using real containers vs. in-memory test doubles for dependencies?

## Questions



None at this time. All requirements are clear and actionable. The specification follows the prioritized user story approach with independently testable journeys (P1-P3).

## Requirements

1. **Given** the application requires a PostgreSQL database, **When** the developer starts the application, **Then** a PostgreSQL container is automatically started with appropriate configuration1. **Given** services are running, **When** a developer opens the dashboard, **Then** they see a list of all services with their current status (running, stopped, error)

### Functional Requirements

2. **Given** containers are running, **When** the developer stops the application, **Then** containers are either stopped or left running based on configuration2. **Given** a service is experiencing errors, **When** viewing the dashboard, **Then** error indicators are prominently displayed for that service

- **FR-001**: System MUST automatically start all services defined in the AppHost project when F5 is pressed or `dotnet run` is executed

- **FR-002**: System MUST automatically pull and start required Docker containers (PostgreSQL, Redis, etc.) before starting dependent services3. **Given** a container fails to start, **When** the developer views the dashboard, **Then** they see clear error messages explaining why the container failed3. **Given** services are generating logs, **When** viewing a specific service in the dashboard, **Then** real-time logs stream to the dashboard interface

- **FR-003**: System MUST provide automatic service discovery so services can reference each other by logical name rather than hardcoded URLs

- **FR-004**: System MUST automatically generate and inject connection strings for databases and caching services into service configuration4. **Given** the developer needs to reset local data, **When** they trigger a "clean start" command, **Then** containers are recreated with fresh state4. **Given** services are emitting telemetry, **When** viewing metrics, **Then** performance metrics (requests/sec, response times, error rates) are displayed

- **FR-005**: System MUST provide a web-based dashboard accessible at a predictable URL (e.g., http://localhost:15000) showing service status

- **FR-006**: Dashboard MUST display real-time logs from all running services with filtering and search capabilities5. **Given** the dashboard is open, **When** a service stops or starts, **Then** the dashboard updates automatically to reflect the new state

- **FR-007**: Dashboard MUST display distributed traces showing request flows across service boundaries using OpenTelemetry standards

- **FR-008**: Dashboard MUST display metrics (CPU, memory, request rates, error rates) for each service in real-time---

- **FR-009**: System MUST support debugging multiple services simultaneously with breakpoints across service boundaries

- **FR-010**: System MUST handle graceful shutdown of all services and containers when the developer stops the application---

- **FR-011**: System MUST detect and report port conflicts with clear error messages suggesting alternative ports

- **FR-012**: System MUST support environment-specific configuration (Development, Staging) with automatic environment variable injection### User Story 4 - Service Configuration and Discovery (Priority: P2)

- **FR-013**: System MUST provide health check endpoints for all services and display health status in the dashboard

- **FR-014**: System MUST support hot reload for code changes without requiring full application restart---

- **FR-015**: System MUST log container startup failures with actionable error messages in the dashboard

- **FR-016**: System MUST support both Windows (PowerShell) and macOS/Linux (bash) development environmentsAs a developer, I want services to automatically discover each other's endpoints and have their configuration injected at runtime, so I don't have to manually maintain URLs, ports, and connection strings in configuration files.

- **FR-017**: System MUST persist container data between runs (databases retain data unless explicitly reset)

- **FR-018**: System MUST provide a mechanism to reset/clean local data (recreate containers with fresh state)### User Story 4 - Container Lifecycle Management (Priority: P2)

- **FR-019**: System MUST start all services and containers in a single "full stack" profile (multiple profiles can be added in future iterations if needed)

- **FR-020**: Dashboard MUST provide direct links to service endpoints and API documentation (Swagger UI)**Why this priority**: Service discovery is important for multi-service development but builds on the core F5 experience (P1). It reduces configuration burden but is secondary to getting services running.



### Key EntitiesAs a developer, I want required containers (databases, caches, message queues) to be automatically managed so that I don't need to remember docker commands or worry about port conflicts.



- **AppHost Project**: .NET Aspire orchestration project that defines service topology and dependencies**Independent Test**: Can be fully tested by verifying that services can successfully call each other using service names (not hardcoded URLs) and that configuration values are correctly injected. Delivers configuration-free inter-service communication.

- **Service Projects**: Individual ASP.NET Core API, Blazor Web, MAUI backend services that are orchestrated

- **Container Resources**: Docker containers for PostgreSQL, Redis, or other infrastructure dependencies**Why this priority**: Essential for modern microservices development but could technically be worked around with manual Docker commands. Automation is key to developer productivity.

- **Service Discovery Configuration**: Automatically generated endpoint mappings between services

- **Dashboard**: Web-based UI for observing service state, logs, metrics, and traces**Acceptance Scenarios**:

- **OpenTelemetry Instrumentation**: Distributed tracing and metrics collection across services

- **Environment Configuration**: Development, staging, production settings with automatic injection**Independent Test**: Can be tested by starting the application without any containers running and verifying they are pulled, started, and correctly configured automatically.

- **Health Checks**: Service health monitoring endpoints for status tracking

1. **Given** the Web project needs to call the API, **When** the application starts, **Then** the Web project automatically receives the correct API endpoint URL via configuration

## Success Criteria

2. **Given** multiple instances of a service might be running, **When** a client service calls it, **Then** the request is routed to an available instance**Acceptance Scenarios**:

### Measurable Outcomes

3. **Given** services need environment-specific configuration (dev, staging), **When** the developer changes the environment, **Then** appropriate configuration is automatically applied

- **SC-001**: Developer can clone the repository and have a fully functional local development environment running within 5 minutes of pressing F5 (excluding initial Docker image downloads)

- **SC-002**: All services start successfully with zero manual configuration or setup steps required beyond cloning the repository4. **Given** a service endpoint changes (different port), **When** the developer restarts the application, **Then** all dependent services automatically receive the updated endpoint1. **Given** a service requires a PostgreSQL database, **When** the orchestrator starts, **Then** a PostgreSQL container is pulled (if not cached) and started with appropriate configuration

- **SC-003**: Dashboard is accessible immediately after services start and displays accurate real-time status for all services

- **SC-004**: Developer can view logs from all services in the dashboard with response time under 1 second for log queries2. **Given** multiple developers are running services, **When** containers start, **Then** port assignments avoid conflicts and are dynamically assigned

- **SC-005**: Distributed traces appear in the dashboard within 5 seconds of request completion showing complete request flow

- **SC-006**: Service discovery resolves endpoint addresses correctly 100% of the time during local development---3. **Given** containers are running from a previous session, **When** the orchestrator starts, **Then** existing containers are reused if compatible, or recreated if configuration changed

- **SC-007**: Container failures are detected and reported in the dashboard within 3 seconds with actionable error messages

- **SC-008**: Developer can set breakpoints in multiple services and debug cross-service requests without tool switching4. **Given** development is complete, **When** the orchestrator stops, **Then** containers are stopped but persisted volumes remain for next session

- **SC-009**: Application startup time (from F5 to all services healthy) is under 30 seconds on subsequent runs with warm containers

- **SC-010**: Hot reload applies code changes to running services within 2 seconds without requiring full restart### User Story 5 - Integrated Debugging Experience (Priority: P3)



## Assumptions---



- Developers have Docker Desktop (Windows/macOS) or Docker Engine (Linux) installed and runningAs a developer, I want to set breakpoints across multiple services and debug them simultaneously in my IDE, so I can trace issues that span service boundaries without switching tools.

- Developers have .NET 10 SDK installed

- Developers are using Visual Studio 2025, VS Code with C# Dev Kit, or Rider### User Story 5 - Distributed Tracing Visualization (Priority: P3)

- Project will use .NET Aspire 10.x (compatible with .NET 10)

- SQLite is used for local development databases (lightweight, no container needed)**Why this priority**: Multi-service debugging is valuable but less critical than basic observability (P1-P2). Developers can use logs/traces initially and add debugging as needed.

- PostgreSQL containers are used for cloud-like development scenarios (optional)

- Services will use HTTP (not HTTPS) for local development unless specifically testing TLSAs a developer, I want to see request traces across multiple services so that I can understand request flows and identify performance bottlenecks.

- Docker images for required services (PostgreSQL, Redis) are available in public registries

- Developers have at least 8GB RAM and 20GB free disk space for containers**Independent Test**: Can be fully tested by setting breakpoints in multiple projects, making a request that flows across services, and verifying that debugger stops at each breakpoint in sequence. Delivers full-stack debugging capability.

- Network connectivity is available for initial Docker image downloads

- Developers understand basic Docker concepts (containers, images, volumes)**Why this priority**: Advanced observability feature that provides significant value for debugging complex issues but is not essential for basic development workflows.

- Application uses OpenTelemetry SDK for distributed tracing instrumentation

**Acceptance Scenarios**:

## Non-Goals

**Independent Test**: Can be tested by making a request that spans multiple services and viewing the trace in the dashboard showing the complete request path with timing information.

- Production deployment orchestration (use Kubernetes, Azure Container Apps, or AWS ECS instead)

- Multi-developer environment synchronization (each developer has isolated local environment)1. **Given** the developer has set breakpoints in both Web and API projects, **When** they make a request from the browser, **Then** the debugger stops at breakpoints in both projects in the correct sequence

- Automatic schema migrations (developers should run migrations explicitly via CLI tool)

- Performance profiling tools (use dedicated profilers like dotMemory or PerfView)2. **Given** the developer is debugging one service, **When** they want to inspect another service's state, **Then** they can view logs and traces from the Aspire dashboard without stopping the debugger**Acceptance Scenarios**:

- Load testing or stress testing capabilities (use separate tools like k6 or JMeter)

- Security scanning or vulnerability assessment (use dedicated security tools)3. **Given** the developer modifies code while debugging, **When** they use hot reload, **Then** changes are applied without restarting all services

- CI/CD pipeline configuration (handled separately in GitHub Actions)

- Secrets management for production credentials (use Azure Key Vault or similar for production)4. **Given** an exception occurs in a background service, **When** the developer views the dashboard, **Then** they can see the exception details and navigate to the source code1. **Given** a request flows through multiple services, **When** viewing traces in the dashboard, **Then** the complete request path is visualized with timing for each service call



## Technical Constraints2. **Given** a service call fails, **When** viewing the trace, **Then** the failure point is highlighted with error details



- Must use .NET Aspire 10.x for orchestration (aligned with .NET 10 requirement)---3. **Given** traces are being collected, **When** filtering traces, **Then** developers can filter by time range, service, status code, or custom tags

- Must target .NET 10 (net10.0) for all projects

- Must use OpenTelemetry standard for observability (logs, metrics, traces)

- Must support Windows, macOS, and Linux development environments

- Must use Docker for containerized dependencies (not Podman or other alternatives)### Edge Cases---

- Must work with Visual Studio 2025, VS Code with C# Dev Kit, and JetBrains Rider

- Dashboard must be web-based and accessible via modern browsers (Chrome, Edge, Firefox, Safari)

- Service discovery must not require external service mesh or infrastructure (built-in .NET Aspire mechanism)

- Must support both SQLite (in-process) and PostgreSQL (containerized) database scenarios- What happens when a required Docker image is not available locally and cannot be pulled (network offline)?### Edge Cases

- Must integrate with existing project structure (src/BloodThinnerTracker.AppHost/)

- How does the system handle port conflicts when default ports (5000, 15000, etc.) are already in use by other applications?

## Dependencies

- What happens when a service fails to start but others continue running?- What happens when a required container image fails to download (poor network, invalid image)?

- .NET 10 SDK with .NET Aspire workload installed (`dotnet workload install aspire`)

- Docker Desktop (Windows/macOS) or Docker Engine (Linux) version 20.10 or later- How does the developer restart a single service without restarting the entire solution?- How does the system handle port conflicts when required ports are already in use?

- Visual Studio 2025, VS Code with C# Dev Kit extension, or JetBrains Rider

- OpenTelemetry SDK packages for .NET (`OpenTelemetry`, `OpenTelemetry.Instrumentation.AspNetCore`, etc.)- What happens when .NET Aspire is not installed or is the wrong version?- What happens when a service crashes repeatedly (crash loop)?

- Aspire.Hosting NuGet packages for AppHost project

- Aspire.ServiceDefaults NuGet packages for service projects- How does the system handle configuration conflicts between local appsettings.json and Aspire-injected configuration?- How does the orchestrator behave when insufficient system resources are available (memory, CPU)?

- Container images: postgres:latest, redis:latest (if caching is added)

- Existing BloodThinnerTracker solution projects (API, Web, Mobile, Shared)- What happens when a container runs out of disk space or memory?- What happens when multiple developers run the orchestrator simultaneously on the same machine?

- Docker Compose files in samples/ directory for reference configuration

- How does the developer switch between using real containers vs. in-memory test doubles for dependencies?- How are database migrations handled during startup?

## Questions

- What happens when configuration changes require container recreation vs. restart?

None at this time. All requirements are clear and actionable. The specification follows the prioritized user story approach with independently testable journeys (P1-P3).

## Requirements- How does the system handle services that depend on each other (startup ordering)?



### Functional Requirements## Requirements *(mandatory)*



- **FR-001**: System MUST automatically start all services defined in the AppHost project when F5 is pressed or `dotnet run` is executed### Functional Requirements

- **FR-002**: System MUST automatically pull and start required Docker containers (PostgreSQL, Redis, etc.) before starting dependent services

- **FR-003**: System MUST provide automatic service discovery so services can reference each other by logical name rather than hardcoded URLs- **FR-001**: System MUST provide a dedicated orchestrator project that can be set as the startup project in Visual Studio

- **FR-004**: System MUST automatically generate and inject connection strings for databases and caching services into service configuration- **FR-002**: System MUST automatically start all registered services when the orchestrator starts

- **FR-005**: System MUST provide a web-based dashboard accessible at a predictable URL (e.g., http://localhost:15000) showing service status- **FR-003**: System MUST automatically discover and start required container dependencies (databases, caches, message queues)

- **FR-006**: Dashboard MUST display real-time logs from all running services with filtering and search capabilities- **FR-004**: System MUST provide automatic service discovery so services can locate and communicate with each other without hardcoded URLs

- **FR-007**: Dashboard MUST display distributed traces showing request flows across service boundaries using OpenTelemetry standards- **FR-005**: System MUST generate and inject connection strings for all services at runtime

- **FR-008**: Dashboard MUST display metrics (CPU, memory, request rates, error rates) for each service in real-time- **FR-006**: System MUST provide a web-based dashboard accessible via browser showing service status

- **FR-009**: System MUST support debugging multiple services simultaneously with breakpoints across service boundaries- **FR-007**: Dashboard MUST display real-time logs from all running services

- **FR-010**: System MUST handle graceful shutdown of all services and containers when the developer stops the application- **FR-008**: Dashboard MUST display telemetry metrics (requests per second, response times, error rates) for all services

- **FR-011**: System MUST detect and report port conflicts with clear error messages suggesting alternative ports- **FR-009**: Dashboard MUST display environment variables and configuration for each service

- **FR-012**: System MUST support environment-specific configuration (Development, Staging) with automatic environment variable injection- **FR-010**: System MUST handle graceful shutdown of all services and containers when debugging stops

- **FR-013**: System MUST provide health check endpoints for all services and display health status in the dashboard- **FR-011**: System MUST detect and prevent port conflicts by dynamically assigning available ports

- **FR-014**: System MUST support hot reload for code changes without requiring full application restart- **FR-012**: System MUST persist container data volumes across restarts for stateful services (databases)

- **FR-015**: System MUST log container startup failures with actionable error messages in the dashboard- **FR-013**: System MUST provide health check endpoints for all services

- **FR-016**: System MUST support both Windows (PowerShell) and macOS/Linux (bash) development environments- **FR-014**: Dashboard MUST allow viewing and filtering logs by service, time range, and log level

- **FR-017**: System MUST persist container data between runs (databases retain data unless explicitly reset)- **FR-015**: System MUST support distributed tracing across service boundaries

- **FR-018**: System MUST provide a mechanism to reset/clean local data (recreate containers with fresh state)- **FR-016**: Dashboard MUST visualize distributed traces showing request flow through multiple services

- **FR-019**: System MUST start all services and containers in a single "full stack" profile (multiple profiles can be added in future iterations if needed)- **FR-017**: System MUST handle service startup dependencies (e.g., API waits for database to be ready)

- **FR-020**: Dashboard MUST provide direct links to service endpoints and API documentation (Swagger UI)- **FR-018**: System MUST provide clear error messages when required resources cannot be started

- **FR-019**: System MUST support multiple profiles [NEEDS CLARIFICATION: Do we need multiple profiles like "API only" vs "Full stack", or just one "run everything" profile?]

### Key Entities- **FR-020**: Dashboard MUST auto-refresh to show current state without requiring manual page reload



- **AppHost Project**: .NET Aspire orchestration project that defines service topology and dependencies### Key Entities

- **Service Projects**: Individual ASP.NET Core API, Blazor Web, MAUI backend services that are orchestrated

- **Container Resources**: Docker containers for PostgreSQL, Redis, or other infrastructure dependencies- **Orchestrator Project**: The .NET Aspire AppHost project that coordinates all service and container lifecycle

- **Service Discovery Configuration**: Automatically generated endpoint mappings between services- **Service Definition**: Configuration describing how to run a service (project path, environment variables, dependencies)

- **Dashboard**: Web-based UI for observing service state, logs, metrics, and traces- **Container Resource**: Configuration for container-based dependencies (image name, ports, volumes, environment)

- **OpenTelemetry Instrumentation**: Distributed tracing and metrics collection across services- **Service Discovery Registry**: Runtime mapping of service names to actual endpoints (URLs, ports)

- **Environment Configuration**: Development, staging, production settings with automatic injection- **Telemetry Stream**: Real-time flow of logs, metrics, and traces from services to dashboard

- **Health Checks**: Service health monitoring endpoints for status tracking- **Health Check**: Endpoint and logic to determine if a service is ready to accept traffic

- **Trace Span**: Segment of a distributed trace representing work done by a single service

## Success Criteria- **Dashboard Session**: Web interface instance showing current state of all services and resources



### Measurable Outcomes## Success Criteria *(mandatory)*



- **SC-001**: Developer can clone the repository and have a fully functional local development environment running within 5 minutes of pressing F5 (excluding initial Docker image downloads)### Measurable Outcomes

- **SC-002**: All services start successfully with zero manual configuration or setup steps required beyond cloning the repository

- **SC-003**: Dashboard is accessible immediately after services start and displays accurate real-time status for all services- **SC-001**: Developer can start entire application stack (all services + containers) in under 60 seconds with a single F5 press

- **SC-004**: Developer can view logs from all services in the dashboard with response time under 1 second for log queries- **SC-002**: Zero manual configuration changes required to run the application locally after initial repository clone

- **SC-005**: Distributed traces appear in the dashboard within 5 seconds of request completion showing complete request flow- **SC-003**: Dashboard loads within 2 seconds and displays real-time updates with less than 1 second latency

- **SC-006**: Service discovery resolves endpoint addresses correctly 100% of the time during local development- **SC-004**: 100% of service-to-service communication succeeds using automatic service discovery (no hardcoded localhost URLs)

- **SC-007**: Container failures are detected and reported in the dashboard within 3 seconds with actionable error messages- **SC-005**: Developers can identify the source of an error within 2 minutes using dashboard logs and traces

- **SC-008**: Developer can set breakpoints in multiple services and debug cross-service requests without tool switching- **SC-006**: Container startup failures are detected and reported with actionable error messages within 10 seconds

- **SC-009**: Application startup time (from F5 to all services healthy) is under 30 seconds on subsequent runs with warm containers- **SC-007**: System handles up to 10 concurrent local service instances without port conflicts

- **SC-010**: Hot reload applies code changes to running services within 2 seconds without requiring full restart- **SC-008**: Dashboard displays telemetry for 100% of HTTP requests across all services with end-to-end trace visualization

- **SC-009**: New developers can run the full application stack successfully on their first attempt without assistance

## Assumptions- **SC-010**: Development environment setup time reduces from 30+ minutes (manual) to under 5 minutes (automated)



- Developers have Docker Desktop (Windows/macOS) or Docker Engine (Linux) installed and running## Assumptions

- Developers have .NET 10 SDK installed

- Developers are using Visual Studio 2025, VS Code with C# Dev Kit, or Rider1. **Development Environment**: Developers are using Visual Studio 2022+ or Visual Studio Code with C# Dev Kit on Windows, macOS, or Linux

- Project will use .NET Aspire 10.x (compatible with .NET 10)2. **Docker Installation**: Docker Desktop or equivalent is installed and running on the developer machine

- SQLite is used for local development databases (lightweight, no container needed)3. **.NET Version**: Project targets .NET 10 which includes .NET Aspire tooling

- PostgreSQL containers are used for cloud-like development scenarios (optional)4. **Network Access**: Developer machine has internet access to pull container images from Docker Hub

- Services will use HTTP (not HTTPS) for local development unless specifically testing TLS5. **Resource Availability**: Developer machine has sufficient resources (8GB+ RAM, 4+ CPU cores) to run multiple services and containers

- Docker images for required services (PostgreSQL, Redis) are available in public registries6. **Solution Structure**: Existing solution follows standard structure with API, Web, and shared projects

- Developers have at least 8GB RAM and 20GB free disk space for containers7. **Database Choice**: PostgreSQL is the production database, so local development will use PostgreSQL containers

- Network connectivity is available for initial Docker image downloads8. **Service Communication**: Services communicate via HTTP REST APIs (no message queues or gRPC in initial scope)

- Developers understand basic Docker concepts (containers, images, volumes)9. **Authentication**: Local development uses simplified auth (no real OAuth providers required)

- Application uses OpenTelemetry SDK for distributed tracing instrumentation10. **Port Range**: System can use ports in range 5000-6000 for services and 15000-16000 for containers

11. **Telemetry Stack**: OpenTelemetry is the standard for logs, metrics, and traces

## Non-Goals12. **Dashboard Technology**: .NET Aspire includes a built-in dashboard (no custom dashboard implementation needed)



- Production deployment orchestration (use Kubernetes, Azure Container Apps, or AWS ECS instead)## Non-Goals (Out of Scope)

- Multi-developer environment synchronization (each developer has isolated local environment)

- Automatic schema migrations (developers should run migrations explicitly via CLI tool)- **Production Deployment**: This feature is for local development only, not production orchestration

- Performance profiling tools (use dedicated profilers like dotMemory or PerfView)- **Remote Development**: Multi-machine or cloud-based development environments

- Load testing or stress testing capabilities (use separate tools like k6 or JMeter)- **Performance Profiling**: Deep performance analysis tools (CPU profiling, memory profiling) beyond basic metrics

- Security scanning or vulnerability assessment (use dedicated security tools)- **Database Seeding**: Automated test data generation (handled separately)

- CI/CD pipeline configuration (handled separately in GitHub Actions)- **Integration Testing**: Automated test execution (separate feature)

- Secrets management for production credentials (use Azure Key Vault or similar for production)- **CI/CD Pipeline**: Build and deployment automation (separate feature)

- **Multi-OS Container Images**: Supporting different container images for different operating systems

## Technical Constraints- **Service Mesh**: Advanced networking features like traffic splitting, circuit breakers

- **Custom Container Orchestration**: Replacing Docker with Kubernetes or other orchestrators

- Must use .NET Aspire 10.x for orchestration (aligned with .NET 10 requirement)- **Offline Development**: Running without internet access (first run requires image pulls)

- Must target .NET 10 (net10.0) for all projects

- Must use OpenTelemetry standard for observability (logs, metrics, traces)## Technical Constraints

- Must support Windows, macOS, and Linux development environments

- Must use Docker for containerized dependencies (not Podman or other alternatives)1. **Must use .NET Aspire**: Project requirements specify .NET Aspire as the orchestration framework

- Must work with Visual Studio 2025, VS Code with C# Dev Kit, and JetBrains Rider2. **Must support F5 experience**: Primary interaction model is Visual Studio debugging experience

- Dashboard must be web-based and accessible via modern browsers (Chrome, Edge, Firefox, Safari)3. **Must be cross-platform**: Solution must work on Windows, macOS, and Linux

- Service discovery must not require external service mesh or infrastructure (built-in .NET Aspire mechanism)4. **Must use existing project structure**: Cannot require major solution restructuring

- Must support both SQLite (in-process) and PostgreSQL (containerized) database scenarios5. **Must preserve existing functionality**: All current features must continue to work

- Must integrate with existing project structure (src/BloodThinnerTracker.AppHost/)6. **Must use Docker**: Container runtime must be Docker-compatible

7. **Must be version-controlled**: All orchestration configuration must be in source control (no local-only config)

## Dependencies8. **Must be zero-configuration**: Default settings must work out-of-the-box for 90% of developers



- .NET 10 SDK with .NET Aspire workload installed (`dotnet workload install aspire`)## Dependencies

- Docker Desktop (Windows/macOS) or Docker Engine (Linux) version 20.10 or later

- Visual Studio 2025, VS Code with C# Dev Kit extension, or JetBrains Rider### External Dependencies

- OpenTelemetry SDK packages for .NET (`OpenTelemetry`, `OpenTelemetry.Instrumentation.AspNetCore`, etc.)- .NET 10 SDK (includes .NET Aspire workload)

- Aspire.Hosting NuGet packages for AppHost project- Docker Desktop or compatible Docker engine

- Aspire.ServiceDefaults NuGet packages for service projects- Visual Studio 2022 17.10+ or VS Code with C# Dev Kit

- Container images: postgres:latest, redis:latest (if caching is added)

- Existing BloodThinnerTracker solution projects (API, Web, Mobile, Shared)### Internal Dependencies

- Docker Compose files in samples/ directory for reference configuration- Existing BloodThinnerTracker.Api project

- Existing BloodThinnerTracker.Web project

## Questions- Existing BloodThinnerTracker.Shared project

- PostgreSQL database schema and migrations

None at this time. All requirements are clear and actionable. The specification follows the prioritized user story approach with independently testable journeys (P1-P3).

### Prerequisite Features
- None (this is a foundational development infrastructure feature)

## Related Documentation

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [Docker Compose Reference](https://docs.docker.com/compose/) (for comparison/migration)
- Project README: `README.md`
- Copilot Instructions: `.github/copilot-instructions.md`

## Revision History

| Date       | Version | Changes       | Author          |
|------------|---------|---------------|-----------------|
| 2025-10-30 | 0.1     | Initial draft | GitHub Copilot  |

  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]
- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]
- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]
- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]
