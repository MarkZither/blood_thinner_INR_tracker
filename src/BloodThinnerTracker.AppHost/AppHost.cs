var builder = DistributedApplication.CreateBuilder(args);

// Add the API project (SQLite database is embedded in the project)
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithHttpsEndpoint(port: 7234, name: "api-https")
    .WithHttpEndpoint(port: 5234, name: "api-http");

// Add the Blazor Web project with reference to the API
var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithHttpsEndpoint(port: 7235, name: "web-https")
    .WithHttpEndpoint(port: 5235, name: "web-http")
    .WithReference(api)
    .WithEnvironment("ApiBaseUrl", api.GetEndpoint("api-https"));

builder.Build().Run();
