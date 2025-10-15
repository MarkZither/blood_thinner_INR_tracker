# Quickstart Guide: Blood Thinner Medication & INR Tracker

**Version**: 1.0.0  
**Target Audience**: Developers setting up local development environment  
**Prerequisites**: Basic knowledge of .NET development

---

## 🚀 Quick Setup (5 minutes)

### 1. Install Prerequisites

**Required Software**:
```bash
# .NET 8 SDK (or later)
winget install Microsoft.DotNet.SDK.8

# Docker Desktop (for database and services)
winget install Docker.DockerDesktop

# Visual Studio 2022 or VS Code
winget install Microsoft.VisualStudio.2022.Community
# OR
winget install Microsoft.VisualStudioCode
```

**Verify Installation**:
```bash
dotnet --version          # Should show 8.0.0 or later
docker --version          # Should show Docker version info
git --version            # Should show Git version info
```

---

### 2. Clone and Setup Repository

```bash
# Clone the repository
git clone https://github.com/your-org/blood-thinner-inr-tracker.git
cd blood-thinner-inr-tracker

# Install .NET Aspire workload
dotnet workload install aspire

# Restore dependencies
dotnet restore

# Install development certificates
dotnet dev-certs https --trust
```

---

### 3. Database Setup

**Option A: Docker (Recommended)**
```bash
# Start PostgreSQL and Redis containers
docker-compose up -d database redis

# Verify containers are running
docker-compose ps
```

**Option B: Local SQLite (Development Only)**
```bash
# SQLite databases will be created automatically in src/BloodThinnerTracker.Api/Data/
# No additional setup required
```

---

### 4. Run the Application

**Start with .NET Aspire (Full Stack)**:
```bash
# Navigate to the App Host project
cd src/BloodThinnerTracker.AppHost

# Run the orchestrated solution
dotnet run

# 🎉 Open browser to http://localhost:5000 (Aspire Dashboard)
# 📱 Web app available at http://localhost:5001
# 🔌 API endpoints at http://localhost:5002/api/v1
```

**Alternative: Run Individual Services**:
```bash
# Terminal 1: API Backend
cd src/BloodThinnerTracker.Api
dotnet run

# Terminal 2: Web Frontend  
cd src/BloodThinnerTracker.Web
dotnet run

# Terminal 3: Mobile (Android Emulator)
cd src/BloodThinnerTracker.Mobile
dotnet build -f net8.0-android
dotnet run -f net8.0-android
```

---

## 📱 Platform-Specific Setup

### Mobile Development (MAUI)

**Android Setup**:
```bash
# Install Android SDK via Visual Studio Installer or:
dotnet workload install android

# Create and start Android emulator
# Via Android Studio or Visual Studio
```

**iOS Setup** (macOS only):
```bash
# Install iOS workload
dotnet workload install ios

# Requires Xcode installation
xcode-select --install
```

**Windows Setup**:
```bash
# Install Windows App SDK
dotnet workload install maccatalyst windows
```

---

### Console Tool Development

```bash
# Build and install as global tool (local development)
cd src/BloodThinnerTracker.Console
dotnet pack
dotnet tool install --global --add-source ./nupkg BloodThinnerTracker.Tool

# Test installation
bloodthinner --version
bloodthinner auth login
bloodthinner medications list
```

---

### MCP Server Development

```bash
# Run MCP server locally
cd src/BloodThinnerTracker.Mcp
dotnet run

# Test MCP server endpoints
curl http://localhost:8080/mcp/health
curl -X POST http://localhost:8080/mcp/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"listResources","id":1}'
```

---

## 🔐 Authentication Setup

### OAuth Provider Configuration

**Azure AD Setup**:
1. Navigate to [Azure Portal](https://portal.azure.com)
2. Register new application in Azure Active Directory
3. Configure redirect URIs:
   - Web: `https://localhost:5001/signin-oidc`
   - Mobile: `msauth.{bundle-id}://auth` (iOS), `https://{package-name}.com/auth` (Android)
4. Copy Application ID and create client secret

**Google OAuth Setup**:
1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create new project or select existing
3. Enable Google+ API
4. Create OAuth 2.0 credentials
5. Configure authorized redirect URIs

**Update Configuration**:
```bash
# Copy template configuration
cp src/BloodThinnerTracker.Api/appsettings.Development.template.json \
   src/BloodThinnerTracker.Api/appsettings.Development.json

# Edit with your OAuth credentials
nano src/BloodThinnerTracker.Api/appsettings.Development.json
```

**Configuration Template**:
```json
{
  "Authentication": {
    "AzureAd": {
      "Instance": "https://login.microsoftonline.com/",
      "TenantId": "your-tenant-id",
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    },
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BloodThinnerTracker;Trusted_Connection=true;",
    "Redis": "localhost:6379"
  }
}
```

---

## 🧪 Testing Setup

### Unit Tests
```bash
# Run all unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
reportgenerator -reports:"**/*.cobertura.xml" -targetdir:coverage -reporttypes:Html
```

### Integration Tests
```bash
# Start test database
docker-compose -f docker-compose.test.yml up -d

# Run integration tests
dotnet test Tests/BloodThinnerTracker.Integration.Tests/

# Cleanup test environment
docker-compose -f docker-compose.test.yml down
```

### Mobile Testing (Playwright)
```bash
# Install Playwright browsers
pwsh bin/Debug/net8.0/playwright.ps1 install

# Run mobile UI tests
dotnet test Tests/BloodThinnerTracker.Mobile.Tests/
```

---

## 📊 Development Tools

### API Documentation (Swagger/Scalar)
- **Swagger UI**: http://localhost:5002/swagger
- **Scalar UI**: http://localhost:5002/scalar/v1
- **OpenAPI Spec**: http://localhost:5002/openapi/v1.json

### Database Tools
```bash
# Entity Framework migrations
dotnet ef migrations add InitialCreate -p src/BloodThinnerTracker.Api
dotnet ef database update -p src/BloodThinnerTracker.Api

# View database schema
dotnet ef dbcontext info -p src/BloodThinnerTracker.Api
```

### Observability Dashboard
- **Aspire Dashboard**: http://localhost:15000
- **Structured Logs**: Available in dashboard
- **Metrics & Traces**: Real-time monitoring
- **Health Checks**: Service status overview

---

## 🔧 Troubleshooting

### Common Issues

**Port Conflicts**:
```bash
# Check if ports are in use
netstat -an | findstr :5001  # Windows
lsof -i :5001                # macOS/Linux

# Kill processes using ports
taskkill /F /PID <pid>       # Windows
kill -9 <pid>               # macOS/Linux
```

**Certificate Issues**:
```bash
# Recreate development certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust

# For mobile development
dotnet dev-certs https -ep localhost.pfx -p password
```

**Database Connection Issues**:
```bash
# Reset local database
docker-compose down -v
docker-compose up -d database

# Apply migrations
dotnet ef database update -p src/BloodThinnerTracker.Api
```

**Build Errors**:
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build

# Clear NuGet cache if needed
dotnet nuget locals all --clear
```

### Performance Optimization

**Development Mode**:
```bash
# Disable unnecessary services for faster startup
export DISABLE_REDIS=true
export DISABLE_TELEMETRY=true
dotnet run --configuration Debug
```

**Memory Usage**:
```bash
# Monitor memory usage
dotnet-counters monitor --process-id <pid>

# Garbage collection insights
dotnet-gcdump collect -p <pid>
```

---

## 📚 Next Steps

### 1. Explore the Codebase
- 📄 **Feature Spec**: `/feature/spec.md` - User stories and requirements  
- 🏗️ **Architecture**: `/specs/feature/blood-thinner-medication-tracker/research.md`
- 📊 **Data Model**: `/specs/feature/blood-thinner-medication-tracker/data-model.md`
- 🔌 **API Contracts**: `/specs/feature/blood-thinner-medication-tracker/contracts/`

### 2. Development Workflow
```bash
# Create feature branch
git checkout -b feature/medication-reminders

# Make changes and test
dotnet test
dotnet run

# Commit with conventional commits
git commit -m "feat(medication): add reminder notification system"

# Push and create pull request
git push origin feature/medication-reminders
```

### 3. Contributing Guidelines
- Follow [conventional commits](https://conventionalcommits.org/) for commit messages
- Maintain 90% code coverage for new features
- Run `dotnet format` before committing
- Update API documentation for endpoint changes
- Add integration tests for new user workflows

### 4. Deployment
```bash
# Build production images
docker build -f src/BloodThinnerTracker.Api/Dockerfile -t bloodthinner-api .
docker build -f src/BloodThinnerTracker.Web/Dockerfile -t bloodthinner-web .

# Push to container registry
docker push your-registry/bloodthinner-api:latest
docker push your-registry/bloodthinner-web:latest
```

---

## 🆘 Getting Help

- 📖 **Documentation**: Check `/docs/` directory for detailed guides
- 🐛 **Issues**: Report bugs via GitHub Issues
- 💬 **Discussions**: Use GitHub Discussions for questions
- 🔐 **Security**: Email security@yourorg.com for security concerns

**Quick Commands Reference**:
```bash
# Health check all services
curl http://localhost:5002/health

# Reset development environment  
docker-compose down -v && docker-compose up -d

# Run full test suite
dotnet test --configuration Release --collect:"XPlat Code Coverage"

# Format code
dotnet format --verbosity diagnostic

# Build all platforms
dotnet build --configuration Release
```

---

**🎉 You're ready to start developing!** The application should now be running with full functionality. Visit the Aspire Dashboard at http://localhost:15000 to monitor all services.