# Azure Container Apps - Source-Based Deployment

## Overview

Configured the Blood Thinner Tracker API for **source-based deployment** to Azure Container Apps. No Dockerfile needed! Azure will build the container from the .NET project using Oryx/buildpacks.

## What Changed

### 1. Updated `BloodThinnerTracker.Api.csproj`

Added container support properties that Azure will use:

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

### 2. Updated GitHub Action

**File**: `.github/workflows/bloodtrackerapi-AutoDeployTrigger-35ca4c93-c6e4-4e72-9d04-ae57bfcba829.yml`

**Key changes**:
```yaml
# ✅ Source-based build (no Dockerfile)
acrBuild: true

# ✅ Points to API project directory
appSourcePath: ${{ github.workspace }}/src/BloodThinnerTracker.Api

# ✅ Removed dockerfilePath (not needed)

# ✅ Correct port configuration
targetPort: 5234
ingress: external
```

### 3. Removed Docker Files

Deleted the following (no longer needed):
- ❌ `Dockerfile.api`
- ❌ `.dockerignore`
- ❌ `docker-compose.api.yml`
- ❌ `DOCKER-DEPLOYMENT.md`
- ❌ `DOCKER-CHANGES-SUMMARY.md`

## How It Works

1. **Push to branch** → Triggers GitHub Action
2. **GitHub Action** → Checks out code, logs into Azure
3. **Azure Container Apps** → Detects .NET 10 project
4. **Oryx buildpack** → Builds container from source
5. **Container Registry** → Stores built image
6. **Container App** → Deploys and runs on port 5234

## Port Configuration

- **Port 5234**: HTTP (Azure Container Apps uses this internally)
- **Port 7234**: HTTPS (configured but Azure handles TLS at ingress)

Azure Container Apps will:
- Accept external HTTPS traffic
- Route to HTTP port 5234 in the container
- Handle SSL/TLS termination automatically

## What Gets Built

The source build includes:
- ✅ `BloodThinnerTracker.Api` project
- ✅ `BloodThinnerTracker.Shared` (referenced project)
- ✅ `BloodThinnerTracker.ServiceDefaults` (referenced project)
- ✅ All NuGet packages from `.csproj`

Azure's Oryx buildpack automatically:
- Detects .NET 10
- Restores NuGet packages
- Builds the project
- Creates optimized container
- No manual Dockerfile needed!

## Deployment Triggers

The action triggers when you push changes to:
- `src/BloodThinnerTracker.Api/**` - API source code
- `src/BloodThinnerTracker.Shared/**` - Shared models
- `src/BloodThinnerTracker.ServiceDefaults/**` - Service defaults
- The workflow file itself

## Required GitHub Secrets

Ensure these are configured in your repository:
- `BLOODTRACKERAPI_AZURE_CLIENT_ID`
- `BLOODTRACKERAPI_AZURE_TENANT_ID`
- `BLOODTRACKERAPI_AZURE_SUBSCRIPTION_ID`
- `BLOODTRACKERAPI_REGISTRY_USERNAME`
- `BLOODTRACKERAPI_REGISTRY_PASSWORD`

## Testing Locally

You can still test locally without Docker:

```bash
cd src/BloodThinnerTracker.Api
dotnet run
```

Access at:
- HTTP: http://localhost:5234
- HTTPS: https://localhost:7234
- API Docs: https://localhost:7234/scalar/v1

## Benefits of Source-Based Builds

✅ **No Dockerfile maintenance** - Azure handles container creation
✅ **Modern .NET 10 approach** - Uses SDK container support
✅ **Simpler configuration** - Everything in `.csproj`
✅ **Automatic optimizations** - Oryx buildpack applies best practices
✅ **Faster iteration** - No need to test Docker builds locally
✅ **Less complexity** - Fewer files to manage

## Deployment Workflow

1. Make changes to API code
2. Commit and push to `feature/blood-thinner-medication-tracker`
3. GitHub Action automatically triggers
4. Azure builds container from source
5. Container App updates with new version
6. Access API at your Container App URL

## Troubleshooting

### Build fails in Azure
- Check GitHub Actions logs
- Verify all project references are correct
- Ensure `.csproj` has no syntax errors

### Container won't start
- Check Azure Container Apps logs
- Verify port 5234 is correctly exposed
- Check environment variables are set

### API not accessible
- Verify ingress is set to `external`
- Check targetPort is 5234
- Verify Azure Container App status

## Next Steps

1. ✅ Verify build succeeds locally (`dotnet build`)
2. Push changes to trigger deployment
3. Monitor GitHub Actions for deployment status
4. Test deployed API endpoint
5. Configure production environment variables in Azure

---

**Deployment Type**: Source-based (Oryx buildpack)
**Build Tool**: Azure Container Registry + Oryx
**Base Image**: `mcr.microsoft.com/dotnet/aspnet:10.0-preview`
**Port**: 5234 (HTTP)
**Date**: October 24, 2025
