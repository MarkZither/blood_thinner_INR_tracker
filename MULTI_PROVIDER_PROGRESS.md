# Multi-Provider Database Architecture - Implementation Progress

**Date**: November 1, 2025  
**Branch**: 004-as-a-developer  
**Status**: ✅ Foundation Complete - Ready for Provider Implementations

## Architecture Overview

We've successfully implemented the **Microsoft-recommended multi-project pattern** for EF Core with multiple database providers.

### Project Structure

```
src/
├── BloodThinnerTracker.Data.Shared/          ✅ COMPLETE
│   ├── IApplicationDbContext.cs              (Interface for DI)
│   ├── ICurrentUserService.cs                (Clean abstraction for current user)
│   └── ApplicationDbContextBase.cs           (Provider-agnostic base context)
│
├── BloodThinnerTracker.Data.PostgreSQL/      🔄 NEXT: Implement
│   ├── ApplicationDbContext.cs               (Inherits base + Npgsql config)
│   ├── DesignTimeDbContextFactory.cs         (For migrations)
│   └── Migrations/                           (PostgreSQL-specific SQL)
│
├── BloodThinnerTracker.Data.SqlServer/       🔄 NEXT: Implement
│   ├── ApplicationDbContext.cs               (Inherits base + SQL Server config)
│   ├── DesignTimeDbContextFactory.cs         (For migrations)
│   └── Migrations/                           (SQL Server-specific SQL)
│
└── BloodThinnerTracker.Data.SQLite/          🔄 NEXT: Implement
    ├── ApplicationDbContext.cs               (Inherits base + SQLite config)
    ├── DesignTimeDbContextFactory.cs         (For migrations)
    └── Migrations/                           (SQLite-specific SQL)
```

## Key Design Decisions

### 1. **Clean Separation of Concerns** ✅

**Problem Solved**: Original design had data layer depending on `IHttpContextAccessor` (ASP.NET Core HTTP).

**Solution**: Created `ICurrentUserService` abstraction:
- Data layer defines the interface
- API layer implements it using HTTP context
- No ASP.NET Core dependencies in data projects

**Benefits**:
- Data layer is infrastructure-agnostic
- Can be used in console apps, background jobs, tests
- Proper layered architecture

### 2. **Provider-Specific Migrations** ✅

**Why Multiple Projects?**

EF Core generates **hardcoded provider-specific SQL** in migrations:

```csharp
// PostgreSQL migration:
.Annotation("Npgsql:ValueGenerationStrategy", 
    NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
type: "integer"
CHECK ("INRValue" >= 0.5)  // Double quotes

// SQL Server migration:
.Annotation("SqlServer:Identity", "1, 1")
type: "int"  
CHECK ([INRValue] >= 0.5)  // Brackets

// SQLite migration:
type: "INTEGER"
CHECK (INRValue >= 0.5)    // No quotes
```

**Result**: Each provider gets correct SQL syntax, types, and constraints.

### 3. **Medical Data Safety** ✅

All CHECK constraints are **medically critical**:

```sql
-- Life-threatening if wrong:
CHECK ("INRValue" >= 0.5 AND "INRValue" <= 8.0)

-- Overdose prevention:
CHECK ("Dosage" > 0 AND "Dosage" <= 1000)

-- Schedule safety:
CHECK ("IntervalDays" >= 1 AND "IntervalDays" <= 365)
```

These constraints MUST work correctly on all providers for patient safety.

## Dependencies (All Correct)

### BloodThinnerTracker.Data.Shared
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0-rc.2.25502.107" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.0-rc.2.25502.107" />
<PackageReference Include="Microsoft.AspNetCore.DataProtection.EntityFrameworkCore" Version="10.0.0-rc.2.25502.107" />
```
✅ **NO ASP.NET Core HTTP dependencies** - Clean architecture

### BloodThinnerTracker.Data.PostgreSQL
```xml
<ProjectReference Include="..\BloodThinnerTracker.Data.Shared\..." />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0-rc.2" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0-rc.2.25502.107" />
```

### BloodThinnerTracker.Data.SqlServer
```xml
<ProjectReference Include="..\BloodThinnerTracker.Data.Shared\..." />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0-rc.2.25502.107" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0-rc.2.25502.107" />
```

### BloodThinnerTracker.Data.SQLite
```xml
<ProjectReference Include="..\BloodThinnerTracker.Data.Shared\..." />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0-rc.2.25502.107" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0-rc.2.25502.107" />
```

## Next Steps

### Step 1: Create Provider-Specific Contexts (15 min)

Each provider project needs:

**ApplicationDbContext.cs**:
```csharp
public class ApplicationDbContext : ApplicationDbContextBase
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDataProtectionProvider dataProtectionProvider,
        ICurrentUserService currentUserService,
        ILogger<ApplicationDbContext> logger)
        : base(options, dataProtectionProvider, currentUserService, logger)
    {
    }
}
```

**DesignTimeDbContextFactory.cs**:
```csharp
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=bloodtracker_migration");
        
        // Create minimal design-time dependencies
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var currentUserService = new DesignTimeCurrentUserService();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ApplicationDbContext>();
        
        return new ApplicationDbContext(options, dataProtectionProvider, currentUserService, logger);
    }
}
```

### Step 2: Generate Migrations (5 min per provider)

```bash
# PostgreSQL
dotnet ef migrations add InitialDualKeySchema --project src/BloodThinnerTracker.Data.PostgreSQL

# SQL Server
dotnet ef migrations add InitialDualKeySchema --project src/BloodThinnerTracker.Data.SqlServer

# SQLite
dotnet ef migrations add InitialDualKeySchema --project src/BloodThinnerTracker.Data.SQLite
```

### Step 3: Implement ICurrentUserService in API (10 min)

**src/BloodThinnerTracker.Api/Services/CurrentUserService.cs**:
```csharp
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public int? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;
            
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null && int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
```

### Step 4: Update API Program.cs (10 min)

```csharp
// Register current user service
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Conditionally register provider
var provider = builder.Configuration["DatabaseProvider"] ?? "PostgreSQL";
switch (provider)
{
    case "PostgreSQL":
        builder.Services.AddDbContext<IApplicationDbContext, 
            BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>(
            options => options.UseNpgsql(connectionString));
        break;
    // ...other providers
}
```

### Step 5: Update Integration Tests (15 min)

Tests need to create provider-specific contexts with mock current user service.

### Step 6: Remove Old Files (5 min)

- Delete `src/BloodThinnerTracker.Api/Data/ApplicationDbContext.cs`
- Delete `src/BloodThinnerTracker.Api/Data/ApplicationDbContextFactory.cs`
- Delete `src/BloodThinnerTracker.Api/Migrations/*`

## Documentation References

- **Multi-Provider Migrations**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers
- **Migration Projects**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects
- **Design-Time Factories**: https://learn.microsoft.com/en-us/ef/core/cli/dbcontext-creation

## Build Status

- ✅ BloodThinnerTracker.Data.Shared: **BUILD SUCCESSFUL**
- ✅ BloodThinnerTracker.Data.PostgreSQL: Project created, packages added
- ✅ BloodThinnerTracker.Data.SqlServer: Project created, packages added
- ✅ BloodThinnerTracker.Data.SQLite: Project created, packages added
- ⏸️ BloodThinnerTracker.Api: **NOT YET UPDATED** (still uses old single context)

## Timeline Estimate

- **Remaining Work**: ~1 hour
- **Provider Contexts**: 15 min
- **Migrations**: 15 min
- **API Updates**: 20 min
- **Test Updates**: 15 min
- **Cleanup**: 5 min

---

**Ready to proceed with Step 1: Creating provider-specific contexts?**
