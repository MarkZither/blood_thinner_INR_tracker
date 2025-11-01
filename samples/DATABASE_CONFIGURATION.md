# Database Configuration Samples

This directory contains sample configuration files for different database providers.

## Supported Database Providers

The Blood Thinner INR Tracker supports three database providers:

### 1. SQLite (Development/Testing)
- **Best for**: Local development, unit tests, quick prototyping
- **File**: Uses default `appsettings.Development.json`
- **Connection**: File-based database
- **Features**: Fast, portable, no setup required

```json
{
  "Database": {
    "Provider": "SQLite"
  }
}
```

### 2. PostgreSQL (Cloud/Production)
- **Best for**: Cloud deployment, Aspire orchestration, production workloads
- **File**: Default for Aspire environments
- **Connection**: Network-based with SSL support
- **Features**: ACID compliant, scalable, free, excellent for cloud

```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "PostgreSQL": {
      "Server": "localhost",
      "Port": "5432",
      "Database": "bloodtracker_production",
      "Username": "bloodtracker_user",
      "Password": "your_secure_password"
    }
  }
}
```

**Or use connection string:**
```json
{
  "ConnectionStrings": {
    "PostgreSQLConnection": "Host=localhost;Port=5432;Database=bloodtracker;Username=user;Password=pass;SSL Mode=Require;"
  },
  "Database": {
    "Provider": "PostgreSQL"
  }
}
```

### 3. SQL Server / Azure SQL (Enterprise)
- **Best for**: Enterprise deployment, Azure integration, existing SQL Server infrastructure
- **File**: `appsettings.SqlServer.json` (local) or `appsettings.AzureSQL.json` (cloud)
- **Connection**: Network-based with encryption
- **Features**: Enterprise features, Azure integration, familiar tooling

**Local SQL Server:**
```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=localhost;Database=bloodtracker_dev;Integrated Security=true;Encrypt=false;TrustServerCertificate=true;MultipleActiveResultSets=true;"
  },
  "Database": {
    "Provider": "SqlServer"
  }
}
```

**Azure SQL:**
```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=tcp:yourserver.database.windows.net,1433;Database=bloodtracker_prod;User ID=admin;Password=<password>;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"
  },
  "Database": {
    "Provider": "SqlServer"
  }
}
```

## Migration Compatibility

**All three providers use the SAME migrations!** 

EF Core automatically translates types:
- SQLite: `TEXT`, `INTEGER`, `REAL`
- PostgreSQL: `text`, `character varying`, `timestamp with time zone`
- SQL Server: `nvarchar`, `int`, `datetime2`

## Testing Your Configuration

### Test with SQLite (Fast)
```bash
dotnet run --project src/BloodThinnerTracker.Api
```

### Test with PostgreSQL (Aspire)
```bash
dotnet run --project src/BloodThinnerTracker.AppHost
```

### Test with SQL Server (Local)
```bash
# 1. Copy sample configuration
cp samples/appsettings.SqlServer.json src/BloodThinnerTracker.Api/appsettings.Development.json

# 2. Update connection string for your SQL Server instance

# 3. Run migrations
cd src/BloodThinnerTracker.Api
dotnet ef database update

# 4. Start application
dotnet run
```

### Test with Azure SQL
```bash
# 1. Create Azure SQL Database
az sql server create ...
az sql db create ...

# 2. Configure connection string with Azure credentials
# Use appsettings.AzureSQL.json as template

# 3. Apply migrations
dotnet ef database update --connection "Server=tcp:..."

# 4. Deploy application
az webapp create ...
```

## Integration Tests

Run tests against all three providers:

```bash
# PostgreSQL (uses Testcontainers)
dotnet test tests/BloodThinnerTracker.Integration.Tests --filter "PostgreSQL"

# SQLite (in-memory)
dotnet test tests/BloodThinnerTracker.Integration.Tests --filter "SQLite"

# SQL Server (requires Testcontainers setup)
dotnet test tests/BloodThinnerTracker.Integration.Tests --filter "SqlServer"
```

## Security Considerations

### SQLite
- ✅ File-based encryption support (SQLCipher)
- ⚠️ Not recommended for production
- ✅ Perfect for local development

### PostgreSQL
- ✅ SSL/TLS encryption
- ✅ Row-level security
- ✅ Connection pooling
- ✅ Automatic retry logic

### SQL Server / Azure SQL
- ✅ Transparent Data Encryption (TDE)
- ✅ Always Encrypted
- ✅ Azure AD authentication
- ✅ Advanced threat protection (Azure)

## Performance Comparison

| Feature | SQLite | PostgreSQL | SQL Server |
|---------|--------|------------|------------|
| Setup Time | Instant | 2 mins | 5 mins |
| Query Speed | Very Fast | Fast | Fast |
| Concurrent Users | 1-10 | 100s | 1000s |
| Data Size | < 1 GB | Unlimited | Unlimited |
| Cost | Free | Free | Licensed/Azure |
| Cloud Ready | No | Yes | Yes |

## Choosing a Provider

**Use SQLite if:**
- Developing locally
- Running tests
- Prototyping features
- Single-user scenarios

**Use PostgreSQL if:**
- Deploying to cloud
- Using .NET Aspire
- Need high concurrency
- Want open-source
- Prefer AWS/GCP/Azure

**Use SQL Server if:**
- Already have SQL Server
- Using Azure ecosystem
- Need enterprise features
- Team knows T-SQL
- Compliance requires Microsoft stack

## Migration Commands

All providers use the same commands:

```bash
# Add new migration
dotnet ef migrations add MigrationName --project src/BloodThinnerTracker.Api

# Apply migrations
dotnet ef database update --project src/BloodThinnerTracker.Api

# Generate SQL script
dotnet ef migrations script --project src/BloodThinnerTracker.Api

# Rollback to specific migration
dotnet ef database update MigrationName --project src/BloodThinnerTracker.Api
```

## Environment Variables

Override configuration with environment variables:

```bash
# PostgreSQL
export Database__Provider=PostgreSQL
export Database__PostgreSQL__Server=localhost
export Database__PostgreSQL__Password=mypassword

# SQL Server
export Database__Provider=SqlServer
export ConnectionStrings__SqlServerConnection="Server=localhost;..."

# SQLite
export Database__Provider=SQLite
```

## Troubleshooting

### PostgreSQL Connection Failed
```bash
# Check if PostgreSQL is running
docker ps | grep postgres

# Check Aspire dashboard
# Open https://localhost:17225
```

### SQL Server Connection Failed
```bash
# Test connection
sqlcmd -S localhost -U sa -P password -Q "SELECT @@VERSION"

# Enable TCP/IP in SQL Server Configuration Manager
# Restart SQL Server service
```

### Migration Errors
```bash
# Drop database and recreate
dotnet ef database drop --project src/BloodThinnerTracker.Api --force
dotnet ef database update --project src/BloodThinnerTracker.Api

# Or use the database script for PostgreSQL
.\tools\scripts\database.ps1 -Command reset
```

## Questions?

See main project README or open an issue on GitHub.
