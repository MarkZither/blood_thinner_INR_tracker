# Test Azure Container Apps Deployment Locally

## Prerequisites

1. **Azure CLI installed** (you should have this already)
2. **Logged into Azure**: 
   ```powershell
   az login
   ```
3. **Docker Desktop running** (for local Docker testing)

---

## Option 1: Test Docker Build Locally (Recommended First)

Before deploying to Azure, test the Dockerfile builds correctly:

```powershell
# Navigate to project root
cd C:\Source\github\blood_thinner_INR_tracker

# Build the Docker image
docker build -f Dockerfile.api -t bloodtracker-api:test .

# Run it locally
docker run -p 5234:5234 -e ASPNETCORE_ENVIRONMENT=Development bloodtracker-api:test

# Test the API (in another terminal)
curl http://localhost:5234/health
curl http://localhost:5234/api/inr
```

**Expected Results:**
- ✅ Build completes without errors
- ✅ Container starts successfully
- ✅ `/health` returns 200 OK
- ✅ `/api/inr` returns 401 Unauthorized (requires auth - that's correct!)

---

## Option 2: Deploy to Azure from Local Machine

This is **exactly** what the GitHub Action does:

```powershell
# Make sure you're logged in
az login

# Set the subscription (if you have multiple)
az account set --subscription "Your Subscription Name"

# Deploy from your local machine
az containerapp up `
  --name bloodtrackerapi `
  --resource-group FreeNorthEurope `
  --location northeurope `
  --source . `
  --dockerfile Dockerfile.api `
  --target-port 5234 `
  --ingress external `
  --env-vars ASPNETCORE_ENVIRONMENT=Production
```

**What this does:**
1. Builds your Dockerfile.api in Azure (cloud build)
2. Pushes to Azure's managed registry
3. Updates your Container App with the new image
4. Returns the URL of your deployed app

**Time:** ~3-5 minutes for first deployment, ~1-2 minutes for updates

---

## Option 3: Test Just the Build (No Deploy)

If you want to test the Azure cloud build without deploying:

```powershell
# This simulates what happens in GitHub Actions
az acr build `
  --registry bloodtrackerapi `
  --resource-group FreeNorthEurope `
  --image bloodtrackerapi:test `
  --file Dockerfile.api `
  .
```

**Note:** This requires you have an ACR. If using managed registry, skip this and use Option 1 or 2.

---

## Troubleshooting Local Docker Build

### Issue: "Cannot find project files"
```powershell
# Make sure you're in the repo root
cd C:\Source\github\blood_thinner_INR_tracker
pwd  # Should show the root directory with Dockerfile.api
```

### Issue: "Port already in use"
```powershell
# Find what's using port 5234
netstat -ano | findstr :5234

# Kill the process (replace <PID> with actual number)
taskkill /PID <PID> /F
```

### Issue: Docker build fails with "dotnet restore" errors
```powershell
# Clear local Docker cache
docker builder prune -a

# Try build again
docker build -f Dockerfile.api -t bloodtracker-api:test . --no-cache
```

---

## Comparing Local vs GitHub Actions

| Step | Local Command | GitHub Actions |
|------|---------------|----------------|
| Login | `az login` | OIDC authentication (automatic) |
| Build | `docker build` or `az containerapp up` | `az containerapp up` in workflow |
| Deploy | `az containerapp up` | Same command |
| Result | Same deployed app | Same deployed app |

**Both produce identical results!** ✅

---

## Quick Test Before Pushing

**Recommended workflow:**

```powershell
# 1. Test Docker build locally (fast, ~2 minutes)
docker build -f Dockerfile.api -t bloodtracker-api:test .

# 2. If build succeeds, test locally
docker run -p 5234:5234 -e ASPNETCORE_ENVIRONMENT=Development bloodtracker-api:test

# 3. Test the health endpoint
curl http://localhost:5234/health

# 4. If everything works, push to GitHub
git add .
git commit -m "Add Dockerfile deployment"
git push origin feature/blood-thinner-medication-tracker
```

This way you catch build errors locally before waiting for GitHub Actions.

---

## Cost Note

- ✅ **Local Docker testing**: FREE (uses your machine)
- ✅ **`az containerapp up`**: FREE (uses Container Apps free tier + managed registry)
- ✅ **GitHub Actions**: FREE (public repos get unlimited minutes)

**Total cost: $0/month** 🎉

---

## Next Steps After Successful Local Test

1. ✅ Local Docker build works
2. ✅ Container runs locally
3. ✅ Health check responds
4. Push to GitHub and watch the same process happen in the cloud
5. Your API will be live at: `https://bloodtrackerapi.{hash}.northeurope.azurecontainerapps.io`
