# Quickstart Guide: Docker Deployment Infrastructure

**Version**: 1.0.0  
**Target Audience**: Developers setting up local deployment infrastructure

---

## 🚀 Quick Setup (5 minutes)

### 1. Install Prerequisites

**Required Software**:
```bash
# .NET 10 SDK
winget install Microsoft.DotNet.SDK.10
# Docker Desktop
winget install Docker.DockerDesktop
# Azure CLI
winget install Microsoft.AzureCLI
# Visual Studio 2022 or VS Code
winget install Microsoft.VisualStudio.2022.Community
# OR
winget install Microsoft.VisualStudioCode
```

**Verify Installation**:
```bash
dotnet --version          # Should show 10.0.x
docker --version          # Should show Docker version info
az --version              # Should show Azure CLI version info
git --version            # Should show Git version info
```

---

### 2. Clone and Setup Repository

```bash
git clone https://github.com/your-org/blood-thinner-inr-tracker.git
cd blood-thinner-inr-tracker
```

---

### 3. Local Docker Compose Startup

```bash
docker-compose up --build
```

- API, Web, and Database containers will start
- Migrations run automatically
- Hot reload enabled for code changes

---

### 4. Azure Container Apps Deployment

```bash
az login
az containerapp up --name bloodthinnertracker-api --resource-group <your-rg> --source ./src/BloodThinnerTracker.Api
az containerapp up --name bloodthinnertracker-web --resource-group <your-rg> --source ./src/BloodThinnerTracker.Web
```

- Uses source-based deployment (no Dockerfile required unless multi-stage)
- Connection strings and secrets managed via Azure Key Vault
- Health checks configured for all services

---

### 5. CI/CD Pipeline

- GitHub Actions workflow in `.github/workflows/deploy.yml`
- On push to main, triggers build and deploy to Azure
- Secrets managed via OIDC and Azure Key Vault

---

### 6. Troubleshooting

- Check container logs: `docker logs <container-name>`
- Check Azure deployment status: `az containerapp show --name <app-name> --resource-group <your-rg>`
- Health check endpoint: `/health` on API/Web

---

**For full entity definitions and business logic, see canonical data model in archive.**
