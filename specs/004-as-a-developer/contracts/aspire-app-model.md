# Aspire Application Model Contract

**Feature**: 004 - Local Development Orchestration  
**Version**: 1.0  
**Date**: October 30, 2025

This document defines the API contract for defining and configuring the Aspire application model in the AppHost project.

---

## AppHost Program.cs Contract

### Required Imports

```csharp
using Aspire.Hosting;
using Projects;  // Auto-generated from .csproj references
```

### Application Model Builder

```csharp
// Create distributed application builder
var builder = DistributedApplication.CreateBuilder(args);

// Define resources (containers, services)
// ... resource definitions ...

// Build and run
var app = builder.Build();
await app.RunAsync();
```

---

## Resource Definition APIs

### 1. PostgreSQL Container

**Method**: `AddPostgres(string name)`

```csharp
IResourceBuilder<PostgresServerResource> AddPostgres(
    string name  // Resource name (e.g., "postgres")
)
```

**Extension Methods**:

```csharp
// Add a database to the PostgreSQL server
IResourceBuilder<PostgresDatabaseResource> AddDatabase(
    string databaseName  // Database name (e.g., "bloodtracker")
)

// Persist data in a Docker volume
IResourceBuilder<PostgresServerResource> WithDataVolume(
    string? volumeName = null  // Optional: custom volume name
)

// Add environment variables
IResourceBuilder<PostgresServerResource> WithEnvironment(
    string name,   // Variable name
    string value   // Variable value
)

// Configure container lifetime
IResourceBuilder<PostgresServerResource> WithLifetime(
    ContainerLifetime lifetime  // Persistent | Session
)
```

**Example**:

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()  // Default: aspire-{resourcename}-data
    .WithEnvironment("POSTGRES_PASSWORD", "Pass@word1")
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("bloodtracker");
```

---

### 2. ASP.NET Core Project

**Method**: `AddProject<TProject>(string name)`

```csharp
IResourceBuilder<ProjectResource> AddProject<TProject>(
    string name  // Resource name (e.g., "api", "web")
) where TProject : IProjectMetadata
```

**Extension Methods**:

```csharp
// Reference a database (injects connection string)
IResourceBuilder<ProjectResource> WithReference(
    IResourceBuilder<PostgresDatabaseResource> database,
    string? connectionName = null  // Optional: connection string name
)

// Reference another service (injects endpoint)
IResourceBuilder<ProjectResource> WithReference(
    IResourceBuilder<ProjectResource> service,
    string? endpointName = null  // Optional: specific endpoint name
)

// Configure HTTP endpoint
IResourceBuilder<ProjectResource> WithHttpEndpoint(
    int? port = null,      // Port number (null = dynamic)
    string? name = "http"  // Endpoint name
)

// Configure HTTPS endpoint
IResourceBuilder<ProjectResource> WithHttpsEndpoint(
    int? port = null,       // Port number (null = dynamic)
    string? name = "https"  // Endpoint name
)

// Add environment variable
IResourceBuilder<ProjectResource> WithEnvironment(
    string name,   // Variable name
    string value   // Variable value
)

// Configure replica count
IResourceBuilder<ProjectResource> WithReplicas(
    int replicas  // Number of instances
)
```

**Example**:

```csharp
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithReference(db)  // Inject connection string
    .WithHttpEndpoint(port: 5234, name: "http")
    .WithHttpsEndpoint(port: 7234, name: "https")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReplicas(1);

var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithReference(api)  // Inject API endpoint
    .WithHttpEndpoint(port: 5235)
    .WithHttpsEndpoint(port: 7235);
```

---

### 3. Generic Container

**Method**: `AddContainer(string name, string image, string? tag = null)`

```csharp
IResourceBuilder<ContainerResource> AddContainer(
    string name,   // Resource name (e.g., "influxdb")
    string image,  // Docker image (e.g., "influxdb")
    string? tag    // Image tag (e.g., "2.7-alpine")
)
```

**Extension Methods**:

```csharp
// Configure HTTP endpoint
IResourceBuilder<ContainerResource> WithHttpEndpoint(
    int? port = null,
    int? targetPort = null,  // Container internal port
    string? name = "http"
)

// Add data volume
IResourceBuilder<ContainerResource> WithDataVolume(
    string? volumeName = null
)

// Add environment variable
IResourceBuilder<ContainerResource> WithEnvironment(
    string name,
    string value
)
```

**Example**:

```csharp
var influx = builder.AddContainer("influxdb", "influxdb", "2.7-alpine")
    .WithHttpEndpoint(port: 8086, targetPort: 8086)
    .WithDataVolume()
    .WithEnvironment("INFLUXDB_DB", "bloodtracker_logs");
```

---

## Environment Variable Injection

### Connection String Injection

**Pattern**: `ConnectionStrings__{name}`

```csharp
// AppHost definition
var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("bloodtracker");
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithReference(db);

// API receives environment variable:
// ConnectionStrings__bloodtracker=Host=localhost;Port=5432;Database=bloodtracker;Username=postgres;Password=...
```

**Consuming in API**:

```csharp
// API/Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("bloodtracker"));
});
```

---

### Service Endpoint Injection

**Pattern**: `services__{servicename}__{protocol}__{index}`

```csharp
// AppHost definition
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithHttpEndpoint(port: 5234);

var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithReference(api);

// Web receives environment variables:
// services__api__http__0=http://localhost:5234
// services__api__https__0=https://localhost:7234  (if HTTPS configured)
```

**Consuming in Web**:

```csharp
// Web/Program.cs
builder.Services.AddHttpClient("api", client =>
{
    // Service discovery resolves "http://api" using environment variable
    client.BaseAddress = new Uri("http://api");
});
```

---

### OpenTelemetry Configuration Injection

**Automatic Variables**:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317  # Aspire OTLP collector
OTEL_SERVICE_NAME={resource-name}                   # Service name from AppHost
```

**Consuming in ServiceDefaults**:

```csharp
// ServiceDefaults.cs
builder.Services.AddOpenTelemetry()
    .UseOtlpExporter();  // Reads OTEL_EXPORTER_OTLP_ENDPOINT automatically
```

---

## Resource Lifecycle

### Container Lifetime Options

```csharp
public enum ContainerLifetime
{
    Persistent,  // Keep running after AppHost stops
    Session      // Stop when AppHost stops
}
```

**Usage**:

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent);  // Database persists
```

### Service Lifecycle

**Services** (ASP.NET Core projects):
- Start when AppHost starts
- Stop when AppHost stops
- Support hot reload for code changes
- Restart on explicit command

**Containers**:
- Pull image if not cached
- Start when AppHost starts
- Persistent: Keep running after AppHost stops
- Session: Stop when AppHost stops

---

## Health Check Contract

### Endpoint Convention

All services SHOULD implement: `GET /health`

**Implementation**:

```csharp
// API/Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("bloodtracker")!);

var app = builder.Build();
app.MapHealthChecks("/health");
```

**Aspire Integration**:

Aspire Dashboard automatically discovers `/health` endpoints and polls them every 10 seconds.

---

## Dashboard Configuration

### Default Configuration

**Dashboard automatically starts** when AppHost runs:

```csharp
var builder = DistributedApplication.CreateBuilder(args);
// ... define resources ...
builder.Build().Run();  // Dashboard starts on http://localhost:15000
```

### Custom Dashboard Port

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Configure dashboard port via environment variable or launchSettings.json
// ASPIRE_DASHBOARD_PORT=16000

builder.Build().Run();
```

---

## Best Practices

### 1. Resource Naming

✅ **DO**:
- Use lowercase names: `api`, `web`, `postgres`
- Use hyphens for multi-word: `api-gateway`, `user-service`
- Keep names short and descriptive

❌ **DON'T**:
- Use uppercase: `API`, `Web`
- Use underscores: `api_service`
- Use special characters: `api.service`, `api@v1`

### 2. Port Assignment

✅ **DO**:
- Use explicit ports for predictable debugging (5234, 5235, etc.)
- Reserve port ranges per project (APIs: 5234-5239, Web: 5235-5239)
- Document port assignments

❌ **DON'T**:
- Use dynamic ports if debugging requires predictable URLs
- Use well-known ports (<1024) that require admin privileges
- Reuse ports across services

### 3. Environment Variables

✅ **DO**:
- Use `WithEnvironment()` for configuration
- Use secrets manager for sensitive data in production
- Document all custom environment variables

❌ **DON'T**:
- Hardcode secrets in AppHost code
- Use environment variables for complex objects (use appsettings.json)

### 4. Dependencies

✅ **DO**:
- Reference databases before services
- Chain dependencies explicitly (`.WithReference()`)
- Keep dependency graph simple (avoid deep nesting)

❌ **DON'T**:
- Create circular dependencies
- Reference undefined resources
- Skip intermediate dependencies

---

## Validation

### Build-Time Validation

Aspire validates the application model at build time:

- ✅ Resource names are unique
- ✅ Referenced resources exist
- ✅ Port conflicts detected
- ✅ Circular dependencies prevented

### Runtime Validation

Aspire validates at startup:

- ✅ Docker is running (for containers)
- ✅ Ports are available
- ✅ Project files exist
- ✅ Container images can be pulled

---

## Example: Complete AppHost

```csharp
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Define PostgreSQL container with persistent data
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("bloodtracker");

// Define API service with database reference
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithReference(db)
    .WithHttpEndpoint(port: 5234, name: "http")
    .WithHttpsEndpoint(port: 7234, name: "https")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

// Define Web service with API reference
var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithReference(api)
    .WithHttpEndpoint(port: 5235)
    .WithHttpsEndpoint(port: 7235)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

// Optional: InfluxDB for log persistence
if (builder.Configuration.GetValue<bool>("Logging:InfluxDB:Enabled"))
{
    var influx = builder.AddContainer("influxdb", "influxdb", "2.7-alpine")
        .WithHttpEndpoint(port: 8086)
        .WithDataVolume()
        .WithEnvironment("INFLUXDB_DB", "bloodtracker_logs")
        .WithLifetime(ContainerLifetime.Persistent);
}

// Build and run (Dashboard starts automatically)
var app = builder.Build();
await app.RunAsync();
```

---

## Summary

This contract defines:
- Resource definition APIs (`AddPostgres`, `AddProject`, `AddContainer`)
- Configuration extension methods (`WithReference`, `WithHttpEndpoint`, etc.)
- Environment variable injection patterns
- Health check conventions
- Best practices and validation rules

**Consuming Projects**: API and Web projects consume injected configuration via `IConfiguration` and service discovery.
