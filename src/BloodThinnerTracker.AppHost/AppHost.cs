var builder = DistributedApplication.CreateBuilder(args);

// Determine container lifetime based on environment
// In tests, use Session lifetime for ephemeral containers that get cleaned up
// In production, use Persistent lifetime to preserve data
var containerLifetime = Environment.GetEnvironmentVariable("ASPIRE_CONTAINER_LIFETIME") == "Session"
    ? ContainerLifetime.Session
    : ContainerLifetime.Persistent;

// Create a parameter for the PostgreSQL password
// This ensures the same password is used in BOTH the connection string AND the container
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

builder.Build().Run();
