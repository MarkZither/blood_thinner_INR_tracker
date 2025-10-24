# GitHub Secrets Configuration for Dockerfile Deployment

## Issue with Current Workflow

The managed registry approach needs the registry URL, but Azure only provides username/password in the auto-generated action.

## Required Secrets

You need to add **ONE** additional secret to GitHub:

### `BLOODTRACKERAPI_REGISTRY_URL`

**How to get it:**

1. Go to Azure Portal
2. Navigate to: **Container Apps** → **bloodtrackerapi**
3. Click on **"Registry"** in the left menu (under Settings)
4. Look for **"Login server"** or **"Registry URL"**
5. It should look like: `bloodtrackerapi.azurecr.io` or similar

**Alternative way (using Azure CLI):**
```bash
az containerapp show \
  --name bloodtrackerapi \
  --resource-group FreeNorthEurope \
  --query properties.configuration.registries[0].server \
  --output tsv
```

## Add Secret to GitHub

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **"New repository secret"**
4. Name: `BLOODTRACKERAPI_REGISTRY_URL`
5. Value: The registry URL from Azure (e.g., `bloodtrackerapi.azurecr.io`)
6. Click **"Add secret"**

## Verify Existing Secrets

Make sure you already have these (Azure should have created them):
- ✅ `BLOODTRACKERAPI_AZURE_CLIENT_ID`
- ✅ `BLOODTRACKERAPI_AZURE_TENANT_ID`
- ✅ `BLOODTRACKERAPI_AZURE_SUBSCRIPTION_ID`
- ✅ `BLOODTRACKERAPI_REGISTRY_USERNAME`
- ✅ `BLOODTRACKERAPI_REGISTRY_PASSWORD`
- ⚠️ `BLOODTRACKERAPI_REGISTRY_URL` **(ADD THIS ONE)**

## Why This Change?

The original Azure-generated workflow tried to use Oryx source builds, which:
- ❌ Doesn't support .NET 10 RC2
- ❌ Gets confused by multiple projects in workspace

The new workflow:
- ✅ Uses Docker to build the Dockerfile.api
- ✅ Pushes to the managed registry
- ✅ Deploys the pre-built image
- ✅ Avoids Oryx completely

## After Adding the Secret

Push your changes and the workflow should work! 🚀
