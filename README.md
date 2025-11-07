# Blood Thinner Medication & INR Tracker

A focused app built using spec-driven development to help users track blood thinner medication and INR test results.

## Quick Start for Developers

### 1. Get a JWT Token (30 seconds)

```bash
# Start the API
dotnet run --project src/BloodThinnerTracker.Api

# Open browser to OAuth test page
# http://localhost:5000/oauth-test.html

# Click "Login with Google" or "Login with Azure AD"
# Copy the JWT token
# Paste into Scalar UI (http://localhost:5000/scalar/v1)
```

**📖 Full Guide**: [docs/QUICK_START_OAUTH.md](docs/QUICK_START_OAUTH.md)

### 2. First-Time OAuth Setup

Configure OAuth credentials (one-time):
- **Google**: [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
- **Azure AD**: [Azure Portal](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade)

**📖 Configuration Guide**: [docs/OAUTH_TESTING_GUIDE.md](docs/OAUTH_TESTING_GUIDE.md)

### 3. Test Protected Endpoints

Use your JWT token in Scalar, cURL, or Postman to test API endpoints.

**📖 Testing Guide**: [docs/AUTHENTICATION_TESTING_GUIDE.md](docs/AUTHENTICATION_TESTING_GUIDE.md)

---

## Technology Stack

- **Framework**: .NET 10 (C# 13) - LTS version
- **Backend**: ASP.NET Core Web API
- **Mobile**: .NET MAUI (iOS/Android)
- **Web**: Blazor Server/WebAssembly
- **Orchestration**: .NET Aspire
- **Database**: Entity Framework Core with SQLite (local) + PostgreSQL (cloud)
- **Authentication**: OAuth 2.0 (Google + Azure AD) with JWT tokens
- **API Docs**: Scalar API Reference
- **Real-time**: SignalR for cross-device sync

---

## Documentation

### Getting Started
- **Quick Start**: [docs/QUICK_START_OAUTH.md](docs/QUICK_START_OAUTH.md) - Get JWT token in 30 seconds
- **OAuth Testing**: [docs/OAUTH_TESTING_GUIDE.md](docs/OAUTH_TESTING_GUIDE.md) - Comprehensive OAuth setup
- **Authentication**: [docs/AUTHENTICATION_TESTING_GUIDE.md](docs/AUTHENTICATION_TESTING_GUIDE.md) - Testing guide

### Architecture & Planning
- **PWA & WebAssembly**: [docs/PWA_WEBASSEMBLY_QUICK_REFERENCE.md](docs/PWA_WEBASSEMBLY_QUICK_REFERENCE.md) - Quick reference for PWA/WASM support
  - [Investigation](docs/PWA_WEBASSEMBLY_INVESTIGATION.md) - Research and architecture analysis
  - [Implementation Guide](docs/PWA_WEBASSEMBLY_IMPLEMENTATION_GUIDE.md) - Step-by-step instructions
  - [Comparison](docs/PWA_WEBASSEMBLY_COMPARISON.md) - Decision matrix and options

### Reference
- **API Reference**: http://localhost:5000/scalar/v1 (when API is running)
- **Task Specifications**: [specs/feature/blood-thinner-medication-tracker/](specs/feature/blood-thinner-medication-tracker/)

---

## Project Structure

```
blood_thinner_INR_tracker/
├── src/
│   ├── BloodThinnerTracker.Api/          # ASP.NET Core Web API
│   ├── BloodThinnerTracker.Mobile/       # .NET MAUI app
│   ├── BloodThinnerTracker.Web/          # Blazor app
│   ├── BloodThinnerTracker.Cli/          # Console tool
│   ├── BloodThinnerTracker.Mcp/          # MCP Server
│   ├── BloodThinnerTracker.AppHost/      # .NET Aspire orchestration
│   └── BloodThinnerTracker.Shared/       # Shared models
├── tests/                                # Unit and integration tests
├── docs/                                 # Documentation
└── specs/                                # Feature specifications
```

---

## Development

### Prerequisites

- .NET 10 SDK (10.0.x or later)
- Visual Studio 2025 or Visual Studio Code with C# Dev Kit
- Git
- Docker Desktop (for PostgreSQL container - used by Aspire orchestration)
- .NET Aspire templates (optional - usually pre-installed with .NET 10 SDK)

### Build

```bash
dotnet build
```

### Run API

```bash
dotnet run --project src/BloodThinnerTracker.Api
```

### Run with Aspire (Orchestration) - Recommended

**.NET Aspire** provides one-click developer orchestration with:
- 🚀 **One-Click F5**: Start all services and containers automatically
- 📊 **Observability Dashboard**: Real-time logs, traces, and metrics at https://localhost:17225
- 🐳 **Container Management**: PostgreSQL database with automatic lifecycle
- 🔍 **Service Discovery**: Automatic endpoint resolution between services
- 🐛 **Integrated Debugging**: Breakpoints work across API, Web, and Database

**Quick Start (30 seconds)**:
1. Set `BloodThinnerTracker.AppHost` as startup project in Visual Studio 2025
2. Press **F5** (or `dotnet run --project src/BloodThinnerTracker.AppHost`)
3. Dashboard opens automatically at https://localhost:17225
4. All services start automatically (API, Web, PostgreSQL)

**First-Time Setup** (optional - templates pre-installed):
```bash
# Install Aspire templates (if not already installed)
dotnet new install Aspire.ProjectTemplates::9.5.2
```

**Dashboard Features**:
- 📝 **Logs**: View real-time logs from all services with filtering
- 📈 **Traces**: See distributed traces across API and Web
- 📊 **Metrics**: Monitor request counts, durations, database calls
- 💚 **Health**: Check service and database health status
- 🔗 **Resources**: Quick links to API, Web, and database connections

**For More Details**: See [specs/004-as-a-developer/quickstart.md](specs/004-as-a-developer/quickstart.md)

### Test

```bash
dotnet test
```

---

## Features

### ✅ Implemented

- **OAuth 2.0 Authentication**: Google and Azure AD login
- **JWT Token Management**: Secure access and refresh tokens
- **Self-Service Testing**: OAuth test page for developers
- **Medication Tracking**: CRUD operations for medications
- **INR Test Results**: Record and track INR values
- **Real-time Sync**: SignalR for cross-device updates
- **API Documentation**: Scalar UI with interactive testing

### 🚧 In Progress

- **Web UI Authentication**: Blazor auth state provider
- **Mobile App**: .NET MAUI implementation
- **Medication Reminders**: Push notifications with 99.9% SLA
- **mTLS Authentication**: Certificate-based auth for integrations

### 📋 Planned

- **INR Trend Analysis**: Charts and insights
- **Medication Schedule**: Customizable dosing schedules
- **Export/Import**: Data portability
- **Multi-language**: Localization support

---

## Security

- **OWASP Guidelines**: Followed for all security implementations
- **AES-256 Encryption**: Health data encrypted at rest
- **HTTPS Only**: Enforced in production
- **CSRF Protection**: State-based CSRF tokens for OAuth
- **ID Token Validation**: Using official Google and Microsoft libraries
- **JWT Expiration**: 15-minute access tokens, refresh token rotation

---

## Compliance

- **HIPAA Considerations**: Data encryption, audit logging, access controls
- **GDPR Ready**: Data portability, right to be forgotten, consent management
- **Medical Device Disclaimer**: This is a tracking tool, not medical advice

---

## Contributing

This project uses spec-driven development. All features start with:
1. **Specification**: Written in `specs/feature/`
2. **Task Breakdown**: Tracked in `specs/feature/blood-thinner-medication-tracker/tasks.md`
3. **Implementation**: Code in `src/`
4. **Documentation**: Updated in `docs/`

---

## License

[License information to be added]

---

## Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/blood_thinner_INR_tracker/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/blood_thinner_INR_tracker/discussions)
- **Documentation**: [docs/](docs/)

---

**Built with ❤️ using .NET 10 and spec-driven development**
