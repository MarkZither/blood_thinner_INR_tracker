<!--
Sync Impact Report:
- Version change: [TEMPLATE] → 1.0.0
- Modified principles: All placeholders replaced with concrete principles
- Added sections: Five core principles covering code quality, testing, UX, performance, and security
- Removed sections: None (template fully implemented)
- Templates requiring updates: ✅ All template references validated
- Follow-up TODOs: None
-->

# Blood Thinner INR Tracker Constitution

**Version**: 1.0.0  
**Ratified**: 2025-10-14  
**Last Amended**: 2025-10-14

## Core Principles

### I. Code Quality & .NET Standards
All code MUST adhere to .NET coding conventions and C# best practices. Code MUST pass automated quality gates including StyleCop, EditorConfig, and Roslyn analyzers. MAUI and Blazor components MUST follow framework-specific patterns and lifecycle management. Dependency injection MUST be used consistently across all layers. Code reviews are mandatory before merge with emphasis on maintainability, readability, and adherence to SOLID principles.

**Rationale**: Medical applications require enterprise-grade code quality for reliability, maintainability, and regulatory compliance. .NET ecosystem provides robust tooling for enforcing these standards.

### II. Testing Discipline & Coverage
All functional code MUST achieve minimum 90% test coverage with unit, integration, and end-to-end tests. Tests MUST be written using xUnit for .NET backend, Playwright for UI automation, and BUnit for Blazor components. Critical user flows (medication reminders, INR logging, authentication) MUST have comprehensive test scenarios covering happy path, edge cases, and error conditions. No production deployment without passing CI/CD pipeline including all test suites.

**Rationale**: Patient safety depends on thoroughly tested functionality. Automated testing prevents regression and ensures consistent behavior across platforms.

### III. User Experience Consistency
User interfaces MUST maintain identical behavior and visual consistency between MAUI mobile and Blazor web applications. All interactive elements MUST comply with WCAG 2.1 AA accessibility standards. Critical safety features (medication alarms, dose logging) MUST have identical user workflows across platforms. UI components MUST be responsive and provide clear feedback for all user actions. Error messages MUST be user-friendly and actionable.

**Rationale**: Consistent UX prevents user confusion that could lead to medication errors. Accessibility ensures the app serves all patients regardless of disabilities.

### IV. Performance & Responsiveness
Application MUST respond to user interactions within 200ms for local operations and 2 seconds for network requests. MAUI apps MUST start within 3 seconds on target devices. Blazor Server components MUST render within 1 second. Database queries MUST be optimized with proper indexing and Entity Framework best practices. Memory usage MUST be monitored and controlled to prevent device resource exhaustion.

**Rationale**: Poor performance can cause users to abandon critical health tasks. Medication reminders require immediate responsiveness for patient safety.

### V. Security & OWASP Compliance
All code MUST prevent OWASP Top 10 vulnerabilities through secure coding practices. Authentication MUST use ASP.NET Core Identity with multi-factor authentication support. All data transmission MUST use HTTPS/TLS 1.3. Input validation MUST be implemented at all API endpoints and UI forms. User data MUST be encrypted at rest using AES-256. Regular security audits and dependency vulnerability scans are mandatory. No hardcoded secrets or credentials in source code.

**Rationale**: Health data requires maximum security protection. OWASP compliance is essential for protecting patient privacy and meeting healthcare regulations.

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