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
- **Medication**: Drug info and scheduling with pattern support
- **MedicationDosagePattern**: Temporal variable-dosage patterns (e.g., "4mg, 4mg, 3mg" repeating)
- **MedicationLog**: Dose tracking with variance detection (expected vs. actual)
- **INRTest**: Blood test results and trends
- **INRSchedule**: Configurable test reminders

### Pattern Management

**Temporal Pattern Tracking**:
- **MedicationDosagePattern** entity stores variable-dosage schedules with date validity
- Pattern sequences stored as JSON: `[4.0, 4.0, 3.0, 4.0, 3.0, 3.0]` (6-day cycle)
- `StartDate` and `EndDate` (nullable) enable temporal queries for any historical date
- Multiple patterns per medication allow tracking dosage adjustments over time

**Pattern Calculation Pattern**:
```csharp
// Get expected dosage for any date
public decimal? GetExpectedDosageForDate(DateTime targetDate)
{
    // Find active pattern on target date
    var pattern = DosagePatterns
        .Where(p => p.StartDate <= targetDate && 
                   (p.EndDate == null || p.EndDate >= targetDate))
        .OrderByDescending(p => p.StartDate)
        .FirstOrDefault();
    
    if (pattern == null) return Dosage; // Fallback to fixed dosage
    
    // Calculate pattern position using modulo arithmetic (O(1) performance)
    int daysSinceStart = (targetDate.Date - pattern.StartDate.Date).Days;
    int patternDay = (daysSinceStart % pattern.PatternLength) + 1;
    
    return pattern.GetDosageForDay(patternDay);
}
```

**Variance Tracking**:
- **MedicationLog** enhanced with `ExpectedDosage`, `ActualDosage`, `PatternDayNumber`
- Auto-populate expected dosage on log creation from active pattern
- `HasVariance` computed property: `|ActualDosage - ExpectedDosage| > 0.01`
- Enable variance reports for medication adherence tracking

**MudBlazor Pattern Entry Components**:
- **Simple Mode**: Single fixed dosage (existing behavior)
- **Pattern Mode**: Comma-separated input with MudChipSet visualization ("4, 4, 3")
- **Advanced Mode**: Day-by-day manual entry for complex patterns
- Use `MudToggleGroup` for mode selection, avoid JavaScript interop

**API Patterns**:
- **POST** `/api/medications/{id}/patterns` - Create new pattern, optionally close previous
- **GET** `/api/medications/{id}/patterns` - List pattern history (temporal tracking)
- **GET** `/api/medications/{id}/patterns/active` - Get current active pattern
- **GET** `/api/medications/{id}/schedule?days=28` - Calculate future dosage schedule
- **GET** `/api/medication-logs/variance-report` - Analyze dosing accuracy

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

**⚠️ CRITICAL: Keep commit messages SHORT to avoid terminal buffer overflow crashes ⚠️**

**The Problem**: PowerShell terminal buffer has limited height. Long commit messages (>15 lines) cause:
- Exception: "The value must be greater than or equal to zero and less than the console's buffer size"
- Terminal hangs/crashes requiring recovery
- Lost work if commit fails silently

**The Solution**: ALWAYS use concise commit format:

- **Summary line**: Max 72 characters, imperative mood
- **Body**: Max 5-8 lines TOTAL, use bullet points
- **Format**: `feat/fix/docs/refactor: <summary>`
- **Example** (GOOD ✅):
  ```
  feat(phase8): Add API docs and user guide
  
  - Created 3 API documentation files (patterns, schedule, logs)
  - Created comprehensive user guide for dosage patterns
  - Verified logging and test coverage complete
  - Phase 8: 5/9 tasks complete (4 deferred)
  ```
- **NEVER DO** (BAD ❌):
  - Include full file contents
  - List every endpoint/method modified
  - Detailed validation rules
  - Multi-paragraph explanations
  - Backend/Frontend section headers with task lists
  - More than 8 lines total
- **Focus**: High-level "what" and "why", NOT the "how"
- **Details belong**: In PR description, not commit message

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

### Configuration File Management

**CRITICAL: Protect Secrets in Source Control**

#### Template & Local File Pattern

The project uses a **template + local override** pattern for configuration files with secrets:

**Committed to Git (Safe)**:
- `*.template` files with placeholder values: `${REPLACE_WITH_SECRET}`
- Setup automation scripts (e.g., `setup-dev.ps1`)
- Configuration documentation

**Local Only (in .gitignore)**:
- `launch.json` - VSCode debug configs (contains OAuth Client IDs)
- `tasks.json` - Build tasks (if modified with secrets)
- `appsettings.*.local.json` - Local overrides
- `.env` files - Environment variables with secrets

#### When to Update Template Files

**DO** update `*.template` files when:
1. Adding new debug configurations (non-secret portions)
2. Modifying build tasks (shared settings only)
3. Adding new launch compounds or test configurations
4. Documentation updates to help team understand usage

Example:
```bash
# Good: Update template and commit
git add .vscode/launch.json.template
git commit -m "feat: Add Android device debug config"
```

**DO NOT** update template if:
- Adding secret values (Client IDs, API keys, passwords)
- Making personal debugging changes
- Adding local-only settings

#### When to Update Local Files

**Local changes only** (in `.gitignore`, never committed):
1. Add OAuth Client IDs, Google API keys, Azure credentials
2. Change local ports or service URLs
3. Personal debugging preferences or breakpoints
4. Environment-specific settings

```bash
# Edit local file (not committed)
nano .vscode/launch.json
# Add: "Features__AzureClientId": "actual-id-here"

# ❌ Will not commit - file is .gitignore'd
git add .vscode/launch.json  # Fails silently (safe)
```

#### Syncing Between Template and Local

When you update the template with new non-secret configurations:

```bash
# 1. Update template file
# Edit .vscode/launch.json.template with new public settings

# 2. Regenerate local file from template
.\tools\scripts\setup-dev.ps1 -Force

# 3. Re-add your secrets to the regenerated local file
```

#### Secret Storage Options (Priority Order)

1. **VSCode launch.json** (Development Debugging)
   - Template: `.vscode/launch.json.template` (committed)
   - Local: `.vscode/launch.json` (.gitignore'd)
   - Use: Environment variables in debug configs
   - Scope: Local development only

2. **.NET User Secrets** (API Development - RECOMMENDED)
   - Location: `%APPDATA%\Microsoft\UserSecrets\<ProjectId>`
   - Encrypted: Yes (OS-level encryption)
   - Scope: Local development only
   - Usage: `dotnet user-secrets set "Key" "value"`

3. **appsettings.*.local.json** (Fallback)
   - Template: `appsettings.Development.json` (committed)
   - Local: `appsettings.Development.local.json` (.gitignore'd)
   - Encrypted: No (configuration only, not for secrets)
   - Scope: Configuration overrides only

4. **GitHub Actions Secrets** (CI/CD)
   - Location: Repository settings → Secrets and variables
   - Encrypted: Yes (GitHub encryption)
   - Scope: Automated deployments only
   - Access: `${{ secrets.SECRET_NAME }}`

5. **Azure KeyVault** (Production)
   - Location: Azure Portal → Key Vaults
   - Encrypted: Yes (HSM encryption)
   - Scope: All environments
   - Access: ServiceDefaults integration (already configured)

#### Development Setup for New Developers

New team members should follow one command:

```bash
# 1. Clone repository
git clone https://github.com/MarkZither/blood_thinner_INR_tracker.git
cd blood_thinner_INR_tracker

# 2. Run setup script (generates local files from templates + prompts for secrets)
.\tools\scripts\setup-dev.ps1

# 3. When prompted, provide OAuth credentials:
# Azure Client ID: [from Azure Portal]
# Google Client ID: [from Google Console]

# 4. Start development
dotnet run --project src/BloodThinnerTracker.AppHost
```

See `docs/DEVELOPMENT_SETUP.md` for complete setup guide with troubleshooting.

#### Copilot Assistant Guidance for Configuration Work

When working with launch.json, tasks.json, or appsettings files:

1. **Always check if file has .template version**
   - If modifying shared settings: Update `*.template` + commit
   - If adding secrets: Update local copy only (never commit)

2. **Never include actual secret values in suggestions**
   - Use `${REPLACE_WITH_*}` placeholders
   - Reference where to obtain values in comments
   - Always mark template files for `.gitignore`

3. **When updating shared templates**
   - Include comprehensive comments explaining each setting
   - List placeholder values that need customization
   - Update `docs/DEVELOPMENT_SETUP.md` with new setup steps

4. **For new configuration features**
   - Create `.template` version first
   - Update `tools/scripts/setup-dev.ps1` to handle new file
   - Document in `DEVELOPMENT_SETUP.md`
   - Commit template + script + docs (never the actual secrets file)
