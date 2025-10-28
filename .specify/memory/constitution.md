<!--
Sync Impact Report:
- Version change: 1.2.0 → 1.3.0
- Modified principles: Added Principle VIII (Feature Sizing & Scope Management)
- Added sections: Feature sizing guidelines, branch organization, scope management
- Removed sections: None
- Templates requiring updates: Feature spec template, PR template
- Follow-up TODOs: Review all in-progress features for size compliance
-->

# Blood Thinner INR Tracker Constitution

**Version**: 1.3.0
**Ratified**: 2025-10-14
**Last Amended**: 2025-10-28


## Core Principles

### VII. Configuration Access & Options Pattern
All configuration access MUST use the strongly-typed options pattern. Magic strings (e.g., Configuration["Some:Key"]) are strictly prohibited in application code. Configuration sections MUST be bound to POCOs and injected via IOptions<T> or equivalent. This applies to authentication, connection strings, feature flags, and all other settings. Secrets MUST be loaded from environment variables or user secrets, never hardcoded or scattered in code.

**Rationale**: The options pattern eliminates magic strings, improves maintainability, enables compile-time safety, and supports robust testing. It prevents configuration drift, reduces runtime errors, and ensures secrets are managed securely and centrally.

### I. Code Quality & .NET Standards
All code MUST adhere to .NET coding conventions and C# best practices. Code MUST pass automated quality gates including StyleCop, EditorConfig, and Roslyn analyzers. MAUI and Blazor components MUST follow framework-specific patterns and lifecycle management. Dependency injection MUST be used consistently across all layers. Code reviews are mandatory before merge with emphasis on maintainability, readability, and adherence to SOLID principles.

**Rationale**: Medical applications require enterprise-grade code quality for reliability, maintainability, and regulatory compliance. .NET ecosystem provides robust tooling for enforcing these standards.

### II. Testing Discipline & Coverage
All functional code MUST achieve minimum 90% test coverage with unit, integration, and end-to-end tests. Tests MUST be written using xUnit for .NET backend, Playwright for UI automation, and BUnit for Blazor components. Critical user flows (medication reminders, INR logging, authentication) MUST have comprehensive test scenarios covering happy path, edge cases, and error conditions. No production deployment without passing CI/CD pipeline including all test suites.

**Rationale**: Patient safety depends on thoroughly tested functionality. Automated testing prevents regression and ensures consistent behavior across platforms.

### III. User Experience Consistency & Pure .NET UI
User interfaces MUST maintain identical behavior and visual consistency between MAUI mobile and Blazor web applications. **Blazor Web applications MUST use MudBlazor component library for all UI components to maintain pure .NET implementation without JavaScript dependencies**. JavaScript interop MUST be avoided except where absolutely necessary for browser APIs (clipboard, file downloads). Charts, dialogs, data grids, and interactive components MUST use MudBlazor's native C# implementations. All interactive elements MUST comply with WCAG 2.1 AA accessibility standards. Critical safety features (medication alarms, dose logging) MUST have identical user workflows across platforms. UI components MUST be responsive and provide clear feedback for all user actions. Error messages MUST be user-friendly and actionable.

**Rationale**: Consistent UX prevents user confusion that could lead to medication errors. Pure .NET implementation eliminates JavaScript prerendering issues, improves maintainability, provides type safety, and aligns with Blazor best practices. MudBlazor provides professional Material Design components with comprehensive accessibility support. Accessibility ensures the app serves all patients regardless of disabilities.

### IV. Performance & Responsiveness
Application MUST respond to user interactions within 200ms for local operations and 2 seconds for network requests. MAUI apps MUST start within 3 seconds on target devices. Blazor Server components MUST render within 1 second. Database queries MUST be optimized with proper indexing and Entity Framework best practices. Memory usage MUST be monitored and controlled to prevent device resource exhaustion.

**Rationale**: Poor performance can cause users to abandon critical health tasks. Medication reminders require immediate responsiveness for patient safety.

### V. Security & OWASP Compliance
All code MUST prevent OWASP Top 10 vulnerabilities through secure coding practices. Authentication MUST use ASP.NET Core Identity with multi-factor authentication support. All data transmission MUST use HTTPS/TLS 1.3. Input validation MUST be implemented at all API endpoints and UI forms. User data MUST be encrypted at rest using AES-256. Regular security audits and dependency vulnerability scans are mandatory. No hardcoded secrets or credentials in source code.

**Rationale**: Health data requires maximum security protection. OWASP compliance is essential for protecting patient privacy and meeting healthcare regulations.

### VI. Cloud Deployment & Container Strategy
API services MUST use **source-based deployments** to Azure Container Apps leveraging .NET SDK container support. Dockerfiles MUST NOT be used unless absolutely necessary for complex multi-stage builds. All container configuration MUST be declared in `.csproj` files using `<EnableSdkContainerSupport>`, `<ContainerPort>`, and `<ContainerBaseImage>` properties. Deployment pipelines MUST use Azure's Oryx buildpacks for automatic .NET detection and optimization. Container images MUST use official Microsoft .NET runtime images from `mcr.microsoft.com`. Port configuration MUST be explicit (5234 for HTTP, 7234 for HTTPS) and documented. Infrastructure as Code (IaC) MUST be used for all Azure resources. GitHub Actions MUST handle CI/CD with proper secret management and OIDC authentication.

**Rationale**: Source-based builds align with modern .NET 10+ capabilities, reduce maintenance overhead, eliminate Dockerfile complexity, and leverage Azure's optimized build pipelines. This approach follows Microsoft's recommended practices for .NET cloud-native applications and simplifies the deployment process while maintaining security and reliability.

### VIII. Feature Sizing & Scope Management
Features MUST be scoped to 2-3 weeks maximum effort per feature. Features exceeding this size MUST be split into smaller, independently deliverable features. Each feature MUST have clear boundaries with a single primary concern (authentication OR deployment OR UI, not multiple). Pull requests MUST be ≤500 lines of changed code (excluding generated code, migrations, package-lock files). Features MUST be independently deployable behind feature flags. Feature specs MUST define explicit non-goals to prevent scope creep. Branch naming MUST follow `feature/NNN-short-description` convention where NNN is a zero-padded feature number.

**Rationale**: Large features increase review time, testing complexity, and deployment risk. Smaller features enable faster feedback cycles, easier rollback, parallel development, and more predictable delivery. The 2-3 week limit ensures features remain focused and prevent scope creep that delays valuable functionality from reaching users. Feature flags enable gradual rollout and quick rollback without deployment.

## Governance

### Amendment Process
Constitution changes require:
1. Documented rationale with security and patient safety impact assessment
2. Review by technical lead, security officer, and product owner
3. Version increment following semantic versioning (MAJOR.MINOR.PATCH)
4. Update of all dependent templates and CI/CD pipeline configurations
5. Communication to all development team members

### Compliance Review
All pull requests MUST demonstrate adherence through:
- Automated quality gate passage (StyleCop, analyzers, tests)
- Security scan results showing no high/critical vulnerabilities
- Performance benchmark validation for affected components
- Accessibility audit results for UI changes
- Code review approval from senior developer

### Enforcement Mechanisms
- Pre-commit hooks prevent commits that violate formatting standards
- CI/CD pipeline blocks deployment on test failures or security issues
- Regular architecture reviews ensure ongoing compliance
- Monthly security dependency updates are mandatory
- Performance monitoring alerts trigger immediate investigation

### Versioning Policy
- MAJOR: Principle removal or fundamental redefinition
- MINOR: New principle addition or significant guidance expansion
- PATCH: Clarifications, examples, or non-semantic improvements
