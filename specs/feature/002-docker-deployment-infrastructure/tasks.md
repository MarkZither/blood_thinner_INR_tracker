# Tasks: Docker Deployment Infrastructure

**Feature**: Docker Deployment Infrastructure
**Branch**: feature/002-docker-deployment-infrastructure
**Date**: 2025-10-28

---

## Phase 1: Setup
- [ ] T001 Create project structure per plan.md
- [ ] T002 Initialize .NET 10 SDK and global.json in repo root
- [ ] T003 [P] Install Docker Desktop and Azure CLI (see quickstart.md) [OUT OF SCOPE]
- [ ] T004 [P] Set up user secrets for local development in src/BloodThinnerTracker.Api and src/BloodThinnerTracker.Web
- [ ] T005 [P] Create appsettings.Development.json with placeholder config in src/BloodThinnerTracker.Api and src/BloodThinnerTracker.Web

## Phase 2: Foundational Infrastructure
- [ ] T006 Create docker-compose.yml for API, Web, and PostgreSQL containers in repo root [OUT OF SCOPE]
- [ ] T007 [P] Create Dockerfile.api and Dockerfile.web in src/BloodThinnerTracker.Api and src/BloodThinnerTracker.Web [OUT OF SCOPE]
- [ ] T008 [P] Implement health check endpoint in src/BloodThinnerTracker.Api/Controllers/HealthController.cs
- [ ] T009 [P] Implement health check endpoint in src/BloodThinnerTracker.Web/Components/Health.razor
- [ ] T010 [P] Create azuredeploy.bicep for Azure resources in repo root [OUT OF SCOPE]
- [ ] T011 [P] Create .github/workflows/deploy.yml for CI/CD pipeline [OUT OF SCOPE]
- [ ] T012 [P] Configure Azure Key Vault integration for secrets in src/BloodThinnerTracker.Api and src/BloodThinnerTracker.Web

## Phase 3: User Story 1 - Local Docker Setup
- [ ] T013 [US1] Implement Docker Compose startup logic in src/BloodThinnerTracker.Api and src/BloodThinnerTracker.Web [OUT OF SCOPE]
- [ ] T014 [US1] Ensure database migrations run automatically on container startup in src/BloodThinnerTracker.Api/Data
- [ ] T015 [US1] Enable hot reload for code changes in Docker containers [OUT OF SCOPE]
- [ ] T016 [US1] Load local environment variables from .env file in docker-compose.yml [OUT OF SCOPE]

## Phase 6: User Story 4 - Azure Deployment
- [ ] T025 [US4] Implement GitHub Actions workflow for CI/CD in .github/workflows/deploy.yml [OUT OF SCOPE]
- [ ] T026 [US4] Deploy API container to Azure Container Apps [OUT OF SCOPE]
- [ ] T027 [US4] Deploy Web container to Azure Container Apps [OUT OF SCOPE]
- [ ] T028 [US4] Provision PostgreSQL database in Azure [OUT OF SCOPE]
- [ ] T029 [US4] Configure connection strings via Azure Key Vault
- [ ] T030 [US4] Enable HTTPS with valid SSL certificates in Azure [OUT OF SCOPE]
- [ ] T031 [US4] Configure health checks for all services in Azure [OUT OF SCOPE]

## Final Phase: Polish & Cross-Cutting Concerns
- [ ] T032 [P] Validate OWASP compliance and AES-256 encryption in src/BloodThinnerTracker.Api and src/BloodThinnerTracker.Web
- [ ] T033 [P] Document deployment steps and troubleshooting in quickstart.md
- [ ] T034 [P] Review and update deployment-quality.md checklist
- [ ] T035 [P] Ensure 90%+ test coverage for deployment logic [OUT OF SCOPE]
- [ ] T036 [P] Final code review and PR preparation

---

## Dependencies
- User Story 1 (Local Docker Setup) must be completed before User Stories 2, 3, and 4 [OUT OF SCOPE]
- User Stories 2 and 3 (OAuth) can be implemented in parallel after US1
- User Story 4 (Azure Deployment) depends on completion of US1

## Parallel Execution Examples
- T003, T004, T005 can be executed in parallel [T003 OUT OF SCOPE]
- T007–T012 can be executed in parallel [T007,T010,T011 OUT OF SCOPE]
- T017–T020 (Microsoft OAuth) and T021–T024 (Google OAuth) can be executed in parallel after US1

## MVP Scope
- Complete Phase 1, Phase 2, and User Story 1 (T001–T016) [UPDATED: Exclude Docker and Azure tasks; focus on local dev without Docker and Key Vault integration]

## Format Validation
- All tasks follow strict checklist format
- Each user story phase has independent test criteria
- File paths are specified for every task
