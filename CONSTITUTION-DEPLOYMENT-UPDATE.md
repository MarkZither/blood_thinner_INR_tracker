# Constitution & Task Updates - Cloud Deployment Standards

**Date**: October 24, 2025  
**Version**: Constitution 1.1.0 → 1.2.0  
**Scope**: Cloud deployment strategy and Azure Container Apps configuration

---

## Constitution Updates

### New Principle: VI. Cloud Deployment & Container Strategy

Added comprehensive deployment guidance to the project constitution:

#### Key Requirements:

1. **Source-Based Deployments** (MUST)
   - API services MUST use source-based deployments to Azure Container Apps
   - Leverage .NET SDK container support (not Dockerfiles)
   - Use Azure's Oryx buildpacks for automatic .NET detection

2. **Container Configuration in .csproj** (MUST)
   - All container settings declared in project files
   - Required properties: `<EnableSdkContainerSupport>`, `<ContainerPort>`, `<ContainerBaseImage>`
   - Dockerfiles MUST NOT be used unless absolutely necessary

3. **Port Standardization** (MUST)
   - Port 5234: HTTP (primary)
   - Port 7234: HTTPS (with certificates)
   - Configuration must be explicit and documented

4. **Infrastructure as Code** (MUST)
   - All Azure resources managed via IaC (Bicep/Terraform)
   - GitHub Actions for CI/CD
   - OIDC authentication for secure deployments

5. **Official Microsoft Images** (MUST)
   - Use `mcr.microsoft.com` base images only
   - Keep base images updated with security patches

#### Rationale:

> Source-based builds align with modern .NET 10+ capabilities, reduce maintenance overhead, eliminate Dockerfile complexity, and leverage Azure's optimized build pipelines. This approach follows Microsoft's recommended practices for .NET cloud-native applications and simplifies the deployment process while maintaining security and reliability.

### Version History:

```
Constitution Version 1.2.0 (2025-10-24)
- Added: Principle VI (Cloud Deployment & Container Strategy)
- Modified: Sync Impact Report with template updates
- Impact: AZURE-DEPLOYMENT.md created, GitHub Actions updated, API .csproj modified
```

---

## Task Updates

### Phase 9: Deployment & Release (Expanded)

Added comprehensive deployment task breakdown:

#### T045: Azure Container Apps Configuration ✅ COMPLETED
- **T045a**: Container properties in .csproj ✅
  - `<EnableSdkContainerSupport>true</EnableSdkContainerSupport>`
  - `<ContainerPort>5234</ContainerPort>`
  - `<ContainerPort>7234</ContainerPort>`
  - `<ContainerBaseImage>mcr.microsoft.com/dotnet/aspnet:10.0-preview</ContainerBaseImage>`

- **T045b**: GitHub Actions workflow configuration ✅
  - Updated `.github/workflows/bloodtrackerapi-AutoDeployTrigger-*.yml`
  - Source-based deployment setup

- **T045c**: Source build configuration ✅
  - Set `acrBuild: true`
  - No Dockerfile required per Constitution VI

- **T045d**: Deployment triggers ✅
  - API paths: `src/BloodThinnerTracker.Api/**`
  - Shared: `src/BloodThinnerTracker.Shared/**`
  - Service defaults: `src/BloodThinnerTracker.ServiceDefaults/**`

- **T045e**: Port and ingress configuration ✅
  - `targetPort: 5234`
  - `ingress: external`

- **T045f**: Documentation ✅
  - Created `AZURE-DEPLOYMENT.md`
  - Explains source-build approach and benefits

#### T046-deployment: Infrastructure as Code (NEW)
- **T046a**: Bicep/Terraform templates for Azure resources
- **T046b**: Azure Key Vault for secrets management
- **T046c**: Application Insights for monitoring
- **T046d**: Azure PostgreSQL configuration
- **T046e**: Custom domain and SSL certificates

#### T047: CI/CD Security & Quality Gates (NEW)
- **T047a**: Dependency scanning with Dependabot
- **T047b**: Code coverage enforcement (90% threshold)
- **T047c**: OWASP security scanning
- **T047d**: Automated integration tests
- **T047e**: Production approval gates

#### T048: Blazor Web Deployment (NEW)
- **T048a**: Azure Static Web Apps or App Service
- **T048b**: Custom domain and SSL
- **T048c**: CORS and API configuration
- **T048d**: Health checks and monitoring

#### T049: Mobile App Distribution (NEW)
- **T049a**: Google Play Console setup
- **T049b**: Apple App Store Connect
- **T049c**: App signing and provisioning
- **T049d**: App Center for distribution

#### T050: Production Readiness (NEW)
- **T050a**: Environment variables validation
- **T050b**: Load testing (1000 req/min target)
- **T050c**: Backup and disaster recovery
- **T050d**: Security audit and penetration testing
- **T050e**: Operational runbooks
- **T050f**: Monitoring dashboards and alerts

---

## Implementation Details

### Current Deployment Configuration

#### API Project (.csproj)
```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <UserSecretsId>bloodthinner-api-oauth-secrets-2025</UserSecretsId>
  
  <!-- Container support for Azure Container Apps source builds -->
  <EnableSdkContainerSupport>true</EnableSdkContainerSupport>
  <ContainerBaseImage>mcr.microsoft.com/dotnet/aspnet:10.0-preview</ContainerBaseImage>
  <ContainerPort>5234</ContainerPort>
  <ContainerPort>7234</ContainerPort>
  <ContainerWorkingDirectory>/app</ContainerWorkingDirectory>
</PropertyGroup>
```

#### GitHub Actions Workflow
```yaml
name: Trigger auto deployment for bloodtrackerapi

on:
  push:
    branches: [ feature/blood-thinner-medication-tracker ]
    paths:
    - 'src/BloodThinnerTracker.Api/**'
    - 'src/BloodThinnerTracker.Shared/**'
    - 'src/BloodThinnerTracker.ServiceDefaults/**'
    - '.github/workflows/bloodtrackerapi-AutoDeployTrigger-*.yml'

jobs:
  build-and-deploy:
    steps:
      - name: Build and push container image to registry
        uses: azure/container-apps-deploy-action@v2
        with:
          acrBuild: true  # Source-based build
          appSourcePath: ${{ github.workspace }}/src/BloodThinnerTracker.Api
          containerAppName: bloodtrackerapi
          resourceGroup: FreeNorthEurope
          targetPort: 5234
          ingress: external
```

### Files Modified

| File | Action | Description |
|------|--------|-------------|
| `.specify/memory/constitution.md` | ✏️ Modified | Added Principle VI, updated version to 1.2.0 |
| `specs/feature/.../tasks.md` | ✏️ Modified | Added Phase 9 deployment tasks T045-T050 |
| `src/BloodThinnerTracker.Api/...csproj` | ✏️ Modified | Added container support properties |
| `.github/workflows/bloodtrackerapi-*.yml` | ✏️ Modified | Configured source-based deployment |
| `AZURE-DEPLOYMENT.md` | ➕ Created | Comprehensive deployment guide |

### Files Deleted

| File | Status | Reason |
|------|--------|--------|
| `Dockerfile.api` | 🗑️ Deleted | Not needed for source builds |
| `.dockerignore` | 🗑️ Deleted | Not needed for source builds |
| `docker-compose.api.yml` | 🗑️ Deleted | Not needed for source builds |
| `DOCKER-DEPLOYMENT.md` | 🗑️ Deleted | Replaced with AZURE-DEPLOYMENT.md |
| `DOCKER-CHANGES-SUMMARY.md` | 🗑️ Deleted | Consolidated into this document |

---

## Compliance Checklist

### Constitution Adherence

- ✅ **Principle I (Code Quality)**: Build succeeds with 0 errors
- ✅ **Principle V (Security)**: OIDC authentication, no hardcoded secrets
- ✅ **Principle VI (Deployment)**: Source-based builds, SDK container support
- ⏳ **Principle II (Testing)**: Pending T047d integration tests
- ⏳ **Principle IV (Performance)**: Pending T050b load testing

### Task Completion Status

**Phase 9 Progress**: 6/35 subtasks complete (17%)

- ✅ T045 (API Container Configuration): 6/6 subtasks complete
- ⏳ T046-deployment (IaC): 0/5 subtasks
- ⏳ T047 (CI/CD Security): 0/5 subtasks
- ⏳ T048 (Web Deployment): 0/4 subtasks
- ⏳ T049 (Mobile Distribution): 0/4 subtasks
- ⏳ T050 (Production Readiness): 0/6 subtasks

---

## Benefits of New Approach

### Developer Experience
- ✅ **Simpler**: No Dockerfile maintenance
- ✅ **Faster**: Quicker local development cycle
- ✅ **Modern**: Leverages .NET 10 SDK features
- ✅ **Type-Safe**: All configuration in .csproj

### Operations
- ✅ **Automated**: Azure handles container creation
- ✅ **Optimized**: Oryx applies best practices
- ✅ **Reliable**: Microsoft-maintained build pipeline
- ✅ **Secure**: Regular base image updates

### Compliance
- ✅ **Constitution VI**: Fully compliant
- ✅ **Microsoft Best Practices**: Follows official guidance
- ✅ **Cloud-Native**: Aligns with Azure patterns
- ✅ **Maintainable**: Fewer files, less complexity

---

## Next Steps

### Immediate (T046-deployment)
1. Create Bicep templates for Azure resources
2. Configure Key Vault for secrets
3. Set up Application Insights monitoring
4. Configure production PostgreSQL database

### Short-Term (T047-T048)
1. Add security scanning to CI/CD
2. Implement code coverage gates
3. Deploy Blazor Web application
4. Configure custom domains

### Long-Term (T049-T050)
1. Prepare mobile app distribution channels
2. Complete production readiness checklist
3. Run comprehensive security audit
4. Set up operational monitoring

---

## References

- **Constitution**: `.specify/memory/constitution.md` (v1.2.0)
- **Tasks**: `specs/feature/blood-thinner-medication-tracker/tasks.md`
- **Deployment Guide**: `AZURE-DEPLOYMENT.md`
- **Microsoft Docs**: [.NET SDK Container Support](https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container)
- **Azure Docs**: [Container Apps Source Builds](https://learn.microsoft.com/en-us/azure/container-apps/deploy-source-code)

---

**Document Version**: 1.0  
**Last Updated**: October 24, 2025  
**Status**: ✅ T045 Complete, Constitution Updated, Documentation Finalized

## Exception Handling & Logging (Constitution Addendum)

As part of the project's coding standards and operational safety requirements, the following policy is appended to the constitution and must be followed by all contributors:

1. **No Silent Exception Swallows (MUST)**
  - Catch blocks MUST not be empty nor swallow exceptions silently. Every `catch` must either log the exception or rethrow.
2. **Prefer Specific Exceptions (SHOULD)**
  - Catch specific exception types where possible (`HttpRequestException`, `DbUpdateException`, etc.) and handle them explicitly.
3. **Always Log With Context (MUST)**
  - Use `ILogger<T>` and structured logging. Include the exception object when logging to preserve stack traces and inner exceptions.
  - Example: `_logger.LogError(ex, "Failed while doing X for id={Id}", id);`
4. **Fallbacks Must Be Explicit (MUST)**
  - If code returns a fallback value after an exception, the fallback must be documented and logged as part of the catch handling.
5. **Propagate Unexpected Errors (MUST)**
  - Unexpected exceptions should generally be rethrown after logging so global middleware can handle them and the runtime can capture telemetry.

### Rationale

Silent exception swallowing hides failures, complicates debugging, and can lead to incorrect application behaviour—particularly dangerous in a medical application handling patient data. Logging preserves context and supports incident investigation and audit requirements.

### Enforcement

- Code reviews will flag empty catch blocks or catch-all handlers without logging.
- CI linters (where available) should include rules to detect empty catch blocks. Reviewers should request changes when found.

