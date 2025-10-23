# .NET Aspire Implementation Guide

## Current State (October 2025)

❌ **AppHost and ServiceDefaults projects exist but are NOT real Aspire projects**
- They are placeholder/stub projects with hardcoded strings
- Missing Aspire.Hosting SDK
- No service discovery or orchestration
- No OpenTelemetry automatic instrumentation
- No Aspire Dashboard

## What Proper Aspire Implementation Provides

### 1. Automatic Service Orchestration
- Single command to start all services: `dotnet run --project src/BloodThinnerTracker.AppHost`
- Service discovery between API, Web, Mobile backends
- Automatic port management and configuration

### 2. Built-in OpenTelemetry (Solves T013)
- Distributed tracing across all services
- Metrics collection and visualization
- Logging aggregation
- Health check monitoring

### 3. Aspire Dashboard
- Real-time service status: http://localhost:15888
- Trace visualization
- Metrics explorer
- Log streaming
- Health check status

### 4. Resilience Patterns
- Automatic retry policies
- Circuit breakers
- Timeouts and fallbacks

## Implementation Steps (T003a-T003e)

### T003a: Add Aspire SDK

Update `src/BloodThinnerTracker.AppHost/BloodThinnerTracker.AppHost.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsAspireHost>true</IsAspireHost>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" Version="10.0.0-rc.2.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\BloodThinnerTracker.Api\BloodThinnerTracker.Api.csproj" />
    <ProjectReference Include="..\BloodThinnerTracker.Web\BloodThinnerTracker.Web.csproj" />
    <ProjectReference Include="..\BloodThinnerTracker.Mcp\BloodThinnerTracker.Mcp.csproj" />
    <ProjectReference Include="..\BloodThinnerTracker.ServiceDefaults\BloodThinnerTracker.ServiceDefaults.csproj" />
  </ItemGroup>

</Project>
```

### T003b: Configure Service Discovery

Replace `src/BloodThinnerTracker.AppHost/Program.cs`:
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL for production (optional - can use SQLite for dev)
var postgres = builder.AddPostgres("postgres")
                     .WithPgAdmin();
var bloodTrackerDb = postgres.AddDatabase("bloodtracker");

// Add API service
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
                .WithReference(bloodTrackerDb)
                .WithExternalHttpEndpoints();

// Add Web UI
var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
                .WithReference(api);

// Add MCP Server
var mcp = builder.AddProject<Projects.BloodThinnerTracker_Mcp>("mcp")
                .WithReference(api);

builder.Build().Run();
```

### T003c: Add OpenTelemetry Integration

Update `src/BloodThinnerTracker.ServiceDefaults/BloodThinnerTracker.ServiceDefaults.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsAspireSharedProject>true</IsAspireSharedProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.0.0-rc.2.*" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="10.0.0-rc.2.*" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.10.0" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.10.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.10.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.10.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.10.0" />
  </ItemGroup>

</Project>
```

Replace `src/BloodThinnerTracker.ServiceDefaults/ServiceDefaults.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace BloodThinnerTracker.ServiceDefaults;

public static class ServiceDefaults
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
```

### T003d: Update Services to Use Aspire

Update `src/BloodThinnerTracker.Api/Program.cs`:
```csharp
// Add at the top of Program.cs (before builder creation)
var builder = WebApplication.CreateBuilder(args);

// Add this line immediately after
builder.AddServiceDefaults(); // <-- Add this for Aspire integration

// ... rest of existing code ...

var app = builder.Build();

// Add this before app.Run()
app.MapDefaultEndpoints(); // <-- Add this for health checks

app.Run();
```

Similarly update `BloodThinnerTracker.Web/Program.cs` and `BloodThinnerTracker.Mcp/Program.cs`.

### T003e: Verify Aspire Dashboard

Run the AppHost:
```powershell
dotnet run --project src/BloodThinnerTracker.AppHost
```

You should see:
- ✅ Aspire Dashboard URL: http://localhost:15888
- ✅ All services auto-started
- ✅ Service discovery working
- ✅ Traces, metrics, and logs flowing

## Benefits for This Project

### Medical Safety
- **Distributed tracing** shows complete request flow across services
- **Health checks** ensure medication reminders are working
- **Metrics** track notification delivery rates (99.9% SLA requirement)

### Development Experience
- **Single command** to start entire stack
- **Live dashboard** shows all service health
- **Automatic configuration** reduces boilerplate

### Production Readiness
- **Resilience patterns** prevent cascade failures
- **OpenTelemetry** enables production observability
- **Service discovery** works in containers/Kubernetes

## Current Blockers

Without proper Aspire:
- ❌ T013 (OpenTelemetry) requires manual configuration
- ❌ Services must be started individually
- ❌ No visibility into cross-service issues
- ❌ Missing health check infrastructure
- ❌ No distributed tracing for debugging

With proper Aspire:
- ✅ T013 automatically satisfied
- ✅ One-command startup
- ✅ Built-in observability
- ✅ Production-grade resilience
- ✅ Healthcare-compliant monitoring

## Next Steps

1. Complete T003a-e to implement real Aspire
2. Mark T013 as complete (will be automatic)
3. Add health checks to MedicationReminderService
4. Configure alerts for 99.9% SLA monitoring (T044a)

## References

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [OpenTelemetry in Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry)
