# Development Setup Guide

## Quick Start (5 minutes)

### 1. Clone Repository

```bash
git clone https://github.com/MarkZither/blood_thinner_INR_tracker.git
cd blood_thinner_INR_tracker
```

### 2. Run Setup Script

The setup script creates local configuration files from templates:

```powershell
# Interactive mode - prompts for OAuth credentials
.\tools\scripts\setup-dev.ps1

# Or provide credentials directly
.\tools\scripts\setup-dev.ps1 -AzureClientId "your-id" -GoogleClientId "your-id"
```

### 3. Start Development Stack

Press **F5** in VS Code and select from the dropdown:
- **📱 Mobile + Backend (Real Services)** - Full mobile + API stack
- **🚀 Full Stack (API + Web)** - API + Blazor web app
- **Launch MAUI (Windows)** - Mobile app with mock services

## Architecture Overview

```
┌─────────────────────────────────────────┐
│  Repository Root (committed to git)     │
├─────────────────────────────────────────┤
│ .vscode/launch.json.template ✅ Public │
│   ├── All non-secret configs            │
│   └── ${REPLACE_WITH_*} placeholders    │
│                                         │
│ tools/scripts/setup-dev.ps1 ✅ Public  │
│   └── Generates local files from        │
│       templates + adds secrets          │
└─────────────────────────────────────────┘
                    ↓
                 Setup Script
                    ↓
┌─────────────────────────────────────────┐
│  Local Machine (in .gitignore)          │
├─────────────────────────────────────────┤
│ .vscode/launch.json ⚠️ PRIVATE         │
│   ├── Contains OAuth Client IDs         │
│   └── Generated from template           │
│                                         │
│ ~/.dotnet/user-secrets/ ⚠️ PRIVATE     │
│   └── Encrypted secrets storage         │
└─────────────────────────────────────────┘
```

## Security Model

### ✅ Safe to Commit
- `*.template` files (templates with placeholders)
- `setup-dev.ps1` (setup automation)
- Generic configuration (non-secret values)

### ⚠️ Never Commit
- `launch.json` (contains OAuth secrets)
- `tasks.json` (if modified with secrets)
- `.env` files (environment variables with secrets)
- `appsettings.*.local.json` (local overrides with secrets)

### Entry in .gitignore
```gitignore
# VSCode configs with potential secrets
.vscode/launch.json
.vscode/tasks.json
.vscode/settings.json

# Local environment files
.env
.env.local
appsettings.*.local.json

# User secrets (stored encrypted locally)
# .dotnet/user-secrets handled by OS
```

## Getting OAuth Credentials

### Azure AD
1. Go to [Azure Portal](https://portal.azure.com)
2. Select **Azure Active Directory** → **App Registrations**
3. Click **+ New registration**
4. Fill in:
   - Name: `Blood Thinner Tracker - Dev`
   - Supported account types: `Any Azure AD directory`
   - Redirect URI: `https://localhost:7235/oauth/callback`
5. Copy **Application (client) ID**
6. Go to **Certificates & Secrets** → **+ New client secret**
7. Copy secret value

### Google OAuth
1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create new project or select existing
3. Enable **Google+ API**
4. Go to **Credentials** → **+ Create Credentials** → **OAuth 2.0 Client IDs**
5. Configure consent screen (user type: External)
6. Application type: **Web Application**
7. Add redirect URIs:
   - `https://localhost:7235/oauth/callback`
   - `https://localhost:7049/oauth/callback` (API direct)
8. Copy **Client ID**

## File Organization

### Configuration Files
```
.vscode/
├── launch.json.template ✅ Committed
├── launch.json          ⚠️ .gitignore'd
├── tasks.json.template  ✅ Committed (if needed)
└── tasks.json           ⚠️ .gitignore'd (if modified)

src/BloodThinnerTracker.Api/
├── appsettings.json                  ✅ Committed
├── appsettings.Development.json      ✅ Committed
├── appsettings.Development.local.json ⚠️ .gitignore'd
├── appsettings.Production.json       ✅ Committed
└── secrets.json                      ⚠️ Never committed

tools/scripts/
└── setup-dev.ps1 ✅ Committed
```

## Updating Configuration

### When to Update `.vscode/launch.json.template`

If you add new debug configurations or non-secret settings:

1. Update the `.template` file (shared with team)
2. Update your local `launch.json` (personal copy)
3. Commit only the `.template` file to git

```bash
# Good: Update template and commit
git add .vscode/launch.json.template
git commit -m "feat: Add Android device debug config to template"

# Bad: Update actual file and commit
git add .vscode/launch.json  # This will fail - it's in .gitignore
```

### When to Update `.vscode/launch.json`

Only update your local copy:
1. Add/modify OAuth Client IDs
2. Change local ports
3. Add personal debugging settings
4. **Do not commit** - it's in `.gitignore`

If you want others to use new settings:
1. Update the `.template` file
2. Run setup script again: `.\tools\scripts\setup-dev.ps1 -Force`

## Troubleshooting

### "Certificate not found" Error
**Cause**: HTTPS certificate not configured  
**Solution**: Using localhost in development, certificates should auto-generate

### "OAuth credentials not working"
**Cause**: Client IDs in launch.json not set  
**Solution**: 
```powershell
.\tools\scripts\setup-dev.ps1 -AzureClientId "your-id" -GoogleClientId "your-id"
```

### "Port already in use"
**Cause**: Another process using port 7049, 7235, etc.  
**Solution**: Kill the process or modify ASPNETCORE_URLS in launch.json

```powershell
# Find and kill process on port 7049
netstat -ano | Select-String ":7049"
taskkill /PID <PID> /F
```

### "AppHost fails to start"
**Cause**: Docker daemon not running or ports in use  
**Solution**: 
```powershell
# Ensure Docker is running
docker ps

# Or run without Docker
dotnet run --project src/BloodThinnerTracker.Api
```

## Development Workflow

### Daily Workflow

```powershell
# 1. Start development stack (F5 in VS Code)
# Select "📱 Mobile + Backend (Real Services)"

# 2. Get OAuth token for API testing
# Open http://localhost:5174/oauth-test.html

# 3. Test API endpoints in Scalar
# https://localhost:7049/scalar/v1

# 4. Debug mobile app in VS Code
# Breakpoints work in integrated terminal
```

### Running Tests

```bash
# Unit tests
dotnet test

# Integration tests
dotnet test tests/Integration/

# Code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Building for Release

```bash
# Clean build
dotnet clean && dotnet build -c Release

# Run release build
dotnet run -c Release --project src/BloodThinnerTracker.Api
```

## CI/CD Integration

### GitHub Actions

For production deployments, use GitHub Secrets:

```yaml
# .github/workflows/deploy.yml
env:
  AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
  GOOGLE_CLIENT_ID: ${{ secrets.GOOGLE_CLIENT_ID }}
```

Never store secrets in:
- ❌ launch.json (committed to git)
- ❌ appsettings.json (committed to git)
- ✅ GitHub Secrets (encrypted by GitHub)
- ✅ Azure KeyVault (integrated with ServiceDefaults)

## Additional Resources

- [OAuth Testing Guide](./OAUTH_TESTING_GUIDE.md)
- [Aspire Implementation](./ASPIRE_IMPLEMENTATION.md)
- [Constitution - Configuration](./CONSTITUTION.md#configuration)

## Support

For issues or questions:
1. Check [troubleshooting](#troubleshooting) above
2. Review [OAuth Testing Guide](./OAUTH_TESTING_GUIDE.md)
3. Open GitHub issue with setup error logs
