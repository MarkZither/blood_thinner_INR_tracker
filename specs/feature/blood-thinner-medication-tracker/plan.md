# Implementation Plan: Blood Thinner Medication & INR Tracker

**Branch**: `feature/blood-thinner-medication-tracker` | **Date**: 2025-10-15 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/blood-thinner-medication-tracker/spec.md`

## Summary

Build a cross-platform blood thinner medication and INR tracking application with persistent reminders, 12-hour safety windows, configurable INR schedules, and cross-device data synchronization. The solution provides multiple frontend options (MAUI mobile, Blazor web, Console tool, MCP server) backed by a secure .NET Web API with Aspire orchestration, focusing on patient safety and healthcare data protection.

## Technical Context

**Language/Version**: .NET 10 (C# 13) - Latest LTS with enhanced performance, AOT support, and improved MAUI reliability  
**Primary Dependencies**: .NET MAUI, Blazor Server/WebAssembly, ASP.NET Core Web API, .NET Aspire, Entity Framework Core  
**Storage**: SQLite (local encrypted) + PostgreSQL/SQL Server (cloud) with EF Core multi-provider  
**Testing**: xUnit (backend), Playwright (E2E), BUnit (Blazor), NUnit (MAUI)  
**Target Platform**: iOS/Android (MAUI), Web browsers (Blazor), Windows/macOS/Linux (Console), Docker containers  
**Project Type**: Multi-platform (mobile + web + console + MCP server)  
**Performance Goals**: <200ms local operations, <2s network requests, <3s app startup, 99.9% reminder delivery  
**Constraints**: OWASP compliance, AES-256 encryption, 12-hour medication window enforcement, cross-device sync <30s  
**Scale/Scope**: Individual users, ~50 API endpoints, 4 frontend applications, healthcare data compliance

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**✅ I. Code Quality & .NET Standards**: Plan uses .NET ecosystem (MAUI, Blazor, ASP.NET Core) with established conventions and tooling (StyleCop, EditorConfig, Roslyn analyzers). Dependency injection patterns planned throughout.

**✅ II. Testing Discipline & Coverage**: 90% coverage mandate with xUnit (backend), Playwright (E2E), BUnit (Blazor). Critical medication and INR flows will have comprehensive test scenarios including safety edge cases.

**✅ III. User Experience Consistency**: Shared components between MAUI and Blazor ensure consistent UX. WCAG 2.1 AA compliance planned for all interactive elements. Medical safety features (12-hour window, INR validation) will have identical behavior across platforms.

**✅ IV. Performance & Responsiveness**: SQLite local storage ensures <200ms local operations. Background sync architecture maintains performance. Specific targets: 3s app startup, 1s Blazor rendering, optimized EF queries.

**✅ V. Security & OWASP Compliance**: Azure AD/Google OAuth integration, AES-256 encryption for local data, HTTPS/TLS 1.3 for all communications. Healthcare data protection with input validation and secure coding practices.

**GATES ASSESSMENT**: All constitutional requirements satisfied. No violations. ✅ PROCEED TO PHASE 0.

## Project Structure

### Documentation (this feature)

```
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

Following David Fowler's repository layout conventions:

```
blood_thinner_INR_tracker/
├── src/
│   ├── BloodThinnerTracker.AppHost/           # .NET Aspire orchestration
│   ├── BloodThinnerTracker.ServiceDefaults/   # Shared Aspire configuration  
│   ├── BloodThinnerTracker.Api/               # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Data/
│   │   └── Models/
│   ├── BloodThinnerTracker.Mobile/            # .NET MAUI (iOS/Android)
│   │   ├── Platforms/
│   │   ├── Pages/
│   │   ├── ViewModels/
│   │   └── Services/
│   ├── BloodThinnerTracker.Web/               # Blazor Server/WebAssembly
│   │   ├── Components/
│   │   ├── Pages/
│   │   └── Services/
│   ├── BloodThinnerTracker.Cli/               # Console tool (.NET tool)
│   │   └── Commands/
│   ├── BloodThinnerTracker.Mcp/               # MCP Server
│   │   └── Handlers/
│   └── BloodThinnerTracker.Shared/            # Shared models/contracts
│       ├── Models/
│       ├── Contracts/
│       └── Extensions/
├── tests/
│   ├── BloodThinnerTracker.Api.Tests/
│   ├── BloodThinnerTracker.Mobile.Tests/
│   ├── BloodThinnerTracker.Web.Tests/
│   └── BloodThinnerTracker.Integration.Tests/
├── docs/
│   ├── api/                                   # API documentation
│   ├── deployment/                            # Deployment guides
│   └── user-guide/                            # End-user documentation
├── samples/
│   ├── basic-setup/                           # Simple setup example
│   └── advanced-config/                       # Advanced configuration
├── tools/
│   ├── scripts/                               # Build and deployment scripts
│   └── generators/                            # Code generators
└── .github/
    ├── workflows/                             # CI/CD workflows
    └── copilot-instructions.md                # Development guidelines
```

**Structure Decision**: Multi-platform .NET solution leveraging Aspire for orchestration and shared infrastructure. Each platform (mobile, web, console, MCP) has dedicated projects sharing common models and business logic through BloodThinnerTracker.Shared. Repository follows David Fowler's conventions with clear separation of source, tests, documentation, samples, and tooling.

## Complexity Tracking

*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

---

## Phase 0: Research & Architecture Decisions ✅

**Status**: COMPLETED  
**Deliverable**: `/specs/feature/blood-thinner-medication-tracker/research.md`

✅ MCP server implementation approach (local development tool with JSON-RPC 2.0)  
✅ Cross-platform authentication patterns (unified abstraction with platform-specific OAuth)  
✅ Data synchronization strategies (hybrid sync with real-time critical updates)  
✅ Local encryption and security measures (SQLCipher with AES-256, platform keychains)  
✅ Notification infrastructure design (Azure Notification Hubs with native fallback)  
✅ OWASP Top 10 compliance mapping  
✅ OpenTelemetry observability patterns  
✅ Container deployment strategy

## Phase 1: Data Design & API Contracts ✅

**Status**: COMPLETED  
**Deliverables**: 
- ✅ `data-model.md` - Entity definitions and relationships
- ✅ `/contracts/auth-api.md` - Authentication service contracts
- ✅ `/contracts/medication-api.md` - Medication management contracts  
- ✅ `/contracts/inr-api.md` - INR tracking contracts
- ✅ `quickstart.md` - Development environment setup guide

### Data Model Completed ✅
✅ User management with OAuth integration (User, UserDevice, UserPreferences)  
✅ Medication scheduling with safety constraints (Medication, MedicationSchedule, MedicationLog)  
✅ INR testing with configurable schedules (INRTest, INRSchedule)  
✅ Cross-device synchronization metadata (SyncMetadata)  
✅ Audit logging for healthcare compliance (AuditLog)  
✅ Database indexes and constraints for performance  
✅ Entity Framework migration strategy

### API Contract Specifications Completed ✅
✅ Authentication endpoints (OAuth2 + JWT, device registration)  
✅ Medication CRUD operations with validation and safety checks  
✅ INR tracking with trend analysis and TTR calculations  
✅ Real-time notification triggers via SignalR  
✅ Data export for healthcare providers (JSON, CSV, PDF)  
✅ Adherence analytics and dosage recommendations  
✅ Error handling and safety alert specifications

### Development Environment Setup Completed ✅
✅ .NET 10 installation guide with Aspire workload  
✅ Database setup (Docker PostgreSQL + Redis, SQLite fallback)  
✅ OAuth provider configuration (Azure AD + Google)  
✅ Multi-platform testing framework (xUnit, Playwright, BUnit)  
✅ Container orchestration with Aspire dashboard  
✅ API documentation with Swagger/Scalar integration  
✅ Performance monitoring and troubleshooting guide

## Phase 2: Implementation

**Status**: PENDING  
**Prerequisites**: Complete Phase 1 data design and API contracts

### Core Implementation Areas
1. **Backend API Development**
   - ASP.NET Core Web API with Entity Framework
   - Authentication middleware and JWT validation
   - Real-time SignalR hubs for notifications
   - Health checks and observability integration

2. **Mobile Application (MAUI)**
   - Cross-platform UI with Blazor Hybrid
   - Local SQLite with encryption (SQLCipher)
   - Native authentication flows
   - Offline-first data synchronization

3. **Web Application (Blazor Server)**
   - Server-side rendering with SignalR
   - Responsive design for mobile and desktop
   - Real-time updates and notifications
   - Healthcare data export capabilities

4. **Console Tool (NuGet Package)**
   - Command-line interface for power users
   - Bulk data import/export functionality
   - Automated reporting and analytics
   - CI/CD integration for healthcare workflows

5. **MCP Server**
   - Local HTTP server with JSON-RPC 2.0
   - Read-only access to health metrics
   - AI assistant integration capabilities
   - Privacy-preserving data aggregation

### Implementation Timeline
- **Week 1-2**: Backend API and database setup
- **Week 3-4**: Mobile application core features
- **Week 5**: Web application development
- **Week 6**: Console tool and MCP server
- **Week 7**: Integration testing and security validation
- **Week 8**: Performance optimization and deployment
