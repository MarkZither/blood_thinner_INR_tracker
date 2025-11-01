using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Feature flag: Enable InfluxDB for time-series metrics storage (optional)
var enableInfluxDb = builder.Configuration.GetValue<bool>("Features:EnableInfluxDB");

// Determine container lifetime based on environment
// In tests, use Session lifetime for ephemeral containers that get cleaned up
// In production, use Persistent lifetime to preserve data
var containerLifetime = Environment.GetEnvironmentVariable("ASPIRE_CONTAINER_LIFETIME") == "Session"
    ? ContainerLifetime.Session
    : ContainerLifetime.Persistent;

// Create a parameter for the PostgreSQL password
// Supports multiple configuration sources (12-factor app principle):
// 1. Environment variable: POSTGRES_PASSWORD → Parameters__postgres-password (auto-mapped)
// 2. Environment variable: Parameters__postgres-password (Aspire convention)
// 3. appsettings.json: Parameters:postgres-password
// 4. Command line: --Parameters:postgres-password=<value>
// Environment variables take precedence over appsettings.json

// Support POSTGRES_PASSWORD environment variable (common convention)
var postgresPasswordFromEnv = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
if (!string.IsNullOrEmpty(postgresPasswordFromEnv))
{
    // Map to Aspire parameter convention
    Environment.SetEnvironmentVariable("Parameters__postgres-password", postgresPasswordFromEnv);
}

var postgresPassword = builder.AddParameter("postgres-password", secret: true);

// Add PostgreSQL container with configurable lifetime
// Note: Using default 'postgres' user (PostgreSQL standard) to avoid initialization errors
var postgresBuilder = builder.AddPostgres("postgres", password: postgresPassword)
    .WithLifetime(containerLifetime)
    .WithEnvironment("POSTGRES_DB", "bloodtracker")
    .PublishAsConnectionString();

// Only add persistent data volume if using persistent lifetime
if (containerLifetime == ContainerLifetime.Persistent)
{
    postgresBuilder = postgresBuilder.WithDataVolume();
}

var postgres = postgresBuilder;

// Add the bloodtracker database
var bloodtrackerDb = postgres.AddDatabase("bloodtracker");

// Add the API project with PostgreSQL database reference
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithHttpsEndpoint(port: 7234, name: "api-https")
    .WithHttpEndpoint(port: 5234, name: "api-http")
    .WithReference(bloodtrackerDb);

// Add the Blazor Web project with reference to the API
var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithHttpsEndpoint(port: 7235, name: "web-https")
    .WithHttpEndpoint(port: 5235, name: "web-http")
    .WithReference(api)
    .WithEnvironment("ApiBaseUrl", api.GetEndpoint("api-https"));

// Optional: Add InfluxDB for time-series metrics storage
// Enable by setting Features:EnableInfluxDB to true in appsettings.json
if (enableInfluxDb)
{
    var influxDbPassword = builder.AddParameter("influxdb-password", secret: true);

    var influxDb = builder.AddContainer("influxdb", "influxdb", "2.7")
        .WithLifetime(containerLifetime)
        .WithEnvironment("DOCKER_INFLUXDB_INIT_MODE", "setup")
        .WithEnvironment("DOCKER_INFLUXDB_INIT_USERNAME", "admin")
        .WithEnvironment("DOCKER_INFLUXDB_INIT_PASSWORD", influxDbPassword)
        .WithEnvironment("DOCKER_INFLUXDB_INIT_ORG", "bloodtracker")
        .WithEnvironment("DOCKER_INFLUXDB_INIT_BUCKET", "metrics")
        .WithHttpEndpoint(port: 8086, name: "influxdb-http");

    // Add persistent data volume if using persistent lifetime
    if (containerLifetime == ContainerLifetime.Persistent)
    {
        influxDb = influxDb.WithBindMount("influxdb-data", "/var/lib/influxdb2");
    }
}

builder.Build().Run();
