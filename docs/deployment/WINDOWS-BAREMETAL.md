# Windows Bare-Metal Deployment Guide

## Overview

This guide covers deploying the Blood Thinner & INR Tracker directly to **Windows Server or Windows Desktop** for internal use. This deployment is optimized for:

- ✅ **Bare metal Windows** (no Docker or WSL)
- ✅ **Native AOT compilation** (optional, for best performance)
- ✅ **Windows Services** (automatic startup and management)
- ✅ **SQLite database** (persisted between updates)
- ✅ **Internal network access** (localhost and LAN)

**⚠️ SECURITY NOTE**: This configuration is designed for internal-only use on a trusted network. Do not expose directly to the internet without additional security measures.

## Prerequisites

### Hardware Requirements
- **Windows 10/11** (64-bit) or **Windows Server 2019+**
- **4GB+ RAM** (8GB recommended)
- **10GB+ free disk space**
- **Network connectivity** (for accessing from other devices)

### Software Prerequisites
- **Administrator access** (required for installing Windows Services)
- **.NET 10 SDK** on the build machine
- **PowerShell 5.1+** (included with Windows)

## Installation

### One-Command Automated Deployment

The easiest way to deploy is using the automated PowerShell script:

#### Option 1: Standard Deployment

```powershell
# Open PowerShell as Administrator
# Navigate to repository root
cd path\to\blood_thinner_INR_tracker

# Deploy API only
.\tools\deploy-windows-baremetal.ps1

# Deploy API + Web UI
.\tools\deploy-windows-baremetal.ps1 -Web
```

#### Option 2: With Native AOT (Recommended for Production)

```powershell
# Deploy API with Native AOT
.\tools\deploy-windows-baremetal.ps1 -AOT

# Deploy API + Web with Native AOT
.\tools\deploy-windows-baremetal.ps1 -Web -AOT
```

**Native AOT Benefits:**
- 50-70% faster startup time
- 30-50% lower memory usage
- No .NET runtime required on target machine
- Longer build time (5-10x) - only affects deployment, not runtime

### What the Script Does

The automated deployment script:

1. **Stops existing services** (if running)
2. **Backs up current installation** to timestamped folder
3. **Compiles application** with self-contained or AOT
4. **Creates installation directories**:
   - `C:\BloodTracker\api` - Application files
   - `C:\BloodTracker\web` - Web UI files (if deploying)
   - `C:\ProgramData\BloodTracker` - **Database location (persisted)**
5. **Creates production configuration** files
6. **Configures Windows Firewall** rules (ports 5234, 5235)
7. **Creates Windows Services**:
   - `BloodTrackerApi` - API service (auto-start)
   - `BloodTrackerWeb` - Web UI service (auto-start, depends on API)
8. **Starts services** and verifies health

### Manual Deployment

If you prefer manual control:

#### Step 1: Build Application

```powershell
# Standard build
dotnet publish src\BloodThinnerTracker.Api\BloodThinnerTracker.Api.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output .\publish\api `
  -p:PublishSingleFile=true

# OR with Native AOT
dotnet publish src\BloodThinnerTracker.Api\BloodThinnerTracker.Api.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output .\publish\api `
  -p:PublishAot=true `
  -p:StripSymbols=true
```

#### Step 2: Create Directories

```powershell
New-Item -Path "C:\BloodTracker\api" -ItemType Directory -Force
New-Item -Path "C:\ProgramData\BloodTracker" -ItemType Directory -Force
```

#### Step 3: Copy Files

```powershell
Copy-Item -Path ".\publish\api\*" -Destination "C:\BloodTracker\api" -Recurse -Force
```

#### Step 4: Create Configuration

Create `C:\BloodTracker\api\appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=C:\\ProgramData\\BloodTracker\\bloodtracker.db;Cache=Shared;Journal Mode=WAL;Synchronous=NORMAL;"
  },
  "Database": {
    "Provider": "SQLite"
  },
  "Urls": "http://localhost:5234;http://0.0.0.0:5234",
  "Security": {
    "RequireHttps": false,
    "EnableCors": true,
    "AllowedOrigins": [
      "http://localhost:5235"
    ]
  },
  "MedicalApplication": {
    "Name": "Blood Thinner Medication & INR Tracker",
    "Version": "1.0.0",
    "ComplianceLevel": "InternalUseOnly",
    "EnableAuditLogging": true
  }
}
```

#### Step 5: Create Windows Service

```powershell
# Create service
sc.exe create BloodTrackerApi `
  binPath= "C:\BloodTracker\api\BloodThinnerTracker.Api.exe" `
  start= auto `
  DisplayName= "Blood Thinner Tracker API"

# Set description
sc.exe description BloodTrackerApi "Blood Thinner Medication & INR Tracker API Service"

# Configure auto-restart on failure
sc.exe failure BloodTrackerApi reset= 86400 actions= restart/60000/restart/60000/restart/60000

# Set environment variable
$regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\BloodTrackerApi"
New-ItemProperty -Path $regPath -Name "Environment" -PropertyType MultiString -Value "ASPNETCORE_ENVIRONMENT=Production" -Force
```

#### Step 6: Configure Firewall

```powershell
New-NetFirewallRule -DisplayName "Blood Thinner Tracker API" `
  -Direction Inbound `
  -LocalPort 5234 `
  -Protocol TCP `
  -Action Allow `
  -Profile Any
```

#### Step 7: Start Service

```powershell
Start-Service -Name BloodTrackerApi
```

## Managing Services

### Common Commands

```powershell
# Check service status
Get-Service -Name BloodTrackerApi

# Start service
Start-Service -Name BloodTrackerApi

# Stop service
Stop-Service -Name BloodTrackerApi

# Restart service
Restart-Service -Name BloodTrackerApi

# View service configuration
Get-Service -Name BloodTrackerApi | Format-List *

# Check if service is set to auto-start
Get-WmiObject -Class Win32_Service -Filter "Name='BloodTrackerApi'" | Select-Object Name, StartMode
```

### Viewing Logs

Windows Services log to the Windows Event Log:

```powershell
# View recent application logs
Get-EventLog -LogName Application -Source "BloodTrackerApi" -Newest 50

# View only errors
Get-EventLog -LogName Application -Source "BloodTrackerApi" -EntryType Error -Newest 20

# Export logs to file
Get-EventLog -LogName Application -Source "BloodTrackerApi" -Newest 100 | 
  Export-Csv -Path ".\logs.csv" -NoTypeInformation
```

### Service Management GUI

You can also manage services through:
- **Services Console**: Press `Win+R`, type `services.msc`, press Enter
- Find "Blood Thinner Tracker API" or "Blood Thinner Tracker Web UI"
- Right-click for Start, Stop, Restart, Properties

## Updating the Application

### Automated Update

Simply run the deployment script again:

```powershell
# Run as Administrator
.\tools\deploy-windows-baremetal.ps1 -Web -AOT
```

The script will:
1. Create a backup of current installation
2. Stop services
3. Deploy new version
4. **Preserve the database** (in `C:\ProgramData\BloodTracker`)
5. Restart services

**Important**: The database at `C:\ProgramData\BloodTracker\bloodtracker.db` is preserved during updates.

### Manual Update

```powershell
# 1. Stop service
Stop-Service -Name BloodTrackerApi

# 2. Backup current installation
Copy-Item -Path "C:\BloodTracker" -Destination "C:\BloodTracker.backup.$(Get-Date -Format 'yyyyMMdd')" -Recurse

# 3. Build new version
dotnet publish src\BloodThinnerTracker.Api\BloodThinnerTracker.Api.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output .\publish\api `
  -p:PublishAot=true

# 4. Copy new files (preserving configuration)
$configBackup = Get-Content "C:\BloodTracker\api\appsettings.Production.json"
Copy-Item -Path ".\publish\api\*" -Destination "C:\BloodTracker\api" -Recurse -Force
$configBackup | Out-File -FilePath "C:\BloodTracker\api\appsettings.Production.json" -Encoding UTF8

# 5. Start service
Start-Service -Name BloodTrackerApi
```

## Database Management

### Database Location

The SQLite database is stored at:
```
C:\ProgramData\BloodTracker\bloodtracker.db
```

This location is separate from the application directory and **persists across updates**.

### Backup Database

```powershell
# Manual backup
Copy-Item "C:\ProgramData\BloodTracker\bloodtracker.db" `
  -Destination ".\backups\bloodtracker-$(Get-Date -Format 'yyyyMMdd-HHmmss').db"

# Scheduled backup (create a scheduled task)
$action = New-ScheduledTaskAction -Execute 'PowerShell.exe' `
  -Argument '-Command "Copy-Item C:\ProgramData\BloodTracker\bloodtracker.db C:\Backups\bloodtracker-$(Get-Date -Format yyyy-MM-dd).db"'
$trigger = New-ScheduledTaskTrigger -Daily -At 2am
Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "BloodTrackerBackup" -Description "Daily backup of Blood Thinner Tracker database"
```

### Restore Database

```powershell
# 1. Stop service
Stop-Service -Name BloodTrackerApi

# 2. Replace database
Copy-Item ".\backups\bloodtracker-20250108.db" `
  -Destination "C:\ProgramData\BloodTracker\bloodtracker.db" -Force

# 3. Start service
Start-Service -Name BloodTrackerApi
```

## Network Access

### Local Access

After deployment, the API is accessible at:
- `http://localhost:5234/scalar/v1` (API documentation)
- `http://localhost:5234/health` (health check)

If Web UI is deployed:
- `http://localhost:5235` (Web UI)

### Network Access from Other Devices

The application binds to `0.0.0.0`, making it accessible from the network:
- `http://192.168.1.100:5234/scalar/v1` (replace with your machine's IP)
- `http://COMPUTER-NAME:5234/scalar/v1` (using computer name)

**Find your IP address:**
```powershell
Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.InterfaceAlias -notlike "*Loopback*"} | Select-Object IPAddress
```

### Firewall Configuration

The deployment script automatically creates firewall rules. To manually manage:

```powershell
# Check firewall rules
Get-NetFirewallRule -DisplayName "Blood Thinner Tracker*"

# Add rule if missing
New-NetFirewallRule -DisplayName "Blood Thinner Tracker API" `
  -Direction Inbound `
  -LocalPort 5234 `
  -Protocol TCP `
  -Action Allow `
  -Profile Private,Domain

# Remove rule
Remove-NetFirewallRule -DisplayName "Blood Thinner Tracker API"
```

## Troubleshooting

### Service Won't Start

```powershell
# Check service status
Get-Service -Name BloodTrackerApi

# Check event log for errors
Get-EventLog -LogName Application -Source "BloodTrackerApi" -EntryType Error -Newest 10

# Try running executable manually to see errors
& "C:\BloodTracker\api\BloodThinnerTracker.Api.exe"
```

**Common Issues:**
1. **Port already in use**: Another application is using port 5234
   - Check: `Get-NetTCPConnection -LocalPort 5234`
   - Solution: Change port in `appsettings.Production.json`

2. **Permission denied**: Service account lacks permissions
   - Solution: Check file permissions on `C:\BloodTracker`

3. **Database locked**: Another process has the database open
   - Solution: Stop all services, check for SQLite browser tools

### Cannot Access from Network

```powershell
# Check if service is listening on all interfaces
Get-NetTCPConnection -LocalPort 5234 -State Listen

# Check firewall rule
Get-NetFirewallRule -DisplayName "Blood Thinner Tracker API" | Get-NetFirewallPortFilter

# Test from another machine
Test-NetConnection -ComputerName SERVER-NAME -Port 5234
```

### High Memory Usage

```powershell
# Check service memory usage
Get-Process -Name "BloodThinnerTracker.Api" | Select-Object Name, WorkingSet, CPU

# Restart service to free memory
Restart-Service -Name BloodTrackerApi
```

**Solution**: Enable Native AOT for lower memory usage:
```powershell
.\tools\deploy-windows-baremetal.ps1 -AOT
```

### Database Corruption

```powershell
# Stop service
Stop-Service -Name BloodTrackerApi

# Check database integrity (requires SQLite command-line tool)
sqlite3 "C:\ProgramData\BloodTracker\bloodtracker.db" "PRAGMA integrity_check;"

# If corrupted, restore from backup
Copy-Item ".\backups\bloodtracker-latest.db" `
  -Destination "C:\ProgramData\BloodTracker\bloodtracker.db" -Force

# Start service
Start-Service -Name BloodTrackerApi
```

## Uninstallation

To completely remove the application:

```powershell
# Run as Administrator

# 1. Stop and remove services
Stop-Service -Name BloodTrackerApi -Force -ErrorAction SilentlyContinue
Stop-Service -Name BloodTrackerWeb -Force -ErrorAction SilentlyContinue
sc.exe delete BloodTrackerApi
sc.exe delete BloodTrackerWeb

# 2. Remove firewall rules
Remove-NetFirewallRule -DisplayName "Blood Thinner Tracker API" -ErrorAction SilentlyContinue
Remove-NetFirewallRule -DisplayName "Blood Thinner Tracker Web" -ErrorAction SilentlyContinue

# 3. Remove installation directory
Remove-Item -Path "C:\BloodTracker" -Recurse -Force

# 4. OPTIONAL: Remove database (WARNING: This deletes all data!)
# Remove-Item -Path "C:\ProgramData\BloodTracker" -Recurse -Force
```

## Performance Optimization

### Enable Native AOT

For best performance, rebuild with Native AOT:

```powershell
.\tools\deploy-windows-baremetal.ps1 -AOT
```

**Benefits:**
- Startup time: ~2-3 seconds (vs 5-8 seconds standard)
- Memory usage: ~100-150 MB (vs 200-300 MB standard)
- No JIT compilation overhead

### SQLite Optimization

The application is pre-configured with SQLite optimizations:
- **WAL mode**: Better concurrency
- **Synchronous=NORMAL**: Balanced durability/performance

To manually optimize:

```powershell
# Stop service
Stop-Service -Name BloodTrackerApi

# Run SQLite optimization (requires sqlite3.exe)
sqlite3 "C:\ProgramData\BloodTracker\bloodtracker.db" "VACUUM; ANALYZE;"

# Start service
Start-Service -Name BloodTrackerApi
```

## Security Considerations

### Internal Use Only

This deployment is designed for **internal use only**:

✅ **Safe on private network**: Internal LAN, VPN
❌ **NOT for public internet**: No advanced security hardening

### Additional Security (Optional)

If you need enhanced security:

1. **Enable HTTPS** with SSL certificate
2. **Configure OAuth** (Azure AD/Google)
3. **Use reverse proxy** (IIS, nginx)
4. **Restrict firewall** to specific IPs
5. **Enable Windows Firewall** advanced rules

### Windows Firewall Profiles

The deployment creates rules for all profiles. To restrict:

```powershell
# Restrict to Private network only
Set-NetFirewallRule -DisplayName "Blood Thinner Tracker API" -Profile Private
```

## System Resource Requirements

### Minimum

- 2GB RAM
- 2GB disk space
- 1 CPU core

### Recommended

- 4GB RAM
- 10GB disk space (for database growth)
- 2+ CPU cores

### Expected Performance

**Standard Deployment:**
- Startup time: 5-8 seconds
- Memory usage: 200-300 MB per service
- Response time: <100ms typical requests

**Native AOT Deployment:**
- Startup time: 2-3 seconds
- Memory usage: 100-150 MB per service
- Response time: <50ms typical requests

## Support

For deployment issues:
- **Documentation**: [Full deployment guide](../docs/deployment/)
- **Issues**: [GitHub Issues](https://github.com/MarkZither/blood_thinner_INR_tracker/issues)
- **Event Log**: Check Windows Event Viewer > Application

---

**Deployment Type**: Bare Metal Windows
**Platform**: Windows 10/11, Windows Server 2019+
**Database**: SQLite (persisted)
**Services**: Windows Services (auto-start)
**Created**: November 2025
