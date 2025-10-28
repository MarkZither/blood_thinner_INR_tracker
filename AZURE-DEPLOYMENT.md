# Azure Container Apps - Deployment with Dockerfile

## Overview

Configured the Blood Thinner Tracker API for **Dockerfile-based deployment** to Azure Container Apps. This approach is used because:
- ✅ Supports **.NET 10 RC2** (preview) which Azure's Oryx buildpack doesn't support yet
- ✅ Uses **managed registry** (FREE - included with Container Apps, no ACR costs)
- ✅ Will switch to source-based builds after .NET 10 GA (November 2025)

## Deployment Strategy

### Current: Dockerfile Build (.NET 10 RC2)
- **Why**: Azure Oryx doesn't support .NET 10 RC2 yet
- **Cost**: FREE (uses Container Apps managed registry)
- **Dockerfile**: `Dockerfile.api` with official Microsoft .NET 10 RC2 images

### Future: Source-Based Build (.NET 10 GA)
- **When**: After .NET 10 GA release (November 2025)
- **Action**: Remove `dockerfilePath`, use Oryx buildpack
- **Benefit**: No Dockerfile maintenance

## What Changed

### 1. Created `Dockerfile.api`

Multi-stage Dockerfile using official .NET 10 RC2 images:

```dockerfile
# Build with .NET 10 RC2 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0-rc AS build

# Runtime with .NET 10 RC2 ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:10.0-rc AS runtime

# Security: Runs as non-root user (appuser)
# Ports: 5234 (HTTP), 7234 (HTTPS)
# Health check: /health endpoint every 30s
```

### 2. Updated GitHub Action

**File**: `.github/workflows/bloodtrackerapi-containerapp-AutoDeployTrigger.yml`

**Key configuration**:
```yaml
# Build from Dockerfile (not source)
appSourcePath: ${{ github.workspace }}
dockerfilePath: Dockerfile.api

# Use managed registry (FREE with Container Apps)
registryUrl:  # Empty = managed registry
registryUsername: ${{ secrets.BLOODTRACKERAPI_REGISTRY_USERNAME }}
registryPassword: ${{ secrets.BLOODTRACKERAPI_REGISTRY_PASSWORD }}
imageToBuild: default/[parameters('containerAppName')]:${{ github.sha }}

# Expose API on port 5234
targetPort: 5234
ingress: external
```

## How It Works

1. **Push to branch** → Triggers GitHub Action
2. **GitHub Action** → Checks out code, logs into Azure
3. **Dockerfile Build** → Builds .NET 10 RC2 container using Dockerfile.api
4. **Managed Registry** → Pushes to FREE registry included with Container Apps
5. **Container App** → Deploys and runs on port 5234

## Cost Breakdown

| Component | Service | Cost |
|-----------|---------|------|
| Container App | Azure Container Apps Free tier | **FREE** (first 180,000 vCPU-seconds + 360,000 GiB-seconds/month) |
| Container Registry | Managed Registry | **FREE** (included with Container Apps) |
| Build Minutes | GitHub Actions | **FREE** (2,000 minutes/month on free tier) |
| **Total** | | **$0/month** ✅ |

No ACR costs, no additional charges!

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
- `BLOODTRACKERAPI_AZURE_CLIENT_ID` - Azure AD app client ID for OIDC authentication
- `BLOODTRACKERAPI_AZURE_TENANT_ID` - Azure AD tenant ID
- `BLOODTRACKERAPI_AZURE_SUBSCRIPTION_ID` - Azure subscription ID
- `BLOODTRACKERAPI_ACR_NAME` - Azure Container Registry name (e.g., "bloodtrackeracr")
- `BLOODTRACKERAPI_REGISTRY_USERNAME` - Container registry username (or service principal ID)
- `BLOODTRACKERAPI_REGISTRY_PASSWORD` - Container registry password (or service principal secret)

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
