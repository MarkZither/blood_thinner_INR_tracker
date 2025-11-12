# Quick Start: Local Development with .NET Aspire

**Feature**: 004 - Local Development Orchestration  
**Version**: 1.0  
**Date**: October 30, 2025

This guide walks through setting up and using the .NET Aspire orchestration for local development of the Blood Thinner Tracker application.

---

## Prerequisites

### Required Software

1. **.NET 10 RC2 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/10.0
   - Verify: `dotnet --version` (should show 10.0.100 or later)

2. **Docker Desktop** (Windows/macOS) or **Docker Engine** (Linux)
   - Download: https://www.docker.com/products/docker-desktop
   - Verify: `docker --version` (should show 20.10 or later)
   - Ensure Docker is **running** before starting the application

3. **IDE** (choose one):
   - Visual Studio 2025 (recommended for Windows)
   - VS Code with C# Dev Kit extension
   - JetBrains Rider

4. **.NET Aspire Workload**
   ```bash
   dotnet workload install aspire
   ```

### System Requirements

- **RAM**: Minimum 8GB (16GB recommended)
- **Disk Space**: 20GB free (for Docker images and volumes)
- **OS**: Windows 10/11, macOS 11+, or Linux with Docker support

---

## First-Time Setup

### Step 1: Clone Repository

```bash
git clone https://github.com/MarkZither/blood_thinner_INR_tracker.git
cd blood_thinner_INR_tracker
```

### Step 2: Verify .NET SDK

```bash
dotnet --version
# Output: 10.0.100 (or later)
```

If the version is incorrect, check `global.json` in the repository root:

```json
{
  "sdk": {
   "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

### Step 3: Install Dependencies

```bash
dotnet restore
```

This downloads all NuGet packages for all projects.

### Step 4: Verify Docker is Running

**Windows/macOS**:
- Open Docker Desktop
- Ensure whale icon in system tray shows "Docker Desktop is running"

**Linux**:
```bash
sudo systemctl status docker
# Should show "active (running)"
```

**Test Docker**:
```bash
docker ps
# Should list containers (or show empty list if none running)
```

---

## Running the Application

### Option 1: Visual Studio 2025 (Windows)

1. Open `BloodThinnerTracker.sln` in Visual Studio
2. Set **BloodThinnerTracker.AppHost** as the startup project:
   - Right-click `BloodThinnerTracker.AppHost` → **Set as Startup Project**
3. Press **F5** (Start Debugging) or **Ctrl+F5** (Start Without Debugging)

### Option 2: VS Code

1. Open the repository folder in VS Code
2. Open integrated terminal (**Ctrl+`**)
3. Run:
   ```bash
   cd src/BloodThinnerTracker.AppHost
   dotnet run
   ```

### Option 3: Command Line

```bash
dotnet run --project src/BloodThinnerTracker.AppHost
```

---

## What Happens on Startup

### 1. AppHost Initialization (0-5 seconds)

```
Building...
Starting distributed application...
```

### 2. Container Orchestration (5-15 seconds)

```
[postgres] Pulling image postgres:16-alpine (if not cached)
[postgres] Creating container aspire-postgres-1
[postgres] Starting container
[postgres] Container started on port 5432
[postgres] Waiting for health check... Healthy
```

**First-time run**: Docker pulls PostgreSQL image (~150MB, 1-3 minutes on slow connections)  
**Subsequent runs**: Uses cached image (starts in <5 seconds)

### 3. Service Startup (10-20 seconds)

```
[api] Starting BloodThinnerTracker.Api on http://localhost:5234
[api] Waiting for service to be ready...
[api] Service healthy

[web] Starting BloodThinnerTracker.Web on http://localhost:5235
[web] Waiting for service to be ready...
[web] Service healthy
```

### 4. Dashboard Launch (15-25 seconds)

```
[aspire-dashboard] Dashboard available at http://localhost:15000
```

Your default browser opens automatically to the Aspire Dashboard.

---

## Accessing the Application

### Aspire Dashboard

**URL**: http://localhost:15000

**Features**:
- **Resources** tab: View all services and containers with status (Running, Stopped, Error)
- **Console Logs** tab: Real-time log streams from all services
- **Structured Logs** tab: Query logs with structured field filtering
- **Traces** tab: Distributed trace visualization across services
- **Metrics** tab: CPU, memory, request rates, error rates

### API Service

**URL**: http://localhost:5234  
**Swagger UI**: http://localhost:5234/swagger

**Test API**:
```bash
curl http://localhost:5234/health
# Output: {"status":"Healthy",...}
```

### Web Application

**URL**: http://localhost:5235

**Login** (if authentication is configured):
- Click "Sign in with Microsoft" or "Sign in with Google"
- Follow OAuth flow
- Redirect back to application

---

## Common Development Tasks

### View Real-Time Logs

1. Open Aspire Dashboard: http://localhost:15000
2. Click **Console Logs** tab
3. Select service from dropdown (api, web, postgres)
4. View live log stream

**Filter logs**:
- Search box: Enter text to filter (e.g., "error", "medication")
- Log level dropdown: Select level (Information, Warning, Error)

### View Distributed Traces

1. Open Aspire Dashboard: http://localhost:15000
2. Click **Traces** tab
3. Select time range (Last 5 minutes, Last hour, etc.)
4. Click on a trace to see span details

**Trace Example**:
```
web → GET /medications → 245ms
  ├─ http_client → GET http://api/medications → 230ms
  │   ├─ api → GET /medications → 225ms
  │   │   └─ database → SELECT * FROM medications → 15ms
```

### Hot Reload Code Changes

**Blazor .razor files**:
1. Edit a .razor component (e.g., `Medications.razor`)
2. Save file
3. Browser automatically refreshes with changes (<2 seconds)

**C# code files**:
1. Edit a C# file (e.g., `MedicationService.cs`)
2. Save file
3. Changes apply automatically without restart (<2 seconds)

**Limitations**:
- AppHost Program.cs changes **require restart**
- appsettings.json changes **require restart**
- NuGet package changes **require restart**

### Debugging Across Multiple Services

.NET Aspire provides a seamless multi-project debugging experience. You can set breakpoints in any service (API, Web, etc.) and debug simultaneously.

#### Setting Up Debug Configuration

**Visual Studio 2025**:
1. Open `BloodThinnerTracker.sln`
2. Right-click `BloodThinnerTracker.AppHost` project → **Set as Startup Project**
3. Verify you see AppHost in the debug dropdown (should show "https" or "http" profile)
4. Press **F5** to start debugging

**Visual Studio Code**:
1. Open repository root folder
2. Create/verify `.vscode/launch.json` (AppHost should be the default launch configuration)
3. Press **F5** to start debugging
4. Select `.NET Core Launch (AppHost)` if prompted

**JetBrains Rider**:
1. Open solution
2. In Run/Debug Configurations, select `AppHost` profile
3. Click **Debug** button or press **Shift+F9**

#### Debugging API Controllers

**Example: Debug a medication endpoint**

1. Open `src/BloodThinnerTracker.Api/Controllers/MedicationsController.cs`
2. Set breakpoint on line in `GetMedications()` method:
   ```csharp
   [HttpGet]
   public async Task<ActionResult<List<MedicationDto>>> GetMedications()
   {
       var medications = await _medicationService.GetAllMedicationsAsync(); // ← SET BREAKPOINT HERE
       return Ok(medications);
   }
   ```
3. Start debugging (F5)
4. Once application is running, open browser to `http://localhost:5235/medications`
5. Debugger will stop at your breakpoint
6. Inspect variables:
   - Hover over `medications` to see the collection
   - Use **Watch** window to examine `medications.Count`, individual items, etc.
   - Check **Call Stack** to see Web → API request flow
7. Press **F5** or **F10** (Step Over) / **F11** (Step Into) to continue

**Debug API directly via Swagger**:
1. Set breakpoint in API controller
2. Navigate to `http://localhost:5234/swagger`
3. Expand endpoint (e.g., `GET /medications`)
4. Click **Try it out** → **Execute**
5. Debugger stops at breakpoint

#### Debugging Blazor Web Pages

**Example: Debug a Razor component**

1. Open `src/BloodThinnerTracker.Web/Components/Pages/Medications.razor`
2. Set breakpoint in the `@code` block:
   ```csharp
   @code {
       private List<MedicationDto>? medications;

       protected override async Task OnInitializedAsync()
       {
           medications = await Http.GetFromJsonAsync<List<MedicationDto>>("medications"); // ← SET BREAKPOINT HERE
       }
   }
   ```
3. Start debugging (F5)
4. Navigate to `http://localhost:5235/medications` in browser
5. Debugger stops at your breakpoint
6. Inspect `Http` client, `medications` variable, etc.

**Note**: Blazor Server-side rendering means breakpoints work seamlessly - no special configuration needed!

#### Cross-Service Debugging

You can debug requests that flow across multiple services:

**Scenario: Web calls API, which queries database**

1. Set breakpoint in **Web** (e.g., `Medications.razor` in `OnInitializedAsync`)
2. Set breakpoint in **API** (e.g., `MedicationsController.GetMedications()`)
3. Set breakpoint in **API** data service (e.g., `MedicationService.GetAllMedicationsAsync()`)
4. Start debugging (F5)
5. Navigate to medications page in browser
6. Debugger stops at **first breakpoint** (Web component)
7. Press **F5** to continue → stops at **second breakpoint** (API controller)
8. Press **F5** to continue → stops at **third breakpoint** (data service)
9. Use **Call Stack** window to see complete request flow

**Call Stack Example**:
```
BloodThinnerTracker.Api.Services.MedicationService.GetAllMedicationsAsync()
BloodThinnerTracker.Api.Controllers.MedicationsController.GetMedications()
Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeActionMethodAsync()
...
System.Net.Http.HttpClient.GetFromJsonAsync()
BloodThinnerTracker.Web.Components.Pages.Medications.OnInitializedAsync()
```

#### Debugging with Aspire Dashboard Open

The Aspire Dashboard remains accessible during debugging:

1. Start debugging (F5)
2. Open `http://localhost:17225` in **separate browser window**
3. Set breakpoints in Visual Studio
4. As requests execute and hit breakpoints:
   - **Dashboard** shows real-time logs from the service
   - **Visual Studio** pauses at breakpoint for inspection
   - **Traces** tab shows distributed trace being built
5. Continue debugging (F5) to resume

**Tip**: Use **two monitors** - Visual Studio on one, Dashboard on the other - for optimal experience!

#### Exception Debugging

**Catch exceptions in the Dashboard**:

1. Trigger an exception (e.g., pass invalid ID to API):
   ```bash
   curl http://localhost:5234/medications/invalid-id
   ```
2. Open Dashboard → **Console Logs** tab → Filter by "Error"
3. See exception details with full stack trace
4. Click trace ID to view distributed trace showing where exception occurred

**Debug exceptions in code**:

1. Configure Visual Studio to break on exceptions:
   - **Debug** menu → **Windows** → **Exception Settings**
   - Check **Common Language Runtime Exceptions**
2. Trigger exception by invalid operation
3. Debugger breaks **immediately** when exception is thrown
4. Inspect **Locals** window to see exception message, inner exception, stack trace
5. Use **Immediate Window** to query variables: `?exception.Message`

**Add exception breakpoint**:
```csharp
try
{
    var result = await _service.ProcessAsync(data);
    return Ok(result);
}
catch (Exception ex)
{
    // SET BREAKPOINT HERE to inspect exceptions
    _logger.LogError(ex, "Error processing request");
    throw;
}
```

#### Hot Reload During Debugging

You can modify code **while debugging** without restarting:

**Supported changes** (Hot Reload compatible):
- ✅ Modify method bodies
- ✅ Add/remove/modify local variables
- ✅ Change string literals
- ✅ Modify LINQ queries
- ✅ Update Blazor .razor file HTML/CSS

**Unsupported changes** (require restart):
- ❌ Add/remove methods or properties
- ❌ Change method signatures
- ❌ Modify class inheritance
- ❌ Add/remove using statements
- ❌ Change dependency injection registrations

**How to use Hot Reload**:
1. Start debugging (F5)
2. Set breakpoint and hit it
3. **While paused at breakpoint**, modify code in the same method
4. Visual Studio shows 🔥 "Hot Reload" indicator
5. Press **F5** to continue → changes apply immediately
6. No restart needed!

**Example**:
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<MedicationDto>> GetMedication(int id)
{
    var medication = await _service.GetByIdAsync(id);
    
    if (medication == null)
    {
        // While debugging, change this message:
        return NotFound("Medication not found"); // ← EDIT THIS
        // to:
        return NotFound($"Medication with ID {id} not found"); // ← NEW VERSION
        // Press F5 → change applies immediately!
    }
    
    return Ok(medication);
}
```

#### Debugging Performance Issues

**Identify slow endpoints**:
1. Open Dashboard → **Metrics** tab
2. Select `api` service
3. View **Request Duration** chart
4. Identify endpoints with high P95/P99 latency
5. Set breakpoints in those endpoints and inspect with **Diagnostic Tools** window

**Use Diagnostic Tools** (Visual Studio):
- **Debug** menu → **Windows** → **Show Diagnostic Tools** (Ctrl+Alt+F2)
- While debugging:
  - **CPU Usage**: See which methods consume most CPU
  - **Memory Usage**: Track memory allocations
  - **Events**: See breakpoint hits, exceptions
- Take **CPU/Memory snapshots** to compare before/after code changes

### Restart a Single Service

**Via Dashboard**:
1. Open http://localhost:15000
2. Navigate to **Resources** tab
3. Find service (api, web)
4. Click **Restart** button

**Via Command Line**:
- Stop AppHost (Ctrl+C)
- Restart AppHost (F5 or `dotnet run`)

### View Metrics

1. Open Aspire Dashboard: http://localhost:15000
2. Click **Metrics** tab
3. Select service (api, web)
4. View charts:
   - **Request Duration**: P50, P95, P99 percentiles
   - **Request Rate**: Requests per second
   - **Error Rate**: Errors per second
   - **CPU Usage**: Percentage
   - **Memory Usage**: MB

---

## Database Management

### Using PostgreSQL (Default)

**Connection string** (automatically injected):
```
Host=localhost;Port=5432;Database=bloodtracker_dev;Username=bloodtracker;Password=Pass@word1
```

**Run migrations**:
```bash
cd src/BloodThinnerTracker.Api
dotnet ef database update
```

**Access PostgreSQL**:
```bash
docker exec -it aspire-postgres-1 psql -U bloodtracker -d bloodtracker_dev
```

**View tables**:
```sql
\dt
```

**Query data**:
```sql
SELECT * FROM medications LIMIT 10;
```

### Reset Database Data

**Recommended: Use reset-database.ps1 script** (safest and easiest):

```powershell
# Navigate to repository root
cd c:\Source\github\blood_thinner_INR_tracker

# Run reset script (interactive mode with confirmation)
.\tools\scripts\reset-database.ps1

# Or run with -Force to skip confirmation
.\tools\scripts\reset-database.ps1 -Force
```

**What the script does**:
- ✓ Finds all PostgreSQL containers used by the application
- ✓ Safely stops running containers
- ✓ Removes containers
- ✓ Removes associated data volumes
- ✓ Provides clear status messages and error handling

**After running the script**:
1. Press F5 in Visual Studio (or run `dotnet run --project src/BloodThinnerTracker.AppHost`)
2. Aspire automatically creates a fresh PostgreSQL container
3. Entity Framework migrations run automatically
4. Clean database is ready to use

**Manual Options** (if script is unavailable):

**Option 1: Delete Docker volume** (complete reset):
```bash
# Find volume name
docker volume ls | findstr postgres

# Stop and remove containers
docker stop <container_id>
docker rm <container_id>

# Remove volume
docker volume rm <volume_name>
```

**Option 2: Truncate tables** (keep schema):
```bash
docker exec -it <container_name> psql -U bloodtracker_user -d bloodtracker -c "TRUNCATE TABLE medications CASCADE;"
```

---

## Troubleshooting

### Error: "Docker is not running"

**Symptoms**:
```
Error: Failed to start container 'postgres'
Reason: Docker daemon is not running
```

**Solution**:
- **Windows/macOS**: Open Docker Desktop and wait for it to start
- **Linux**: `sudo systemctl start docker`

### Error: "Port already in use"

**Symptoms**:
```
Error: Failed to bind to address http://localhost:5234
Reason: Address already in use
```

**Solution**:
1. Find process using port:
   ```bash
   # Windows
   netstat -ano | findstr :5234
   
   # macOS/Linux
   lsof -i :5234
   ```

2. Kill process or change port in `AppHost/Program.cs`:
   ```csharp
   .WithHttpEndpoint(port: 5240)  // Use different port
   ```

### Error: "Connection string not found"

**Symptoms**:
```
InvalidOperationException: A named connection string was not found in the configuration
```

**Solution**:
1. Verify AppHost defines database reference:
   ```csharp
   var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
       .WithReference(db);  // THIS LINE REQUIRED
   ```

2. Check API Program.cs uses correct connection name:
   ```csharp
   builder.Configuration.GetConnectionString("bloodtracker")  // Match database name
   ```

### Error: "Service discovery resolution failed"

**Symptoms**:
```
HttpRequestException: No such host is known (http://api)
```

**Solution**:
1. Verify `AddServiceDefaults()` called in both API and Web:
   ```csharp
   builder.AddServiceDefaults();  // REQUIRED
   ```

2. Verify Web project references API in AppHost:
   ```csharp
   var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
       .WithReference(api);  // THIS LINE REQUIRED
   ```

### Slow startup times

**Symptoms**: Application takes >60 seconds to start

**Solutions**:
1. **First-time Docker image pull**: Wait for images to download (one-time cost)
2. **Antivirus scanning Docker volumes**: Add Docker volumes to antivirus exclusion list
3. **Low disk space**: Free up disk space (Docker needs 10GB+ free)
4. **Docker Desktop resource limits**: Increase CPU/memory in Docker Desktop settings

### Aspire Dashboard won't open

**Symptoms**: Dashboard URL doesn't open or shows "Connection refused"

**Solutions**:
1. Wait 10-15 seconds after AppHost starts
2. Manually open http://localhost:15000
3. Check console output for actual dashboard URL (may be different port)
4. Verify no firewall blocking localhost:15000

---

## Advanced Configuration

### Enable InfluxDB for Log Persistence

1. Edit `appsettings.Development.json` in API project:
   ```json
   {
     "Logging": {
       "InfluxDB": {
         "Enabled": true,
         "Uri": "http://localhost:8086",
         "Database": "bloodtracker_logs"
       }
     }
   }
   ```

2. Restart AppHost (InfluxDB container starts automatically)

3. Access InfluxDB UI: http://localhost:8086
   - Username: `admin`
   - Password: `admin123`

### Switch to SQLite (No Container)

1. Edit `AppHost/Program.cs`:
   ```csharp
   // Comment out PostgreSQL
   // var postgres = builder.AddPostgres("postgres");
   // var db = postgres.AddDatabase("bloodtracker");

   var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api");
   // .WithReference(db);  // Remove this line
   ```

2. Edit `API/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "bloodtracker": "Data Source=bloodtracker_dev.db"
     }
   }
   ```

3. Restart AppHost

### Customize Dashboard Port

1. Edit `AppHost/Properties/launchSettings.json`:
   ```json
   {
     "profiles": {
       "http": {
         "environmentVariables": {
           "ASPIRE_DASHBOARD_PORT": "16000"
         }
       }
     }
   }
   ```

3. Restart AppHost

4. Dashboard now at http://localhost:16000

### Enable InfluxDB for Time-Series Metrics (Optional)

**Purpose**: Store metrics in InfluxDB for long-term analysis and custom dashboards

**Enable InfluxDB**:

1. Edit `AppHost/appsettings.json`:
   ```json
   {
     "Features": {
       "EnableInfluxDB": true
     }
   }
   ```

2. Restart AppHost (F5)

3. InfluxDB will start automatically and appear in Dashboard under **Resources**

**Access InfluxDB UI**:
- URL: http://localhost:8086
- Username: `admin`
- Password: `local_dev_only_password` (from appsettings.json)
- Organization: `bloodtracker`
- Bucket: `metrics`

**Configure OpenTelemetry to Export to InfluxDB**:

1. Add package to `ServiceDefaults`:
   ```bash
   dotnet add package OpenTelemetry.Exporter.InfluxDB
   ```

2. Update `ServiceDefaults.cs` to add InfluxDB exporter:
   ```csharp
   services.AddOpenTelemetry()
       .WithMetrics(metrics =>
       {
           metrics.AddInfluxDBMetricsExporter(options =>
           {
               options.Endpoint = new Uri("http://localhost:8086");
               options.Token = "your_influxdb_token";
               options.Org = "bloodtracker";
               options.Bucket = "metrics";
           });
       });
   ```

3. Restart services to begin exporting metrics

**Use Cases**:
- **Long-term metric storage**: Keep metrics beyond Dashboard's in-memory retention
- **Custom dashboards**: Build custom Grafana/InfluxDB dashboards
- **Alerting**: Set up alerts based on metric thresholds
- **Historical analysis**: Compare performance over weeks/months

**Note**: InfluxDB is disabled by default to keep local setup simple. Enable only if you need advanced metrics capabilities.

---

## Next Steps

- **Feature Development**: Make code changes and use hot reload
- **Debugging**: Set breakpoints in Visual Studio and debug across services
- **Testing**: Run integration tests with `dotnet test`
- **Monitoring**: Use Aspire Dashboard to diagnose issues
- **Database Changes**: Run EF Core migrations to update schema

---

## Additional Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Serilog Documentation](https://serilog.net/)
- [Polly Documentation](https://www.thepollyproject.org/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

---

## Getting Help

**Issues**:
- Check Aspire Dashboard logs first (http://localhost:15000)
- Review this troubleshooting section
- Search GitHub issues: https://github.com/MarkZither/blood_thinner_INR_tracker/issues

**Questions**:
- Open a discussion: https://github.com/MarkZither/blood_thinner_INR_tracker/discussions
- Contact maintainer: @MarkZither

---

**Summary**: This guide covers the complete local development workflow from initial setup to advanced configuration. The F5 experience provides automatic orchestration with comprehensive observability through the Aspire Dashboard.
