# Service Defaults Integration Contract

**Feature**: 004 - Local Development Orchestration  
**Version**: 1.0  
**Date**: October 30, 2025

This document defines the contract for integrating Aspire service defaults into API and Web projects for automatic OpenTelemetry, logging, resilience, and service discovery configuration.

---

## Overview

The `ServiceDefaults` extension provides a single method call (`builder.AddServiceDefaults()`) that configures:
1. **OpenTelemetry** - Distributed tracing and metrics export to Aspire Dashboard
2. **Serilog** - Structured logging with OTLP integration
3. **Service Discovery** - HTTP client integration for resolving service names
4. **Resilience** - Polly policies for HTTP clients (retry, circuit breaker, timeout)
5. **Health Checks** - Standard health check endpoints

---

## Integration API

### Primary Method

```csharp
public static IHostApplicationBuilder AddServiceDefaults(
    this IHostApplicationBuilder builder
)
```

**Usage in API/Web Projects**:

```csharp
// API/Program.cs or Web/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (MUST be called early)
builder.AddServiceDefaults();

// ... register other services ...

var app = builder.Build();

// Map default endpoints (health checks)
app.MapDefaultEndpoints();

app.Run();
```

---

## What Gets Configured

### 1. OpenTelemetry Integration

**Tracing**:
- Traces ASP.NET Core requests
- Traces HttpClient calls
- Traces Entity Framework Core queries
- Exports to Aspire Dashboard via OTLP

**Metrics**:
- ASP.NET Core metrics (request duration, status codes)
- HttpClient metrics (request duration, failures)
- Runtime metrics (GC, thread pool)
- Custom metrics (medication logs, INR tests)

**Implementation**:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
    })
    .UseOtlpExporter();  // Reads OTEL_EXPORTER_OTLP_ENDPOINT env var
```

**Environment Variables Used**:
- `OTEL_EXPORTER_OTLP_ENDPOINT` - Aspire collector endpoint (e.g., `http://localhost:4317`)
- `OTEL_SERVICE_NAME` - Service name for telemetry (e.g., `api`, `web`)

---

### 2. Serilog Configuration

**Log Sinks**:
- Console sink (JSON formatted for OTLP ingestion)
- OpenTelemetry sink (direct OTLP export)
- Optional: InfluxDB sink (if enabled via configuration)

**Enrichers**:
- FromLogContext (correlation IDs, user context)
- Application name
- Environment name
- Machine name

**Implementation**:

```csharp
builder.Services.AddSerilog((services, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .WriteTo.Console(new JsonFormatter())
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
            options.Protocol = OtlpProtocol.Grpc;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = builder.Configuration["OTEL_SERVICE_NAME"] ?? "unknown"
            };
        });

    // Optional InfluxDB sink
    if (builder.Configuration.GetValue<bool>("Logging:InfluxDB:Enabled"))
    {
        loggerConfig.WriteTo.InfluxDB(
            builder.Configuration["Logging:InfluxDB:Uri"]!,
            builder.Configuration["Logging:InfluxDB:Database"]!
        );
    }
});
```

**Log Levels** (appsettings.Development.json):

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Information",
        "System.Net.Http.HttpClient": "Warning"
      }
    }
  }
}
```

---

### 3. Service Discovery

**HTTP Client Integration**:
- Resolves service names (e.g., `http://api`) to actual endpoints
- Reads `services__*` environment variables injected by AppHost
- Integrates with Polly resilience policies

**Implementation**:

```csharp
builder.Services.AddServiceDiscovery();

builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddServiceDiscovery();  // Enable service name resolution
});
```

**Usage in Application Code**:

```csharp
// Web/Services/MedicationService.cs
public class MedicationService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MedicationService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<Medication>> GetMedicationsAsync()
    {
        var client = _httpClientFactory.CreateClient();
        
        // "http://api" resolved via service discovery to http://localhost:5234
        var response = await client.GetAsync("http://api/medications");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<List<Medication>>();
    }
}
```

**Environment Variables Used**:
- `services__api__http__0` - API HTTP endpoint
- `services__api__https__0` - API HTTPS endpoint

---

### 4. Resilience Policies (Polly)

**Standard Resilience Handler**:
- Retry with exponential backoff
- Circuit breaker to prevent cascading failures
- Timeout for slow requests

**Implementation**:

```csharp
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler(options =>
    {
        // Retry policy
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;

        // Circuit breaker policy
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);

        // Timeout policy
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
    });
});
```

**What This Means**:
- If HTTP call fails, retry up to 3 times with exponential backoff (500ms, 1000ms, 2000ms)
- If 50% of calls fail within 10 seconds (min 5 calls), circuit opens for 5 seconds
- Individual attempts timeout after 30 seconds
- Total request (all retries) timeout after 60 seconds

---

### 5. Health Checks

**Default Health Checks**:
- Basic liveness check (always healthy)
- Database connectivity check (if EF Core detected)
- HTTP endpoint availability

**Implementation**:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Service is running"));

// If using Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("bloodtracker"));
});

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("bloodtracker")!);
```

**Endpoint Registration**:

```csharp
app.MapDefaultEndpoints();  // Registers /health, /alive, /ready
```

**Health Check Response**:

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "self": {
      "status": "Healthy",
      "description": "Service is running",
      "duration": "00:00:00.0000123"
    },
    "database": {
      "status": "Healthy",
      "description": "PostgreSQL connection successful",
      "duration": "00:00:00.0012345"
    }
  }
}
```

---

## Configuration Files

### Required NuGet Packages

```xml
<!-- API/BloodThinnerTracker.Api.csproj -->
<ItemGroup>
  <ProjectReference Include="..\BloodThinnerTracker.ServiceDefaults\BloodThinnerTracker.ServiceDefaults.csproj" />
</ItemGroup>
```

```xml
<!-- ServiceDefaults/BloodThinnerTracker.ServiceDefaults.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="8.10.0" />
  <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="10.0.0-rc.2" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.9.0" />
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />
  <PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.1.0" />
  <PackageReference Include="Serilog.Sinks.InfluxDB" Version="1.2.0" />
</ItemGroup>
```

### appsettings.Development.json

```json
{
  "Logging": {
    "InfluxDB": {
      "Enabled": false,
      "Uri": "http://localhost:8086",
      "Database": "bloodtracker_logs"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Information",
        "System.Net.Http.HttpClient": "Warning"
      }
    }
  }
}
```

---

## Usage Contract

### Mandatory Integration Steps

**Step 1**: Add ServiceDefaults project reference

```xml
<ProjectReference Include="..\BloodThinnerTracker.ServiceDefaults\BloodThinnerTracker.ServiceDefaults.csproj" />
```

**Step 2**: Call `AddServiceDefaults()` early in `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();  // MUST be called before other service registrations
```

**Step 3**: Map default endpoints

```csharp
var app = builder.Build();
app.MapDefaultEndpoints();  // Registers /health, /alive, /ready
```

### Optional Customization

**Add Custom Metrics**:

```csharp
// After builder.AddServiceDefaults()
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("BloodThinnerTracker.Medications");
    });

// In application code
public class MedicationService
{
    private static readonly Meter Meter = new("BloodThinnerTracker.Medications");
    private static readonly Counter<int> DosesLoggedCounter = Meter.CreateCounter<int>("doses_logged");

    public async Task LogDoseAsync(Medication medication)
    {
        // ... log dose ...
        DosesLoggedCounter.Add(1);
    }
}
```

**Add Custom Health Checks**:

```csharp
// After builder.AddServiceDefaults()
builder.Services.AddHealthChecks()
    .AddCheck<ApiHealthCheck>("api_connectivity")
    .AddCheck<FeatureFlagHealthCheck>("feature_flags");
```

---

## Best Practices

### 1. Service Defaults Integration

✅ **DO**:
- Call `builder.AddServiceDefaults()` FIRST (before other service registrations)
- Call `app.MapDefaultEndpoints()` to expose health checks
- Use ILogger<T> for structured logging (not Console.WriteLine)
- Use HttpClientFactory for all HTTP calls (not HttpClient directly)

❌ **DON'T**:
- Register OpenTelemetry manually (ServiceDefaults does this)
- Configure Serilog manually (ServiceDefaults does this)
- Hardcode service URLs (use service discovery)

### 2. Logging

✅ **DO**:
- Use structured logging: `_logger.LogInformation("User {UserId} logged dose {DoseId}", userId, doseId)`
- Log exceptions with full context
- Use appropriate log levels (Debug, Information, Warning, Error, Critical)

❌ **DON'T**:
- Use string interpolation: `_logger.LogInformation($"User {userId}...")`
- Log sensitive data (passwords, tokens, PII)
- Log in tight loops (use sampling)

### 3. HTTP Clients

✅ **DO**:
- Use IHttpClientFactory to create clients
- Use service names in URLs: `http://api/medications`
- Let Polly handle retries and circuit breaking

❌ **DON'T**:
- Create HttpClient instances directly (`new HttpClient()`)
- Hardcode URLs in client code
- Implement manual retry logic (Polly does this)

---

## Validation

### Build-Time Validation

- ✅ ServiceDefaults project reference exists
- ✅ Required NuGet packages installed
- ✅ `AddServiceDefaults()` called in Program.cs

### Runtime Validation

- ✅ OpenTelemetry traces appear in Aspire Dashboard
- ✅ Logs appear in Aspire Dashboard with structured fields
- ✅ Health checks return 200 OK at `/health`
- ✅ Service discovery resolves service names correctly
- ✅ HTTP resilience policies execute (retry on transient failures)

---

## Testing

### Unit Testing

```csharp
// Mock HttpClient with service discovery
[Fact]
public async Task GetMedications_ReturnsData()
{
    // Arrange
    var handler = new MockHttpMessageHandler(request =>
    {
        Assert.Equal("http://api/medications", request.RequestUri.ToString());
        return new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(new List<Medication>())
        };
    });

    var client = new HttpClient(handler);
    var service = new MedicationService(new TestHttpClientFactory(client));

    // Act
    var result = await service.GetMedicationsAsync();

    // Assert
    Assert.NotNull(result);
}
```

### Integration Testing

```csharp
// Test with Aspire.Hosting.Testing
[Fact]
public async Task ServiceDefaults_ConfiguresOpenTelemetry()
{
    // Arrange
    var appHost = await DistributedApplicationTestingBuilder
        .CreateAsync<Projects.BloodThinnerTracker_AppHost>();

    await using var app = await appHost.BuildAsync();
    await app.StartAsync();

    // Act
    var apiClient = app.CreateHttpClient("api");
    var response = await apiClient.GetAsync("/health");

    // Assert
    response.EnsureSuccessStatusCode();
    
    // Verify telemetry in dashboard
    var dashboardClient = app.CreateHttpClient("aspire-dashboard");
    var traces = await dashboardClient.GetFromJsonAsync<TraceResponse>("/traces");
    Assert.Contains(traces.Traces, t => t.ServiceName == "api");
}
```

---

## Summary

The ServiceDefaults contract provides:
- Single-method integration (`builder.AddServiceDefaults()`)
- Automatic OpenTelemetry configuration (traces, metrics, OTLP export)
- Structured logging with Serilog and multiple sinks
- Service discovery for HTTP clients
- Resilience policies (retry, circuit breaker, timeout)
- Health check endpoints

**Consuming Projects**: API and Web projects call `AddServiceDefaults()` to receive all configurations automatically. No manual OpenTelemetry, logging, or service discovery setup needed.
