# API Versioning Strategy

## Overview

This document defines the versioning strategy for the Blood Thinner Tracker API, including how to manage breaking changes, maintain backward compatibility, and deprecate old versions.

## Current State

- **Current Version**: v1 (`/api/v1/...`)
- **Versioning Scheme**: URL path versioning (`api/v{major}/...`)
- **Implementation**: Microsoft.AspNetCore.Mvc.Versioning

## Versioning Principles

### 1. Semantic Versioning for API

| Change Type | Version Impact | Client Action Required |
|-------------|----------------|------------------------|
| Bug fix (no contract change) | None | None |
| New optional field in response | None (additive) | None |
| New optional query parameter | None (additive) | None |
| New endpoint | None (additive) | None |
| New required field in request | **Major bump** | Update client |
| Field renamed/removed | **Major bump** | Update client |
| Field type changed | **Major bump** | Update client |
| Endpoint path changed | **Major bump** | Update client |
| Authentication change | **Major bump** | Update client |

### 2. URL Path Versioning

We use URL path versioning (`/api/v1/...`, `/api/v2/...`) because:
- Explicit and visible in logs/debugging
- Easy to route at infrastructure level (load balancers, API gateways)
- Clear for client developers
- Supported by OpenAPI tooling (separate specs per version)

### 3. Version Lifecycle

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Active    │───▶│  Deprecated │───▶│   Sunset    │───▶│   Removed   │
│  (Current)  │    │ (6 months)  │    │  (Warning)  │    │   (Gone)    │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
```

| Status | Description | Duration |
|--------|-------------|----------|
| **Active** | Fully supported, receives bug fixes | Until next major version |
| **Deprecated** | Functional but discouraged; returns `Deprecation` header | 6 months minimum |
| **Sunset** | Returns `410 Gone` with migration instructions | 1 month |
| **Removed** | Endpoint no longer exists | Permanent |

---

## Making Changes

### Non-Breaking Changes (No Version Bump)

These can be added to the current version:

```csharp
// SAFE: Adding optional response field
public class MedicationResponse
{
    public Guid PublicId { get; set; }
    public string Name { get; set; }
    public string? NewOptionalField { get; set; }  // ✓ Safe to add
}

// SAFE: Adding optional query parameter
[HttpGet]
public async Task<ActionResult<List<MedicationResponse>>> GetMedications(
    [FromQuery] bool includeInactive = false,
    [FromQuery] string? newOptionalFilter = null)  // ✓ Safe to add
```

### Breaking Changes (Requires New Version)

When a breaking change is needed:

#### Step 1: Create the New Version Controller

```csharp
// File: Controllers/V2/MedicationsController.cs
namespace BloodThinnerTracker.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class MedicationsController : ControllerBase
{
    // New implementation with breaking changes
}
```

#### Step 2: Keep the Old Version (Deprecated)

```csharp
// File: Controllers/V1/MedicationsController.cs (renamed from Controllers/)
namespace BloodThinnerTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0", Deprecated = true)]  // Mark as deprecated
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class MedicationsController : ControllerBase
{
    // Original implementation - maintained for backward compatibility
}
```

#### Step 3: Add Deprecation Headers

```csharp
// In Program.cs or a filter
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1"))
    {
        context.Response.Headers.Append("Deprecation", "true");
        context.Response.Headers.Append("Sunset", "2025-12-31T00:00:00Z");
        context.Response.Headers.Append("Link", 
            "</api/v2/medications>; rel=\"successor-version\"");
    }
    await next();
});
```

#### Step 4: Update OpenAPI Documentation

```csharp
// In Program.cs - generate separate OpenAPI docs per version
builder.Services.AddOpenApi("v1", options => { /* v1 config */ });
builder.Services.AddOpenApi("v2", options => { /* v2 config */ });

// Map both
app.MapOpenApi("/openapi/v1.json", "v1");
app.MapOpenApi("/openapi/v2.json", "v2");
```

#### Step 5: Regenerate Clients

```powershell
# Regenerate v1 client (for existing consumers)
kiota generate -l CSharp -d https://localhost:7049/openapi/v1.json -o src/BloodThinnerTracker.Clients/V1 -n BloodThinnerTracker.Clients.V1

# Generate v2 client (for new consumers)
kiota generate -l CSharp -d https://localhost:7049/openapi/v2.json -o src/BloodThinnerTracker.Clients/V2 -n BloodThinnerTracker.Clients.V2
```

---

## Directory Structure for Multi-Version

```
src/BloodThinnerTracker.Api/
├── Controllers/
│   ├── V1/                          # Deprecated version
│   │   ├── MedicationsController.cs
│   │   ├── INRController.cs
│   │   └── AuthController.cs
│   └── V2/                          # Current version
│       ├── MedicationsController.cs
│       ├── INRController.cs
│       └── AuthController.cs
├── Models/
│   ├── V1/                          # Request/Response models for v1
│   └── V2/                          # Request/Response models for v2
└── Program.cs
```

---

## Client Migration Guide Template

When deprecating a version, provide a migration guide:

```markdown
# Migrating from API v1 to v2

## Timeline
- **v1 Deprecated**: 2025-07-01
- **v1 Sunset**: 2025-12-31
- **v1 Removed**: 2026-01-31

## Breaking Changes

### 1. Medication endpoint changes
- `GET /api/v1/medications` → `GET /api/v2/medications`
- Response field `Dosage` (decimal) → `DosageAmount` (decimal) + `DosageUnit` (string)

### 2. Authentication changes
- Header `X-Api-Key` removed
- Use `Authorization: Bearer {token}` instead

## Code Changes Required

### Before (v1)
```csharp
var meds = await client.GetFromJsonAsync<List<MedicationV1>>("api/v1/medications");
Console.WriteLine(meds[0].Dosage);
```

### After (v2)
```csharp
var meds = await client.GetFromJsonAsync<List<MedicationV2>>("api/v2/medications");
Console.WriteLine($"{meds[0].DosageAmount} {meds[0].DosageUnit}");
```
```

---

## Configuration

### appsettings.json

```json
{
  "ApiVersioning": {
    "DefaultVersion": "1.0",
    "AssumeDefaultVersionWhenUnspecified": true,
    "ReportApiVersions": true,
    "DeprecatedVersions": ["1.0"],
    "SunsetDates": {
      "1.0": "2025-12-31"
    }
  }
}
```

### Required Configuration (No Fallbacks)

Per project policy, API clients **must** configure their base URL explicitly:

```csharp
// ✗ BAD - Don't use fallback URLs
BaseAddress = new Uri(config["ApiBaseUrl"] ?? "https://localhost:7234")

// ✓ GOOD - Fail fast if not configured
var apiBaseUrl = config["ApiBaseUrl"]
    ?? throw new InvalidOperationException(
        "ApiBaseUrl configuration is required.");
```

---

## Checklist: Releasing a New API Version

- [ ] Document all breaking changes
- [ ] Create new versioned controller namespace (`Controllers/V2/`)
- [ ] Mark old version as deprecated (`[ApiVersion("1.0", Deprecated = true)]`)
- [ ] Add deprecation response headers for old version
- [ ] Generate separate OpenAPI specs for each version
- [ ] Regenerate client libraries for both versions
- [ ] Update client projects to reference new version
- [ ] Write migration guide for consumers
- [ ] Set sunset date (minimum 6 months from deprecation)
- [ ] Add monitoring/alerting for old version usage
- [ ] Communicate timeline to all API consumers

---

## Related Documents

- [OpenAPI Specification](../contracts/openapi.yaml)
- [Authentication Guide](../AUTHENTICATION_TESTING_GUIDE.md)
- [Kiota Client Generation](../../tools/clients/generate-client.ps1)
