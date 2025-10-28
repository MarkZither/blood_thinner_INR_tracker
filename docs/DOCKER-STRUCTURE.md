# Docker Build Structure - Multi-Project Repository

**Date**: October 24, 2025  
**Pattern**: Dockerfile per project, build from root

---

## Directory Structure

```
blood_thinner_INR_tracker/
├── src/
│   ├── BloodThinnerTracker.Api/
│   │   ├── Dockerfile              # ← API Dockerfile here
│   │   ├── Program.cs
│   │   └── BloodThinnerTracker.Api.csproj
│   ├── BloodThinnerTracker.Web/
│   │   ├── Dockerfile              # ← Future: Web Dockerfile here
│   │   └── BloodThinnerTracker.Web.csproj
│   ├── BloodThinnerTracker.Shared/
│   └── BloodThinnerTracker.ServiceDefaults/
├── Directory.Build.props
├── global.json
└── .github/
    └── workflows/
        ├── bloodtrackerapi-containerapp-AutoDeployTrigger.yml
        └── bloodtrackerweb-containerapp-AutoDeployTrigger.yml  # Future
```

---

## Why This Structure?

### ✅ Benefits
1. **One Dockerfile per deployable unit** - Each service has its own build instructions
2. **No naming conflicts** - All files named `Dockerfile` (standard)
3. **Scales to multiple services** - API, Web, Mobile backend, MCP server
4. **Docker best practice** - Dockerfile lives with the code it builds
5. **IDE support** - VS Code/Rider detect Dockerfiles automatically

### ❌ Why NOT in root?
- Multiple projects → multiple Dockerfiles → naming conflicts (Dockerfile.api, Dockerfile.web, etc.)
- Not standard Docker convention
- Harder for tools to discover

---

## Build Commands

### API Project
```bash
# Local build
docker build -f src/BloodThinnerTracker.Api/Dockerfile -t bloodtracker-api:latest .

# Run
docker run -p 5234:5234 -e ASPNETCORE_URLS="http://+:5234" bloodtracker-api:latest
```

### Web Project (Future)
```bash
# Local build
docker build -f src/BloodThinnerTracker.Web/Dockerfile -t bloodtracker-web:latest .

# Run
docker run -p 5000:5000 bloodtracker-web:latest
```

---

## Azure Container Apps Deployment

### GitHub Actions Workflow

For **API**:
```yaml
- name: Build and deploy API
  run: |
    az containerapp up \
      --name bloodtrackerapi \
      --resource-group FreeNorthEurope \
      --source . \
      --context-path src/BloodThinnerTracker.Api \
      --ingress external \
      --target-port 5234
```

For **Web** (Future):
```yaml
- name: Build and deploy Web
  run: |
    az containerapp up \
      --name bloodtrackerweb \
      --resource-group FreeNorthEurope \
      --source . \
      --context-path src/BloodThinnerTracker.Web \
      --ingress external \
      --target-port 5000
```

**Key Parameter**: `--context-path` tells Azure where to find the Dockerfile

---

## Dockerfile Template

Each Dockerfile follows the same pattern:

```dockerfile
# Multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files (relative to workspace root)
COPY ["src/ProjectName/ProjectName.csproj", "ProjectName/"]
COPY ["src/BloodThinnerTracker.Shared/BloodThinnerTracker.Shared.csproj", "Shared/"]
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]

# Restore dependencies
RUN dotnet restore "ProjectName/ProjectName.csproj"

# Copy all source
COPY src/ .

# Build and publish
WORKDIR "/src/ProjectName"
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 5234
ENTRYPOINT ["dotnet", "ProjectName.dll"]
```

---

## File Paths Explained

### Build Context: Repository Root (`.`)
- Why? Need access to `Directory.Build.props`, `global.json`, and all `src/` projects
- Docker can copy any file under the build context

### Dockerfile Location: `src/ProjectName/Dockerfile`
- Specified with `-f` flag: `docker build -f src/ProjectName/Dockerfile .`
- Azure uses `--context-path src/ProjectName`

### COPY Commands: Relative to Build Context
```dockerfile
# ✅ Correct - relative to root
COPY ["src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj", "Api/"]
COPY ["Directory.Build.props", "./"]

# ❌ Wrong - would fail because context is root, not src/
COPY ["BloodThinnerTracker.Api.csproj", "Api/"]
```

---

## Testing Locally

```bash
# From repository root
cd C:\Source\github\blood_thinner_INR_tracker

# Build API
docker build -f src/BloodThinnerTracker.Api/Dockerfile -t bloodtracker-api:test .

# Build Web (future)
docker build -f src/BloodThinnerTracker.Web/Dockerfile -t bloodtracker-web:test .

# Test API
docker run --rm -p 5234:5234 -e ASPNETCORE_URLS="http://+:5234" bloodtracker-api:test
curl http://localhost:5234/health
```

---

## Future Projects

When adding new deployable services:

1. **Create Dockerfile** in `src/ProjectName/Dockerfile`
2. **Follow template** above, adjust project references
3. **Add workflow** in `.github/workflows/projectname-containerapp-AutoDeployTrigger.yml`
4. **Configure triggers** to only deploy when that project changes

Example for MCP Server:
```yaml
# .github/workflows/bloodtrackermcp-containerapp-AutoDeployTrigger.yml
on:
  push:
    paths:
      - 'src/BloodThinnerTracker.Mcp/**'
      - 'src/BloodThinnerTracker.Shared/**'

jobs:
  deploy:
    steps:
      - run: |
          az containerapp up \
            --name bloodtrackermcp \
            --context-path src/BloodThinnerTracker.Mcp \
            --target-port 3000
```

---

## Migration Path

If you have a Dockerfile in the root:

```bash
# Move to project directory
mv Dockerfile.projectname src/ProjectName/Dockerfile

# Update build commands
# FROM: docker build -f Dockerfile.projectname .
# TO:   docker build -f src/ProjectName/Dockerfile .

# Update workflows
# Add: --context-path src/ProjectName
```

---

## Benefits Summary

| Aspect | Root Dockerfiles | Project Dockerfiles |
|--------|-----------------|---------------------|
| Naming | `Dockerfile.api`, `Dockerfile.web` | All named `Dockerfile` ✅ |
| Discovery | Manual specification | Auto-detected by tools ✅ |
| Separation | All builds in one place | Each project owns its build ✅ |
| Scaling | Gets messy with 5+ services | Clean with any number ✅ |
| Convention | Non-standard | Docker best practice ✅ |

---

**Conclusion**: Dockerfiles belong with their projects, build from root.
