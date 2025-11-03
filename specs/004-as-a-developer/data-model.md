# Feature 004: Aspire Application Model (Data Model)

**Phase**: 1 (Design)  
**Date**: October 30, 2025

This document defines the "data model" for Feature 004, which is the **Aspire Application Model** - the topology of services, containers, and their relationships in the local development orchestration.

---

## Application Topology

```
┌─────────────────────────────────────────────────────────────┐
│                    Aspire AppHost                            │
│                  (Orchestrator)                              │
└─────────────────────────────────────────────────────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        │                 │                 │
        ▼                 ▼                 ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  PostgreSQL  │  │     API      │  │     Web      │
│  Container   │  │   Service    │  │   Service    │
│              │  │              │  │              │
│ Port: 5432   │  │ Port: 5234   │  │ Port: 5235   │
│              │  │ Port: 7234   │  │ Port: 7235   │
└──────────────┘  └──────────────┘  └──────────────┘
        │                 │                 │
        │                 │                 │
        └────────┬────────┘                 │
                 │                          │
                 │     Connection String    │
                 │                          │
                 │          ┌───────────────┘
                 │          │
                 │          │ Service Reference
                 │          │ (http://api)
                 │          │
                 ▼          ▼
        ┌─────────────────────┐
        │  Aspire Dashboard    │
        │  (Observability)     │
        │                      │
        │  Port: 15000         │
        └─────────────────────┘
                 │
                 │ OTLP
                 │ (Logs, Metrics, Traces)
                 │
                 ▼
        ┌─────────────────────┐
        │  Optional: InfluxDB  │
        │  Container           │
        │  (Log Persistence)   │
        │                      │
        │  Port: 8086          │
        └─────────────────────┘
```

---

## Resource Definitions

### 1. PostgreSQL Container Resource

**Resource Name**: `postgres`  
**Type**: Container (PostgreSQL 16)  
**Purpose**: Development database for testing PostgreSQL-specific features  
**Image**: `postgres:16-alpine`

**Configuration**:
```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()  // Named volume: aspire-postgres-data
    .WithEnvironment("POSTGRES_PASSWORD", "Pass@word1")
    .WithEnvironment("POSTGRES_USER", "bloodtracker")
    .WithEnvironment("POSTGRES_DB", "bloodtracker_dev")
    .WithHttpHealthCheck("/health")
    .WithLifetime(ContainerLifetime.Persistent);  // Keep running between restarts
```

**Ports**:
- Container: 5432 → Host: Dynamic (managed by Aspire)

**Volume**:
- Name: `aspire-postgres-data` (Docker named volume)
- Mount: `/var/lib/postgresql/data`
- Persistence: Data retained across container restarts

**Health Check**:
- Endpoint: TCP connection on port 5432
- Interval: 5 seconds
- Timeout: 3 seconds
- Retries: 3

**Environment Variables Injected to API**:
```
ConnectionStrings__bloodtracker=Host=localhost;Port={dynamic_port};Database=bloodtracker_dev;Username=bloodtracker;Password=Pass@word1
```

---

### 2. API Service Resource

**Resource Name**: `api`  
**Type**: ASP.NET Core Web API Project  
**Purpose**: Backend REST API for blood thinner tracking  
**Project Path**: `src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj`

**Configuration**:
```csharp
var db = postgres.AddDatabase("bloodtracker");

var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithReference(db)  // Inject connection string
    .WithHttpEndpoint(port: 5234, name: "http")
    .WithHttpsEndpoint(port: 7234, name: "https")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReplicas(1);
```

**Endpoints**:
- HTTP: `http://localhost:5234`
- HTTPS: `https://localhost:7234`

**Dependencies**:
- PostgreSQL database (via `db` reference)
- ServiceDefaults (OpenTelemetry, Serilog, Polly)

**Environment Variables Received**:
```
ConnectionStrings__bloodtracker=Host=localhost;Port=5432;Database=bloodtracker_dev;Username=bloodtracker;Password=Pass@word1
ASPNETCORE_ENVIRONMENT=Development
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_SERVICE_NAME=api
```

**Health Check**:
- Endpoint: `/health`
- Interval: 10 seconds
- Response: `{"status": "Healthy", "totalDuration": "00:00:00.0123456"}`

---

### 3. Web Service Resource

**Resource Name**: `web`  
**Type**: Blazor Server Project  
**Purpose**: Frontend web application  
**Project Path**: `src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj`

**Configuration**:
```csharp
var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithReference(api)  // Inject API endpoint
    .WithHttpEndpoint(port: 5235, name: "http")
    .WithHttpsEndpoint(port: 7235, name: "https")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReplicas(1);
```

**Endpoints**:
- HTTP: `http://localhost:5235`
- HTTPS: `https://localhost:7235`

**Dependencies**:
- API service (via `api` reference)
- ServiceDefaults (OpenTelemetry, Serilog, Service Discovery, Polly)

**Environment Variables Received**:
```
services__api__http__0=http://localhost:5234
services__api__https__0=https://localhost:7234
ASPNETCORE_ENVIRONMENT=Development
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_SERVICE_NAME=web
```

**Service Discovery Resolution**:
```csharp
// In Web project code
var apiClient = httpClientFactory.CreateClient("api");
apiClient.BaseAddress = new Uri("http://api");  // Resolved to http://localhost:5234
```

**Health Check**:
- Endpoint: `/health`
- Interval: 10 seconds

---

### 4. Aspire Dashboard Resource

**Resource Name**: N/A (implicit)  
**Type**: Built-in Aspire component  
**Purpose**: Observability dashboard for logs, metrics, traces  
**Automatic**: Started automatically by AppHost

**Configuration**:
```csharp
// No explicit configuration needed - dashboard auto-starts
var builder = DistributedApplication.CreateBuilder(args);
// ... define resources ...
builder.Build().Run();  // Dashboard starts automatically
```

**Endpoints**:
- Dashboard UI: `http://localhost:15000`
- OTLP Receiver: `http://localhost:4317` (gRPC)
- OTLP HTTP Receiver: `http://localhost:4318` (HTTP)

**Features**:
- Real-time log streaming from all services
- Distributed trace visualization
- Metrics dashboard (CPU, memory, request rates)
- Resource health status
- Environment variable inspection

**No Authentication**: Dashboard is localhost-only, no authentication required for local development.

---

### 5. Optional: InfluxDB Container Resource

**Resource Name**: `influxdb` (optional)  
**Type**: Container (InfluxDB 2.7)  
**Purpose**: Long-term log storage and advanced querying  
**Image**: `influxdb:2.7-alpine`

**Configuration** (Optional - enabled via configuration):
```csharp
if (builder.Configuration.GetValue<bool>("Logging:InfluxDB:Enabled"))
{
    var influx = builder.AddContainer("influxdb", "influxdb", "2.7-alpine")
        .WithDataVolume()
        .WithHttpEndpoint(port: 8086, name: "http")
        .WithEnvironment("INFLUXDB_DB", "bloodtracker_logs")
        .WithEnvironment("INFLUXDB_ADMIN_USER", "admin")
        .WithEnvironment("INFLUXDB_ADMIN_PASSWORD", "admin123")
        .WithLifetime(ContainerLifetime.Persistent);
}
```

**Ports**:
- HTTP API: `http://localhost:8086`

**Usage**: Serilog sinks write structured logs to InfluxDB for historical analysis.

---

## Service Relationships

### Dependency Graph

```
PostgreSQL ──┐
             ├──> API ──> Web
             │            │
InfluxDB ────┼────────────┘
             │
             └──> Aspire Dashboard (monitors all)
```

### Reference Types

| From | To | Reference Type | Injected Configuration |
|------|-----|----------------|------------------------|
| API | PostgreSQL | Database Reference | `ConnectionStrings__bloodtracker` |
| Web | API | Service Reference | `services__api__http__0` |
| API | InfluxDB | Direct Connection | `Logging:InfluxDB:Uri` (manual config) |
| All | Dashboard | OTLP Exporter | `OTEL_EXPORTER_OTLP_ENDPOINT` |

---

## Resource Naming Conventions

**Service Names** (lowercase, hyphens for multi-word):
- `api` (not `Api` or `API`)
- `web` (not `Web`)
- `postgres` (not `PostgreSQL` or `db`)
- `influxdb` (not `InfluxDB` or `logs`)

**Endpoint Names** (lowercase):
- `http` (primary HTTP endpoint)
- `https` (primary HTTPS endpoint)
- `health` (health check endpoint)

**Environment Variable Pattern**:
- Connection strings: `ConnectionStrings__{name}`
- Service endpoints: `services__{servicename}__{protocol}__{index}`
- OTEL: `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME`

---

## Configuration Injection Flow

### 1. AppHost Defines Topology

```csharp
// AppHost/Program.cs
var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("bloodtracker");
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithReference(db);
var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithReference(api);
```

### 2. Aspire Injects Environment Variables

**API receives**:
```bash
ConnectionStrings__bloodtracker=Host=localhost;Port=5432;...
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_SERVICE_NAME=api
```

**Web receives**:
```bash
services__api__http__0=http://localhost:5234
services__api__https__0=https://localhost:7234
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_SERVICE_NAME=web
```

### 3. ServiceDefaults Reads Configuration

```csharp
// ServiceDefaults.cs
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    // OpenTelemetry reads OTEL_* environment variables
    builder.Services.AddOpenTelemetry()
        .WithTracing(...)
        .WithMetrics(...);

    // Service discovery reads services__* environment variables
    builder.Services.AddServiceDiscovery();

    // Serilog reads OTEL endpoint for log export
    builder.Services.AddSerilog(...);

    return builder;
}
```

### 4. Application Code Uses Injected Configuration

```csharp
// API/Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
{
    // Connection string automatically resolved from ConnectionStrings__bloodtracker
    options.UseNpgsql(builder.Configuration.GetConnectionString("bloodtracker"));
});

// Web/Program.cs
builder.Services.AddHttpClient("api", client =>
{
    // Service discovery resolves "http://api" to actual endpoint
    client.BaseAddress = new Uri("http://api");
});
```

---

## State Management

### Container State

**Persistent State** (survives restarts):
- PostgreSQL data (`aspire-postgres-data` volume)
- InfluxDB data (`aspire-influxdb-data` volume)

**Ephemeral State** (lost on restart):
- Aspire Dashboard logs/metrics (in-memory only)
- Container logs (use `docker logs` for historical)

### Service State

**Stateless Services**:
- API (no in-memory state, all data in database)
- Web (Blazor Server maintains circuit state, but sessions are ephemeral)

**State Recovery**:
- On service restart, database connections re-establish automatically
- Blazor Server circuits reconnect (SignalR auto-reconnect)
- No manual state recovery needed

---

## Validation Rules

### Resource Naming Rules

1. ✅ Service names MUST be lowercase alphanumeric + hyphens
2. ✅ Service names MUST NOT contain spaces or special characters
3. ✅ Service names MUST be unique within the application model
4. ✅ Endpoint names MUST be lowercase

### Dependency Rules

1. ✅ Circular dependencies are NOT allowed (A→B→C→A)
2. ✅ Database references MUST come before service references
3. ✅ Service references MUST point to defined services
4. ✅ Container resources MUST be defined before being referenced

### Port Assignment Rules

1. ✅ Explicit ports MUST NOT conflict with other services
2. ✅ Explicit ports MUST be in range 1024-65535
3. ✅ Dashboard port (15000) is reserved
4. ✅ OTLP ports (4317, 4318) are reserved

---

## Health Check Contract

All services MUST implement a `/health` endpoint that returns:

**Success Response** (200 OK):
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Healthy",
      "description": "PostgreSQL connection successful",
      "duration": "00:00:00.0012345"
    }
  }
}
```

**Failure Response** (503 Service Unavailable):
```json
{
  "status": "Unhealthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Unhealthy",
      "description": "Unable to connect to PostgreSQL",
      "exception": "Npgsql.NpgsqlException: Connection refused",
      "duration": "00:00:00.0012345"
    }
  }
}
```

---

## Summary

This application model defines:
- 2-3 service resources (API, Web, optional Mobile)
- 1-2 container resources (PostgreSQL, optional InfluxDB)
- 1 implicit dashboard resource (Aspire Dashboard)
- Clear dependency relationships and configuration injection patterns
- Naming conventions and validation rules
- Health check contracts for monitoring

**Next Steps**: Generate API contracts in `contracts/` directory.
