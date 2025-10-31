var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL container with persistent data volume
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

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
