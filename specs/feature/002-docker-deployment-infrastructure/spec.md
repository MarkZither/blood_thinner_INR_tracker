# Feature Specification: Docker Deployment Infrastructure

**Feature Branch**: `feature/002-docker-deployment-infrastructure`  
**Created**: 2025-10-28  
**Status**: In Progress  
**Part of**: Blood Thinner Medication & INR Tracker (Feature Split)

## Overview

This feature establishes the Docker containerization and Azure Container Apps deployment infrastructure for the Blood Thinner Tracker application. It includes OAuth authentication setup (Microsoft Azure AD + Google), PostgreSQL cloud database configuration, and CI/CD pipeline establishment.

<!-- OUT OF SCOPE NOTE: The containerization (Docker Compose/local containers), Azure Container Apps production deployment, and related CI/CD/infra provisioning are OUT OF SCOPE for this iteration. These will be handled in a later feature where .NET Aspire orchestration and local development with Aspire are implemented. The Key Vault integration and authentication configuration remain IN SCOPE for this iteration. -->

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developer Local Setup with Docker
<!--
**OUT OF SCOPE for current feature - will be implemented in a future Aspire/local orchestration feature.**
**As a** developer  
**I want** to run the complete application stack locally using Docker Compose  
**So that** I can develop and test without complex environment setup

**Acceptance Criteria**:
- [ ] Running `docker-compose up` starts all services (API, Web, Database)
- [ ] Services can communicate with each other
- [ ] Database migrations run automatically on startup
- [ ] Hot reload works for code changes
- [ ] Local environment variables loaded from `.env` file
-->

### User Story 2 - User Authentication with Microsoft Account
<!--
**OUT OF SCOPE for current feature - will be implemented in a future feature.**
**As a** user  
**I want** to sign in with my Microsoft account  
**So that** I can access my medication data securely across devices

**Acceptance Criteria**:
- [ ] "Sign in with Microsoft" button visible on login page
- [ ] Clicking button redirects to Microsoft OAuth flow
- [ ] After successful auth, redirected back to app with user logged in
- [ ] User profile information (name, email) displayed in app
- [ ] Authentication state persists across browser sessions
-->

### User Story 3 - User Authentication with Google Account
<!--
**OUT OF SCOPE for current feature - will be implemented in a future feature.**
**As a** user  
**I want** to sign in with my Google account  
**So that** I can access my medication data without creating a separate account

**Acceptance Criteria**:
- [ ] "Sign in with Google" button visible on login page
- [ ] Clicking button redirects to Google OAuth flow
- [ ] After successful auth, redirected back to app with user logged in
- [ ] User profile information (name, email) displayed in app
- [ ] Authentication state persists across browser sessions
-->

<!--
### User Story 4 - Production Deployment to Azure
**[Out of Scope for this feature iteration. Will revisit later.]**
**As a** DevOps engineer  
**I want** automated deployment to Azure Container Apps  
**So that** the application is production-ready and automatically updated

**Acceptance Criteria**:
- [ ] API container runs in Azure Container Apps
- [ ] Web container runs in Azure Container Apps
- [ ] PostgreSQL database provisioned in Azure
- [ ] Connection strings configured via Azure Key Vault
- [ ] HTTPS enabled with valid SSL certificates
- [ ] Health checks configured for all services
-->

## Technical Implementation

### Infrastructure Components

1. **Docker Containers**
   - `bloodthinnertracker-api`: ASP.NET Core Web API
   - `bloodthinnertracker-web`: Blazor Server application
   - `postgres`: PostgreSQL database (development)

2. **Azure Resources**
   - Azure Container Apps (API + Web)
   - Azure Container Registry
   - Azure Database for PostgreSQL
   - Azure Key Vault (secrets management)
   - Azure Monitor (logging + diagnostics)

3. **Authentication Providers**
   - Microsoft Azure AD OAuth 2.0
   - Google OAuth 2.0
   - JWT token-based authentication

### Configuration Files

- `docker-compose.yml`: Local development orchestration
- `Dockerfile.api`: API container image
- `Dockerfile.web`: Web container image
- `.github/workflows/deploy.yml`: CI/CD pipeline
- `azuredeploy.bicep`: Azure infrastructure as code

### Security Implementation

- User secrets for local development (no secrets in code)
- Azure Key Vault for production secrets
- HTTPS everywhere (HSTS enabled)
- OAuth state parameter validation
- CSRF protection for authentication flows
- Connection strings encrypted at rest

## Dependencies

- .NET 10 SDK
- Docker Desktop 4.x
- Azure CLI 2.x
- GitHub Actions (for CI/CD)

## Testing Strategy

### Unit Tests
- OAuth configuration validation
- JWT token generation/validation
- Database connection string parsing

### Integration Tests
- OAuth flow end-to-end (using test accounts)
- Database migrations in container
- API health checks
- Web health checks

### Manual Testing
- Local Docker Compose startup
- Sign in with Microsoft (dev tenant)
- Sign in with Google (test account)
- Azure deployment verification

## Out of Scope

- User interface implementation (covered in Feature 003)
- Medication tracking functionality (covered in Feature 004)
- INR test logging (covered in Feature 005)
- Notification system (covered in Feature 006)

## Success Metrics

- [ ] Docker Compose brings up all services in < 2 minutes
- [ ] Azure deployment completes in < 15 minutes
- [ ] All health checks passing in production
- [ ] No secrets exposed in code or configuration files

## References

- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [Microsoft Identity Platform OAuth 2.0](https://learn.microsoft.com/entra/identity-platform/v2-oauth2-auth-code-flow)
- [Google OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)
- [Docker Compose Documentation](https://docs.docker.com/compose/)

## Medical Disclaimer

⚠️ **This application is not a medical device and is not intended to diagnose, treat, cure, or prevent any disease. Always consult with your healthcare provider regarding your medication regimen and blood test schedule.**
