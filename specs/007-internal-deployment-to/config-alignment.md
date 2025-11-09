# Config alignment: Example keys → IOptions bindings

This document maps the example configuration keys used in deployment templates and example files to the strongly-typed options classes that must be used in application code, per the project's constitution (Configuration Access & Options Pattern).

Purpose: give implementers an explicit mapping so generated example `deployment.config.example.json` files and any sample env vars align 1:1 with `IOptions<T>` bindings.

Example mapping

- App: API options
  - Key: `Api:BaseUrl` → IOptions class: `ApiOptions` (property: `BaseUrl : string`)
  - Key: `Api:HealthPath` → IOptions class: `ApiOptions` (property: `HealthPath : string`)

- App: Logging options
  - Key: `Logging:Path` → IOptions class: `LoggingOptions` (property: `Path : string`)
  - Key: `Logging:RotateDays` → IOptions class: `LoggingOptions` (property: `RotateDays : int`)

- App: Deployment/Runtime options (used by scripts & runtime binding)
  - Key: `Deployment:ArtifactPath` → IOptions class: `DeploymentOptions` (property: `ArtifactPath : string`)
  - Key: `Deployment:ServiceName` → IOptions class: `DeploymentOptions` (property: `ServiceName : string`)
  - Key: `Deployment:RetentionPath` → IOptions class: `DeploymentOptions` (property: `RetentionPath : string`)

Notes and guidance

- Use `IOptions<T>` binding with POCO classes named above. Example in Program.cs:

```csharp
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));
```

- Do not use magic strings in application code for configuration keys; use the options classes instead.
- Secrets (if any) referenced by these options MUST be sourced from environment variables or user secrets (do not commit secrets in example files).

If additional config keys are added in tasks or templates, extend this document with the exact IOptions type and property names.
