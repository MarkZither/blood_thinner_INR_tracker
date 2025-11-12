# Feature 004: Technology Research & Decision Log

**Phase**: 0 (Research)  
**Date**: October 30, 2025  
**Status**: Complete

This document captures all technology decisions, rationale, and alternatives considered for implementing local development orchestration with .NET Aspire.

---

## R-001: Aspire 10 RC2 Compatibility

**Question**: Does .NET Aspire 10.0.0 work with .NET 10 GA? What are the exact version combinations?

**Decision**: Use .NET Aspire 10.0.0 with .NET 10.0.100 SDK

**Rationale**:

**Configuration**:
```json
// global.json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

```xml
<!-- AppHost.csproj -->
<ItemGroup>
    <PackageReference Include="Aspire.Hosting" Version="10.0.0" />
    <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="10.0.0" />
</ItemGroup>
```

**Alternatives Considered**:
- ❌ Using .NET 9 with Aspire 9.x: Rejected because project requires .NET 10 features
- ❌ Waiting for .NET 10 GA: Rejected because RC2 is stable enough for development

**References**:
- https://learn.microsoft.com/en-us/dotnet/aspire/get-started/
- NuGet: https://www.nuget.org/packages/Aspire.Hosting

---

## R-002: Serilog Integration with Aspire Dashboard

**Question**: How to configure Serilog to work with both Aspire Dashboard (OTLP) AND external sinks (InfluxDB)?

**Decision**: Use Serilog with dual sinks - Console (for Aspire) + InfluxDB (for persistence)

**Rationale**:
- Aspire Dashboard consumes logs via OpenTelemetry Protocol (OTLP)
- Serilog can write to multiple sinks simultaneously
- Console sink with structured JSON format integrates with OTLP exporter
- InfluxDB sink provides long-term storage and advanced querying
- Serilog.Sinks.OpenTelemetry package bridges Serilog to OTLP directly

**Configuration**:
```csharp
// ServiceDefaults.cs
builder.Services.AddSerilog((services, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
        .WriteTo.Console(new JsonFormatter())  // Aspire Dashboard
        .WriteTo.OpenTelemetry(options =>      // Direct OTLP export
        {
            options.Endpoint = "http://localhost:4317";  // Aspire collector
            options.Protocol = OtlpProtocol.Grpc;
        });

    // Optional: InfluxDB sink for advanced querying
    if (builder.Configuration.GetValue<bool>("Logging:InfluxDB:Enabled"))
    {
        loggerConfig.WriteTo.InfluxDB(
            builder.Configuration["Logging:InfluxDB:Uri"],
            builder.Configuration["Logging:InfluxDB:Database"]
        );
    }
});
```

**Structured Logging Pattern**:
```csharp
_logger.LogInformation(
    "Service {ServiceName} started on {Endpoint} in {StartupTime}ms",
    serviceName, endpoint, startupTime);
// Not: _logger.LogInformation($"Service {serviceName} started...");
```

**Alternatives Considered**:
- ❌ Microsoft.Extensions.Logging only: Rejected because lacks structured logging richness
- ❌ Direct OpenTelemetry SDK: Rejected because Serilog provides better structured logging API
- ❌ NLog: Rejected because Serilog has better .NET ecosystem integration

**References**:
- https://github.com/serilog/serilog-sinks-opentelemetry
- https://github.com/serilog/serilog-sinks-influxdb

---

## R-003: Polly Resilience Patterns for Service-to-Service Calls

**Question**: Which Polly policies are recommended for local development service calls?

**Decision**: Use Microsoft.Extensions.Http.Resilience with standard resilience handler

**Rationale**:
- Microsoft.Extensions.Http.Resilience provides opinionated defaults for microservices
- Standard resilience handler combines retry, circuit breaker, and timeout
- Local development benefits from fast failure detection (circuit breaker)
- Retry with exponential backoff handles transient startup timing issues
- Timeout prevents hanging on slow responses

**Configuration**:
```csharp
// ServiceDefaults.cs
builder.Services.ConfigureHttpClientDefaults(http =>
{
    // Add standard resilience handler (retry + circuit breaker + timeout)
    http.AddStandardResilienceHandler(options =>
    {
        // Retry configuration for local development
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;

        // Circuit breaker to prevent cascading failures
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);

        // Timeout for local calls
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
    });

    // Add service discovery integration
    http.AddServiceDiscovery();
});
```

**Usage in API Client**:
```csharp
// Web project calling API
public class MedicationService
{
    private readonly HttpClient _httpClient;

    public MedicationService(IHttpClientFactory httpClientFactory)
    {
        // Polly policies automatically applied
        _httpClient = httpClientFactory.CreateClient("api");
    }

    public async Task<List<Medication>> GetMedicationsAsync()
    {
        // Resilience policies handle retries, circuit breaking, timeouts
        var response = await _httpClient.GetAsync("http://api/medications");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Medication>>();
    }
}
```

**Alternatives Considered**:
- ❌ No resilience policies: Rejected because local development has timing issues
- ❌ Manual Polly configuration: Rejected because standard handler is more maintainable
- ❌ Different policies per service: Rejected because adds complexity without local dev benefit

**References**:
- https://learn.microsoft.com/en-us/dotnet/core/resilience/
- https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience

---

## R-004: Service Discovery Endpoint Resolution

**Question**: How does Aspire service discovery resolve service names to URLs?

**Decision**: Use Aspire's built-in service discovery via `builder.AddServiceDiscovery()` and `http://{servicename}` URLs

**Rationale**:
- Aspire injects environment variables for service endpoints automatically
- Service discovery middleware resolves logical names to actual URLs
- Works seamlessly with HttpClient and Polly integration
- No manual configuration needed in appsettings.json
- Supports multiple instances and load balancing if needed

**Configuration**:
```csharp
// AppHost Program.cs - Define service topology
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();  // Persist data between restarts

var db = postgres.AddDatabase("bloodtracker");

var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithReference(db);  // Injects CONNECTION_STRING environment variable

var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithReference(api);  // Injects API endpoint as services__api__http__0

builder.Build().Run();
```

```csharp
// ServiceDefaults.cs - Enable service discovery
public static class ServiceDefaultsExtensions
{
    public static IHostApplicationBuilder AddServiceDefaults(
        this IHostApplicationBuilder builder)
    {
        builder.Services.AddServiceDiscovery();  // Enable resolution

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddServiceDiscovery();  // Integrate with HttpClient
        });

        return builder;
    }
}
```

```csharp
// Web project - Use logical service names
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient("api", client =>
        {
            client.BaseAddress = new Uri("http://api");  // Resolved by service discovery
        });
    }
}
```

**Environment Variable Injection**:
When AppHost starts, it sets these environment variables for dependent services:
- API service gets: `ConnectionStrings__bloodtracker="Host=localhost;Port=5432;..."`
- Web service gets: `services__api__http__0="http://localhost:5234"`

**Alternatives Considered**:
- ❌ Hardcoded URLs in appsettings.json: Rejected because not portable across environments
- ❌ Manual environment variable management: Rejected because error-prone and inflexible
- ❌ Service mesh (Consul, Eureka): Rejected as overkill for local development

**References**:
- https://learn.microsoft.com/en-us/dotnet/aspire/service-discovery/overview
- https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview

---

## R-005: Hot Reload Compatibility

**Question**: Does hot reload work across multiple projects in Aspire orchestration?

**Decision**: Hot reload is supported with limitations - works for individual service code changes, not AppHost changes

**Rationale**:
- .NET hot reload works within individual service projects (API, Web)
- Changes to AppHost Program.cs require full restart (orchestration topology change)
- Changes to ServiceDefaults.cs require service restart (not hot reloadable)
- Blazor hot reload works for .razor component changes
- API hot reload works for controller/service changes

**What Works** (Hot Reload Supported):
- ✅ Blazor .razor component UI changes
- ✅ C# method implementation changes in controllers/services
- ✅ Adding new methods to existing classes
- ✅ CSS/JavaScript changes in wwwroot

**What Doesn't Work** (Requires Restart):
- ❌ AppHost Program.cs topology changes (adding/removing services)
- ❌ Service startup configuration changes (Program.cs)
- ❌ appsettings.json changes (requires service restart)
- ❌ Adding new NuGet packages
- ❌ Database schema changes (requires migration)

**Developer Experience**:
- Most code changes (<80%) can use hot reload
- Infrastructure changes require full restart (~15 seconds)
- Dashboard shows which services restarted vs. hot reloaded

**Alternatives Considered**:
- ❌ Manual project-by-project debugging: Rejected because loses orchestration benefits
- ❌ File watchers for automatic restart: Rejected because .NET hot reload is faster

**References**:
- https://learn.microsoft.com/en-us/visualstudio/debugger/hot-reload
- https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-watch

---

## R-006: Container Volume Management

**Question**: How to persist PostgreSQL data between AppHost restarts?

**Decision**: Use `WithDataVolume()` for persistent storage, default to ephemeral for clean state

**Rationale**:
- Persistent volumes maintain database state across restarts (developer preference)
- Ephemeral containers provide clean state for testing (useful for integration tests)
- Named volumes allow multiple developers to use different data sets
- Docker automatically manages volume lifecycle

**Configuration**:
```csharp
// AppHost Program.cs - Persistent data
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();  // Creates Docker named volume

var db = postgres.AddDatabase("bloodtracker");
```

```csharp
// Alternative: Ephemeral (no volume)
var postgres = builder.AddPostgres("postgres");  // Data lost on stop
```

```csharp
// Alternative: Custom volume name
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("bloodtracker-dev-data");  // Named volume
```

**Volume Management**:
```bash
# View volumes
docker volume ls

# Remove volume (reset data)
docker volume rm bloodtracker-dev-data

# Backup volume
docker run --rm -v bloodtracker-dev-data:/data -v $(pwd):/backup busybox tar czf /backup/backup.tar.gz /data
```

**Developer Workflow**:
1. Default: Use persistent volume (developers keep their test data)
2. Fresh start: Delete volume via Docker Desktop or CLI
3. Integration tests: Use ephemeral containers (Testcontainers)

**Alternatives Considered**:
- ❌ Host-mounted volumes: Rejected due to permission issues across OS
- ❌ Always ephemeral: Rejected because developers lose test data frequently
- ❌ SQLite only: Rejected because want to test PostgreSQL-specific features

**References**:
- https://learn.microsoft.com/en-us/dotnet/aspire/database/postgresql-component
- https://docs.docker.com/storage/volumes/

---

## R-007: Port Conflict Handling

**Question**: How does Aspire handle port conflicts when default ports are occupied?

**Decision**: Use Aspire's automatic port allocation with explicit port configuration for predictable debugging

**Rationale**:
- Aspire assigns dynamic ports automatically to avoid conflicts
- Dashboard always runs on fixed port (15000 by default, configurable)
- Services can specify preferred ports via launchSettings.json
- Environment variables communicate actual ports to dependent services
- Port conflicts detected at startup with clear error messages

**Configuration**:
```csharp
// AppHost Program.cs - Explicit port configuration
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithHttpEndpoint(port: 5234, name: "http")
    .WithHttpsEndpoint(port: 7234, name: "https");

var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithHttpEndpoint(port: 5235, name: "http")
    .WithReference(api);
```

```json
// API launchSettings.json - Match AppHost configuration
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5234"
    },
    "https": {
      "commandName": "Project",
      "applicationUrl": "https://localhost:7234;http://localhost:5234"
    }
  }
}
```

**Port Conflict Detection**:
- Aspire checks port availability at startup
- Error message: "Port {port} is already in use by another application"
- Suggested fix: "Change port in AppHost configuration or stop conflicting application"
- Dashboard URL shown in console: "Dashboard: http://localhost:15000"

**Dynamic Port Allocation**:
```csharp
// Alternative: Let Aspire choose ports (for CI/CD)
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithHttpEndpoint();  // Aspire assigns available port
```

**Alternatives Considered**:
- ❌ Always use dynamic ports: Rejected because debugging requires predictable URLs
- ❌ Manual port conflict resolution: Rejected because error-prone
- ❌ Port range reservation: Rejected as unnecessary complexity

**References**:
- https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview#endpoint-configuration

---

## R-008: Dashboard Authentication (or lack thereof)

**Question**: Does Aspire Dashboard require authentication for local development?

**Decision**: No authentication required for local development - dashboard is localhost-only

**Rationale**:
- Aspire Dashboard is designed for local development (not production monitoring)
- Binds to localhost (127.0.0.1) only, not accessible from network
- Browser security model prevents cross-origin access
- No sensitive patient data in dashboard (only development logs/metrics)
- Adding authentication would slow down development workflow

**Security Posture**:
- ✅ Dashboard accessible only from developer's machine
- ✅ No network exposure (localhost binding)
- ✅ No patient data displayed (development environment)
- ✅ Logs/traces contain no PII (per constitution)
- ⚠️ Anyone with physical access to developer machine can view dashboard (acceptable for local dev)

**Configuration**:
```csharp
// AppHost Program.cs - Dashboard configuration
var builder = DistributedApplication.CreateBuilder(args);

// Dashboard automatically starts on localhost:15000
// No authentication configuration needed

builder.Build().Run();
```

**Production Monitoring**:
- ❌ DO NOT use Aspire Dashboard in production
- ✅ Use Azure Monitor / Application Insights for production
- ✅ Use Azure Portal for Azure Container Apps monitoring
- Production monitoring configured separately (not part of this feature)

**Alternatives Considered**:
- ❌ Basic authentication: Rejected because adds friction to local development
- ❌ Network-accessible dashboard: Rejected as security risk and not needed
- ❌ Production-grade monitoring: Rejected as out of scope (local dev only)

**References**:
- https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview
- https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/configuration

---

## R-009: Connection String Security Strategy

**Question**: How should PostgreSQL connection strings be secured for local development? What password approach balances security with developer experience?

**Decision**: **Phase 1** - Start with hardcoded password `local_dev_only_password`; **Phase 2** - Upgrade to environment variable before feature completion

**Rationale**:
- **Local development only** - No patient data, no network exposure, acceptable risk
- **Iterative approach** - Get basic functionality working first, then enhance security
- **Constitution compliance** - Feature is explicitly "LOCAL DEVELOPMENT ONLY" (Principle VI)
- **Clear migration path** - Environment variables provide sufficient security for local dev
- **Developer experience** - Predictable passwords simplify debugging initially
- **Security upgrade planned** - Tasks T073a and T073b ensure env var migration

**Phase 1 Configuration (Initial Implementation)**:
```csharp
// AppHost/Program.cs - Initial approach (T040)
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", "local_dev_only_password")  // Hardcoded
    .WithEnvironment("POSTGRES_DB", "bloodtracker")
    .WithDataVolume("aspire-postgres-data");

var db = postgres.AddDatabase("bloodtracker");

var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithReference(db);  // Injects: Host=localhost;Port=5432;Username=postgres;Password=local_dev_only_password;Database=bloodtracker
```

**Phase 2 Configuration (Security Upgrade - T073a)**:
```csharp
// AppHost/Program.cs - Environment variable approach
var postgresPassword = builder.Configuration["POSTGRES_PASSWORD"] 
    ?? throw new InvalidOperationException("POSTGRES_PASSWORD environment variable required");

var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", postgresPassword)  // From environment
    .WithEnvironment("POSTGRES_DB", "bloodtracker")
    .WithDataVolume("aspire-postgres-data");
```

**Developer Setup (Phase 2)**:
```powershell
# Windows - Add to user environment variables
[Environment]::SetEnvironmentVariable("POSTGRES_PASSWORD", "MySecureLocalPassword123", "User")

# Or set temporarily in PowerShell session
$env:POSTGRES_PASSWORD = "MySecureLocalPassword123"

# Linux/macOS - Add to ~/.bashrc or ~/.zshrc
export POSTGRES_PASSWORD="MySecureLocalPassword123"
```

**Documentation Requirements**:
- quickstart.md must document Phase 2 environment variable requirement (added in T073b)
- reset-database.ps1 must handle both approaches (T073b)
- README.md must include security note about local dev passwords

**Security Boundaries**:
- ✅ Acceptable for local development (no patient data)
- ✅ Not committed to source control (environment-specific)
- ✅ Not exposed to network (localhost binding)
- ✅ Documented as non-production approach
- ⚠️ Visible in process environment variables (acceptable trade-off)
- ❌ NOT acceptable for production (use Azure PostgreSQL managed identity)

**Production Approach** (Out of Scope):
- Azure PostgreSQL with Azure Managed Identity (no passwords at all)
- Connection string from Azure Key Vault
- Handled by separate production deployment feature

**Alternatives Considered**:
- ❌ **Random password per session**: Rejected because breaks data persistence between runs (Docker volume would have different password)
- ❌ **User Secrets (dotnet user-secrets)**: Rejected because requires per-developer setup before F5 works
- ❌ **Azure Key Vault for local dev**: Rejected as overkill for local development
- ❌ **No password (trust authentication)**: Rejected because PostgreSQL requires authentication

**Migration Timeline**:
- **T040**: Phase 1 implementation (hardcoded password)
- **T040a**: Document Phase 1 approach with security note
- **US1-US5**: Focus on functionality with Phase 1
- **T073a**: Phase 2 implementation (environment variable)
- **T073b**: Update tooling (reset script)
- **T074**: Update documentation with Phase 2 setup instructions

**References**:
- https://learn.microsoft.com/en-us/aspire/database/postgresql-component
- https://www.postgresql.org/docs/current/auth-password.html
- Blood Thinner Tracker Constitution Principle VI (Cloud Deployment - Local Dev Only)

---

## R-010: Aspire Workload Deprecation & Distribution Model Change

**Question**: How should Aspire be installed and distributed in .NET 10 RC2? Is the workload still required?

**Decision**: Use NuGet packages directly + Aspire.ProjectTemplates for project scaffolding. **DO NOT use `dotnet workload install aspire`** (deprecated).

**Rationale**:
- **CRITICAL CHANGE**: As of .NET Aspire 10, the Aspire workload is **deprecated and no longer necessary**
- Aspire is now distributed exclusively via NuGet packages (Aspire.Hosting, Aspire.Hosting.PostgreSQL, etc.)
- Project templates are installed separately via `dotnet new install Aspire.ProjectTemplates`
- This simplifies installation, reduces SDK bloat, and aligns with standard .NET package management
- Official guidance from Microsoft redirects from workload to NuGet approach
- Using templates (e.g., `aspire-xunit`) provides better starting point than manual project creation

**Updated Installation Steps**:
```powershell
# 1. Install project templates (one-time)
dotnet new install Aspire.ProjectTemplates

# 2. No workload installation needed! (deprecated)
# OLD (DO NOT USE): dotnet workload install aspire

# 3. Create projects using templates
dotnet new aspire-apphost -n BloodThinnerTracker.AppHost
dotnet new aspire-servicedefaults -n BloodThinnerTracker.ServiceDefaults

# OR use aspire-xunit template for testing-first approach
dotnet new aspire-xunit -n BloodThinnerTracker.AppHost.Tests
```

**Updated Package References**:
```xml
<!-- AppHost.csproj -->
<ItemGroup>
    <PackageReference Include="Aspire.Hosting" Version="10.0.0" />
    <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="10.0.0" />
</ItemGroup>

<!-- ServiceDefaults.csproj -->
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="10.0.0" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.9.0" />
</ItemGroup>
```

**Recommended Approach for This Feature**:
1. **Delete existing AppHost and ServiceDefaults projects** (if manually created)
2. Use `dotnet new aspire-xunit` template to scaffold AppHost with integrated testing
3. Customize generated AppHost/Program.cs with our service topology
4. Add ServiceDefaults using `aspire-servicedefaults` template
5. Reference AppHost from test project using `DistributedApplication` pattern

**Advantages of Template Approach**:
- ✅ Correct project structure from start (proper references, launch settings)
- ✅ Integrated testing setup (aspire-xunit includes Aspire.Hosting.Testing)
- ✅ Up-to-date NuGet package versions
- ✅ Proper SDK configuration (EnableSdkContainerSupport, etc.)
- ✅ No manual configuration of OTLP endpoints, service discovery, etc.

**Alternatives Considered**:
- ❌ Using deprecated `dotnet workload install aspire`: No longer supported, generates warnings
- ❌ Manual project creation: Error-prone, misses template-provided configuration
- ❌ Keeping existing AppHost/ServiceDefaults: May have incorrect structure/missing configuration

**Impact on Implementation Plan**:
- **Phase 1 Setup (T001-T006)**: Replace manual project creation with template usage
- **T001**: Change from "Create AppHost project" to "Generate from aspire-xunit template"
- **T002**: Template handles global.json and SDK configuration automatically
- **T003**: Template includes ServiceDefaults reference and package setup
- **Prerequisites Documentation**: Update to remove workload, add template installation

**References**:
- https://aka.ms/aspire/support-policy (Workload deprecation notice)
- https://learn.microsoft.com/en-us/dotnet/aspire/get-started/
- NuGet: https://www.nuget.org/packages/Aspire.ProjectTemplates

---

## Summary

All research tasks completed. Key technology decisions:

| Component | Decision | Version |
|-----------|----------|---------|
| .NET SDK | .NET 10 | 10.0.100 |
| Aspire | **NuGet packages (NOT workload)** | 9.5.2 |
| Templates | Aspire.ProjectTemplates | 10.0.0 |
| Logging | Serilog + OTLP | Serilog.AspNetCore 8.0.2 |
| Resilience | Microsoft.Extensions.Http.Resilience | 8.10.0 |
| Service Discovery | Aspire built-in | (included in Aspire) |
| Container Storage | Docker named volumes | (persistent by default) |
| Authentication | None (local dev only) | N/A |
| Connection Security | Phase 1: Hardcoded → Phase 2: Env Var | PostgreSQL 16-alpine |

**CRITICAL NOTE**: Aspire workload is deprecated. Use NuGet packages + project templates instead.

**Exit Criteria**: ✅ All research questions answered with documented rationale and alternatives considered.

**Next Phase**: Phase 1 (Design & Contracts) - Generate data-model.md, contracts/, and quickstart.md
