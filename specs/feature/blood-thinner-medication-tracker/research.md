# Research Findings: Blood Thinner Medication & INR Tracker

**Created**: 2025-10-15  
**Purpose**: Resolve technical architecture decisions and research findings

---

## Architecture Decisions

### 1. MCP (Model Context Protocol) Server Implementation

**Decision**: Implement MCP server as a local development and health data integration tool.

**Rationale**: MCP servers provide structured access to application data for AI assistants and development workflows. For health applications:
- Enables AI-assisted health trend analysis and reporting
- Provides structured data export for healthcare consultations
- Local-only access ensures patient data privacy
- Integration with development tools for testing and debugging

**Implementation Approach**:
- Local HTTP server with JSON-RPC 2.0 protocol
- Read-only access to aggregated health metrics
- Authentication via local API keys
- Export capabilities for medical reports

**Alternatives Considered**: 
- Cloud-based MCP service (rejected: privacy concerns)
- File-based integration (rejected: real-time limitations)

---

### 2. Cross-Platform Authentication Architecture

**Decision**: Unified authentication abstraction with platform-specific OAuth implementations.

**Rationale**: Each platform requires native authentication flows for optimal user experience while maintaining security:
- MAUI: Native Azure AD/Google OAuth with platform keychain storage
- Blazor: Server-side OAuth flows with secure cookie authentication
- Console: Device code flow for headless scenarios
- MCP Server: Local API key authentication

**Implementation Details**:
- Shared JWT token validation in backend API
- Platform-specific secure storage (iOS Keychain, Android KeyStore, Windows DPAPI)
- Certificate pinning for OAuth endpoints
- Automatic token refresh with fallback mechanisms

**Security Considerations**:
- No OAuth secrets in client applications
- Proof Key for Code Exchange (PKCE) for mobile flows
- Token binding to device identifiers
- Suspicious activity monitoring across IP addresses

---

### 3. Data Synchronization Strategy

**Decision**: Hybrid sync with real-time critical updates and eventual consistency for historical data.

**Rationale**: Medication timing is safety-critical requiring immediate sync, while historical data can use eventual consistency for performance:
- **Real-time**: Medication schedules, reminders, safety alerts
- **Eventual**: Historical logs, trend data, user preferences
- **Offline-first**: Local SQLite as single source of truth

**Sync Architecture**:
- SignalR hubs for real-time medication/INR reminders
- Background service for historical data synchronization
- Conflict resolution: Last-writer-wins with timestamps
- Retry logic with exponential backoff
- Delta sync to minimize bandwidth usage

**Performance Targets**:
- Critical updates: <5 seconds cross-device
- Historical sync: <30 seconds for recent data
- Offline operation: Full functionality without connectivity

---

### 4. Local Data Encryption & Security

**Decision**: SQLCipher for local encryption with platform-specific key management.

**Rationale**: Healthcare data requires encryption at rest with secure key management:
- SQLCipher provides AES-256 encryption for SQLite databases
- Platform keychains ensure secure key storage
- Zero-knowledge architecture: keys never leave device
- FIPS 140-2 compliance for healthcare requirements

**Key Management Strategy**:
- PBKDF2 key derivation with device-specific salt
- Platform secure storage (iOS Keychain, Android KeyStore)
- Key rotation on security events
- Secure backup and recovery mechanisms

**Data Classification**:
- **Public**: User IDs, timestamps, enumeration values
- **Sensitive**: Medication dosages, INR values, schedules
- **Highly Sensitive**: Personal notes, location data

---

### 5. Notification Infrastructure

**Decision**: Azure Notification Hubs with platform-native fallback for medication reminders.

**Rationale**: Medication reminders are safety-critical requiring maximum reliability:
- Azure Notification Hubs provides cross-platform push notification orchestration
- Platform-native implementations ensure reliable delivery
- Local notification scheduling for offline scenarios
- Health-specific notification priorities and persistence

**Implementation Architecture**:
- Azure Notification Hubs for centralized management
- Platform-specific implementations (UNNotificationCenter, NotificationManager)
- Local SQLite notification queue for offline persistence
- Delivery confirmation and retry mechanisms
- Critical alert bypassing Do Not Disturb settings

**Notification Types**:
- **Critical**: Medication reminders (non-dismissible)
- **Important**: INR test reminders, safety alerts
- **Informational**: Sync completion, trend insights

---

## Technology Integration Patterns

### .NET Aspire Orchestration

**Pattern**: Service discovery and configuration management for multi-container deployments.

**Benefits**:
- Simplified local development with service orchestration
- Built-in observability with OpenTelemetry integration
- Configuration management across multiple services
- Health check aggregation and monitoring

**Implementation**:
- AppHost project for service orchestration
- ServiceDefaults for shared configuration
- Redis for caching and session state
- PostgreSQL for cloud database hosting

---

### Blazor + MAUI Shared Components

**Pattern**: Shared UI component library with platform-specific rendering.

**Benefits**:
- Consistent user experience across platforms
- Reduced development and maintenance overhead
- Single source of truth for business logic
- Platform-specific optimizations where needed

**Architecture**:
- Blazor Hybrid for MAUI integration
- Shared component library for common UI elements
- Platform-specific styling and navigation
- Responsive design with mobile-first approach

---

### Entity Framework Multi-Provider Architecture

**Pattern**: Single data model with multiple database providers for local and cloud storage.

**Benefits**:
- Consistent data access patterns across environments
- Seamless migration between database providers
- Entity validation and business rule enforcement
- Automatic schema migration and versioning

**Configuration**:
- SQLite provider for local MAUI applications
- PostgreSQL provider for cloud deployment
- SQL Server provider for enterprise scenarios
- In-memory provider for testing scenarios

---

## Security Implementation

### OWASP Top 10 Compliance

**A01 - Broken Access Control**:
- Role-based authorization with user isolation
- API endpoint protection with JWT validation
- Resource-level access control for health data

**A02 - Cryptographic Failures**:
- AES-256 encryption for data at rest
- TLS 1.3 for data in transit
- Proper key management with platform keychains

**A03 - Injection**:
- Parameterized queries with Entity Framework
- Input validation at API boundaries
- SQL injection prevention with ORM patterns

**A04 - Insecure Design**:
- Security-by-design architecture
- Threat modeling for health data flows
- Principle of least privilege implementation

**A05 - Security Misconfiguration**:
- Secure defaults in all configurations
- Regular security dependency updates
- Container security scanning in CI/CD

**A06 - Vulnerable Components**:
- Automated dependency vulnerability scanning
- Regular updates of NuGet packages
- Component inventory and risk assessment

**A07 - Identification and Authentication Failures**:
- Multi-factor authentication support
- Secure session management
- Password policy enforcement

**A08 - Software and Data Integrity Failures**:
- Code signing for mobile applications
- Integrity validation for data synchronization
- Secure CI/CD pipeline with artifact verification

**A09 - Insufficient Logging & Monitoring**:
- Comprehensive audit logging for health data access
- Real-time monitoring for security events
- SIEM integration for threat detection

**A10 - Server-Side Request Forgery**:
- Input validation for external requests
- Network segmentation for backend services
- Allow-list approach for external integrations

---

## Performance & Observability

### OpenTelemetry Implementation

**Metrics**:
- Medication reminder delivery rates and latency
- INR data entry completion rates
- Cross-device synchronization performance
- API endpoint response times and error rates

**Logging**:
- Structured logging with health data anonymization
- Audit trails for medication and INR entries
- Error tracking with patient privacy protection
- Performance bottleneck identification

**Tracing**:
- Distributed tracing across service boundaries
- User journey tracking for UX optimization
- Database query performance monitoring
- Authentication flow analysis

### Health Checks

**Application Health**:
- Database connectivity and performance
- Authentication service availability
- Notification service functionality
- Cross-service communication health

**Business Logic Health**:
- Medication reminder accuracy and delivery
- Data synchronization integrity
- Encryption service functionality
- Backup and recovery system status

---

## Deployment & DevOps

### Container Strategy

**Base Images**: 
- Microsoft .NET runtime containers for minimal attack surface
- Multi-stage builds for optimized production images
- Security scanning with Trivy and Docker Scout

**Registry Management**:
- Docker Hub public registry for open-source components
- Azure Container Registry for private application images
- Image signing and vulnerability scanning

### CI/CD Pipeline

**Build Pipeline**:
- Automated testing with 90% coverage enforcement
- Security scanning for vulnerabilities and secrets
- Code quality analysis with SonarQube
- Performance benchmarking for regression detection

**Deployment Pipeline**:
- Blue-green deployment for zero-downtime updates
- Automated rollback on health check failures
- Database migration validation
- Monitoring and alerting integration

---

**Phase 0 Complete**: All architecture decisions finalized. Ready for Phase 1 data modeling and API contract design.