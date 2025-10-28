# Research Findings: Docker Deployment Infrastructure

**Created**: 2025-10-28  
**Purpose**: Resolve technical architecture decisions and research findings for deployment infrastructure

---

## Architecture Decisions

### 1. Azure Container Apps Source-Based Deployment
**Decision**: Use Azure Container Apps with .NET SDK container support (no Dockerfile unless multi-stage build required)
**Rationale**: Aligns with constitution, reduces maintenance, leverages Azure buildpacks
**Alternatives Considered**: Dockerfile-based builds (only if multi-stage needed)

### 2. OIDC Setup for GitHub Actions
**Decision**: Use OIDC authentication for GitHub Actions to Azure
**Rationale**: Secure, secretless CI/CD, aligns with Azure best practices
**Alternatives Considered**: Service principal with secrets (rejected: security risk)

### 3. Health Check Endpoint Patterns
**Decision**: Implement `/health` endpoint in API and Web containers
**Rationale**: Enables Azure health monitoring, supports uptime guarantees
**Alternatives Considered**: Custom endpoints (rejected: less standard)

### 4. Secret Management
**Decision**: Use Azure Key Vault for production secrets, user secrets for local
**Rationale**: Secure, compliant, aligns with constitution
**Alternatives Considered**: Hardcoded secrets (rejected: non-compliant)

---

**For full business logic and entity decisions, see canonical research.md in archive.**
