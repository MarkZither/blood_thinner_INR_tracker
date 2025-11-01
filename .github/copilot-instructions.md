# GitHub Copilot Instructions

## Project Context: Blood Thinner Medication & INR Tracker

**CRITICAL: This project MUST use .NET 10 and C# 13 exclusively.**

### Technology Stack Requirements

- **Framework**: .NET 10 (C# 13) - LTS version
- **Platforms**: 
  - Backend: ASP.NET Core Web API
  - Mobile: .NET MAUI (iOS/Android)
  - Web: Blazor Server/WebAssembly
  - Console: .NET CLI tool
  - Integration: MCP Server
- **Orchestration**: .NET Aspire
- **Database**: Entity Framework Core with SQLite (local) + PostgreSQL (cloud)
- **Authentication**: OAuth 2.0 (Azure AD + Google) with JWT
- **Real-time**: SignalR for cross-device sync

### Code Generation Rules

1. **Target Framework**: Always use `<TargetFramework>net10.0</TargetFramework>` in project files
2. **Mobile Targets**: Use `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows10.0.19041.0`
3. **C# Version**: Leverage C# 13 features where appropriate
4. **Package References**: Use latest stable packages compatible with .NET 10
5. **Global.json**: Ensure SDK version 10.0.x is specified

### Medical Application Constraints

- **Safety First**: Implement 12-hour medication window validation
- **Data Security**: All health data must be encrypted (AES-256)
- **Compliance**: OWASP security guidelines mandatory
- **Privacy**: Local-first data with secure sync
- **Reliability**: 99.9% uptime requirement for reminders
- **Testing**: 90% code coverage requirement

### Project Structure

Follow David Fowler's repository layout conventions:

```
blood_thinner_INR_tracker/
├── src/
│   ├── BloodThinnerTracker.AppHost/           # .NET Aspire orchestration
│   ├── BloodThinnerTracker.ServiceDefaults/   # Shared Aspire configuration  
│   ├── BloodThinnerTracker.Api/               # ASP.NET Core Web API
│   ├── BloodThinnerTracker.Mobile/            # .NET MAUI app
│   ├── BloodThinnerTracker.Web/               # Blazor app
│   ├── BloodThinnerTracker.Cli/               # Console tool (.NET tool)
│   ├── BloodThinnerTracker.Mcp/               # MCP Server
│   └── BloodThinnerTracker.Shared/            # Shared models/contracts
├── tests/
│   ├── BloodThinnerTracker.Api.Tests/
│   ├── BloodThinnerTracker.Mobile.Tests/
│   ├── BloodThinnerTracker.Web.Tests/
│   └── BloodThinnerTracker.Integration.Tests/
├── docs/
│   ├── api/                                   # API documentation
│   ├── deployment/                            # Deployment guides
│   └── user-guide/                            # End-user documentation
├── samples/
│   ├── basic-setup/                           # Simple setup example
│   └── advanced-config/                       # Advanced configuration
├── tools/
│   ├── scripts/                               # Build and deployment scripts
│   └── generators/                            # Code generators
└── .github/
    ├── workflows/                             # CI/CD workflows
    └── copilot-instructions.md                # This file
```

### Key Entities

- **User**: Authentication and preferences
- **Medication**: Drug info and scheduling  
- **MedicationLog**: Dose tracking with safety validations
- **INRTest**: Blood test results and trends
- **INRSchedule**: Configurable test reminders

### Development Commands

```bash
# Always verify .NET 10
dotnet --version  # Should show 10.0.x

# Build commands
dotnet build
dotnet run --project BloodThinnerTracker.AppHost

# Mobile specific  
dotnet build -f net10.0-android
dotnet run -f net10.0-android

# Testing
dotnet test --collect:"XPlat Code Coverage"
```

### Git Commit Guidelines

**CRITICAL: Keep commit messages concise to avoid terminal crashes**

- **Summary line**: Max 72 characters, imperative mood
- **Body**: Max 10-15 lines total, use bullet points
- **Format**: `feat/fix/docs/refactor: <summary>`
- **Example**:
  ```
  feat(ui): Migrate Dashboard and Medications to MudBlazor
  
  - Convert Bootstrap/FontAwesome to Material Design components
  - Implement reactive property pattern for filtering
  - Remove all legacy CSS framework dependencies
  ```
- **Never**: Include full file contents, detailed method signatures, or exhaustive change lists
- **Focus**: High-level summary of what changed and why

### Common Patterns

1. **Dependency Injection**: Use built-in DI container
2. **Async/Await**: All I/O operations must be async
3. **Logging**: Use ILogger with structured logging
4. **Configuration**: Use IConfiguration with appsettings.json
5. **Health Checks**: Implement for all services
6. **Error Handling**: Global exception middleware

### Security Requirements

- Input validation on all endpoints
- SQL injection prevention (use EF parameterized queries)
- XSS protection for Blazor components
- CSRF protection for state-changing operations
- Secure authentication token storage
- Medical data encryption at rest and in transit

### Performance Guidelines

- Use async enumerable for large data sets
- Implement pagination for API endpoints
- Use EF Core compiled queries for frequent operations
- Optimize Blazor rendering with proper component lifecycle
- Implement caching for read-heavy operations

### Mobile Considerations

- Offline-first data architecture
- Background processing for reminders
- Platform-specific notification handling
- Secure storage using platform keychains
- Battery optimization for background tasks

### .NET Aspire Orchestration Patterns

**AppHost Configuration** (BloodThinnerTracker.AppHost):
- Use `DistributedApplication.CreateBuilder(args)` to create AppHost
- Configure container lifetime based on environment (Persistent vs Session)
- Use parameters for secrets: `builder.AddParameter("name", secret: true)`
- Reference services with `WithReference()` for automatic service discovery
- Configure ports with `WithHttpsEndpoint()` and `WithHttpEndpoint()`
- Add database containers with `.AddPostgres()`, `.AddSqlServer()`, etc.
- Use `.WithDataVolume()` for persistent container storage

**ServiceDefaults Configuration** (BloodThinnerTracker.ServiceDefaults):
- All projects MUST call `builder.AddServiceDefaults()` in Program.cs
- All projects MUST call `app.MapDefaultEndpoints()` before Run()
- ServiceDefaults provides: OpenTelemetry, Service Discovery, Polly, Health Checks
- Use `IServiceDefaults` interface for shared configuration
- Test ServiceDefaults with unit tests (target 90%+ coverage)

**Service Discovery Pattern**:
```csharp
// In AppHost.cs
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api");
var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithReference(api); // Web can now discover API

// In Program.cs (Web project)
builder.AddServiceDefaults(); // Enables service discovery
var app = builder.Build();

// HttpClient automatically resolves "http://api" to actual endpoint
var response = await httpClient.GetAsync("http://api/health");
```

**Database Reference Pattern**:
```csharp
// In AppHost.cs
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();
var bloodtrackerDb = postgres.AddDatabase("bloodtracker");

var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithReference(bloodtrackerDb); // Injects connection string

// Connection string automatically available as:
// ConnectionStrings__bloodtracker (environment variable)
```

**Testing Pattern** (AppHost.Tests):
```csharp
// Use AppHostFixture with IClassFixture for test performance
public class AppHostFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    
    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.BloodThinnerTracker_AppHost>(cancellationToken);
        _app = await appHost.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);
    }
}

// Use fixture in tests
[Collection("AppHost")]
public class MyTests : IClassFixture<AppHostFixture>
{
    private readonly AppHostFixture _fixture;
    
    [Fact]
    public async Task Test()
    {
        var httpClient = _fixture.App.CreateHttpClient("api");
        // Test code here
    }
}
```

**Container Lifetime Management**:
- Use `ASPIRE_CONTAINER_LIFETIME=Session` environment variable for tests (ephemeral)
- Use `ContainerLifetime.Persistent` for local development (preserves data)
- Always call `.WithDataVolume()` when using Persistent lifetime

**Optional Features**:
- Use configuration-based feature flags: `builder.Configuration.GetValue<bool>("Features:EnableX")`
- InfluxDB: Optional time-series metrics storage (disabled by default)
- Keep optional features behind flags to maintain simple default experience

When generating code, ALWAYS:
- Use .NET 10 syntax and features
- Include proper error handling
- Add XML documentation comments
- Follow naming conventions (PascalCase for public, camelCase for private)
- Include unit tests for business logic
- Validate medical data constraints (12-hour window, INR ranges 0.5-8.0)
- Add medical disclaimer where appropriate
