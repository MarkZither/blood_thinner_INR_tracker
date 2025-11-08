# Blood Thinner Tracker - Windows Bare-Metal Deployment Script
# This script automates the deployment to Windows Server/Desktop for internal use
# Compiles with AOT, sets up Windows Services, persists SQLite DB

#Requires -Version 5.1
#Requires -RunAsAdministrator

# Stop on errors
$ErrorActionPreference = "Stop"

# Configuration
$INSTALL_DIR = "C:\BloodTracker"
$DATA_DIR = "C:\ProgramData\BloodTracker"
$PUBLISH_DIR = ".\publish-windows"
$API_PROJ = "src\BloodThinnerTracker.Api\BloodThinnerTracker.Api.csproj"
$WEB_PROJ = "src\BloodThinnerTracker.Web\BloodThinnerTracker.Web.csproj"
$API_SERVICE_NAME = "BloodTrackerApi"
$WEB_SERVICE_NAME = "BloodTrackerWeb"

# Color output functions
function Write-Step {
    param([string]$Message)
    Write-Host ">>> $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error-Message {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

Write-Host "=== Blood Thinner Tracker - Windows Bare-Metal Deployment ===" -ForegroundColor Green
Write-Host ""

# Check if running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error-Message "This script must be run as Administrator"
    Write-Host "Please run PowerShell as Administrator and try again"
    exit 1
}

Write-Success "Running with Administrator privileges"

# Check if dotnet is installed
$dotnetVersion = $null
try {
    $dotnetVersion = & dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed"
    }
} catch {
    Write-Error-Message "dotnet SDK not found. Please install .NET 10 SDK."
    exit 1
}

Write-Success "dotnet SDK found: $dotnetVersion"

# Parse command line arguments
param(
    [switch]$Web,
    [switch]$AOT,
    [switch]$Help
)

if ($Help) {
    Write-Host "Usage: .\deploy-windows-baremetal.ps1 [OPTIONS]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Web           Deploy Web UI in addition to API"
    Write-Host "  -AOT           Enable Native AOT compilation (recommended for production)"
    Write-Host "  -Help          Show this help message"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\deploy-windows-baremetal.ps1                    # Deploy API only"
    Write-Host "  .\deploy-windows-baremetal.ps1 -Web               # Deploy API and Web"
    Write-Host "  .\deploy-windows-baremetal.ps1 -AOT               # Deploy API with AOT"
    Write-Host "  .\deploy-windows-baremetal.ps1 -Web -AOT          # Deploy both with AOT"
    Write-Host ""
    Write-Host "Note: This script requires Administrator privileges"
    exit 0
}

$DEPLOY_WEB = $Web.IsPresent
$ENABLE_AOT = $AOT.IsPresent

Write-Host ""
Write-Host "Deployment Configuration:"
Write-Host "  Install Directory: $INSTALL_DIR"
Write-Host "  Data Directory: $DATA_DIR"
Write-Host "  Deploy Web: $DEPLOY_WEB"
Write-Host "  Enable AOT: $ENABLE_AOT"
Write-Host ""

if ($ENABLE_AOT) {
    Write-Host "⚠️  AOT Warning: Native AOT compilation provides:" -ForegroundColor Yellow
    Write-Host "    - Faster startup time (~50-70% faster)" -ForegroundColor Yellow
    Write-Host "    - Lower memory usage (~30-50% less)" -ForegroundColor Yellow
    Write-Host "    - Longer build time (5-10x longer)" -ForegroundColor Yellow
    Write-Host "    - Some reflection-based features may not work" -ForegroundColor Yellow
    Write-Host ""
}

# Confirm deployment
$confirmation = Read-Host "Continue with deployment? (y/N)"
if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
    Write-Error-Message "Deployment cancelled"
    exit 1
}

# Step 1: Stop existing services if running
Write-Step "Stopping existing services..."
$apiService = Get-Service -Name $API_SERVICE_NAME -ErrorAction SilentlyContinue
if ($apiService) {
    if ($apiService.Status -eq 'Running') {
        Stop-Service -Name $API_SERVICE_NAME -Force
        Write-Success "Stopped $API_SERVICE_NAME"
    }
}

if ($DEPLOY_WEB) {
    $webService = Get-Service -Name $WEB_SERVICE_NAME -ErrorAction SilentlyContinue
    if ($webService) {
        if ($webService.Status -eq 'Running') {
            Stop-Service -Name $WEB_SERVICE_NAME -Force
            Write-Success "Stopped $WEB_SERVICE_NAME"
        }
    }
}

# Step 2: Backup current installation
Write-Step "Backing up current installation..."
if (Test-Path $INSTALL_DIR) {
    $backupDir = "$INSTALL_DIR.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Copy-Item -Path $INSTALL_DIR -Destination $backupDir -Recurse -Force
    Write-Success "Backup created at $backupDir"
} else {
    Write-Host "No existing installation to backup"
}

# Step 3: Clean previous publish
Write-Step "Cleaning previous publish directory..."
if (Test-Path $PUBLISH_DIR) {
    Remove-Item -Path $PUBLISH_DIR -Recurse -Force
}
New-Item -Path "$PUBLISH_DIR\api" -ItemType Directory -Force | Out-Null
if ($DEPLOY_WEB) {
    New-Item -Path "$PUBLISH_DIR\web" -ItemType Directory -Force | Out-Null
}
Write-Success "Cleaned publish directory"

# Step 4: Publish API
Write-Step "Publishing API for Windows..."

$apiPublishArgs = @(
    "publish", $API_PROJ,
    "--configuration", "Release",
    "--runtime", "win-x64",
    "--self-contained", "true",
    "--output", "$PUBLISH_DIR\api"
)

if ($ENABLE_AOT) {
    Write-Host "  Building with Native AOT (this may take several minutes)..." -ForegroundColor Cyan
    $apiPublishArgs += "-p:PublishAot=true"
    $apiPublishArgs += "-p:StripSymbols=true"
} else {
    $apiPublishArgs += "-p:PublishSingleFile=true"
    $apiPublishArgs += "-p:PublishTrimmed=false"
}

& dotnet $apiPublishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error-Message "API publish failed"
    exit 1
}

Write-Success "API published to $PUBLISH_DIR\api"

# Step 5: Publish Web (optional)
if ($DEPLOY_WEB) {
    Write-Step "Publishing Web UI for Windows..."
    
    $webPublishArgs = @(
        "publish", $WEB_PROJ,
        "--configuration", "Release",
        "--runtime", "win-x64",
        "--self-contained", "true",
        "--output", "$PUBLISH_DIR\web"
    )
    
    if ($ENABLE_AOT) {
        Write-Host "  Building with Native AOT (this may take several minutes)..." -ForegroundColor Cyan
        $webPublishArgs += "-p:PublishAot=true"
        $webPublishArgs += "-p:StripSymbols=true"
    } else {
        $webPublishArgs += "-p:PublishSingleFile=true"
        $webPublishArgs += "-p:PublishTrimmed=false"
    }
    
    & dotnet $webPublishArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Message "Web UI publish failed"
        exit 1
    }
    
    Write-Success "Web UI published to $PUBLISH_DIR\web"
}

# Step 6: Create installation directories
Write-Step "Creating installation directories..."
if (-not (Test-Path $INSTALL_DIR)) {
    New-Item -Path $INSTALL_DIR -ItemType Directory -Force | Out-Null
}
if (-not (Test-Path "$INSTALL_DIR\api")) {
    New-Item -Path "$INSTALL_DIR\api" -ItemType Directory -Force | Out-Null
}
if ($DEPLOY_WEB -and -not (Test-Path "$INSTALL_DIR\web")) {
    New-Item -Path "$INSTALL_DIR\web" -ItemType Directory -Force | Out-Null
}

# Create data directory (persists across updates)
if (-not (Test-Path $DATA_DIR)) {
    New-Item -Path $DATA_DIR -ItemType Directory -Force | Out-Null
}
Write-Success "Installation directories created"

# Step 7: Copy files to installation directory
Write-Step "Copying API files to installation directory..."
Copy-Item -Path "$PUBLISH_DIR\api\*" -Destination "$INSTALL_DIR\api" -Recurse -Force
Write-Success "API files copied"

if ($DEPLOY_WEB) {
    Write-Step "Copying Web UI files to installation directory..."
    Copy-Item -Path "$PUBLISH_DIR\web\*" -Destination "$INSTALL_DIR\web" -Recurse -Force
    Write-Success "Web UI files copied"
}

# Step 8: Create production configuration files
Write-Step "Creating production configuration files..."

# API configuration
$apiConfig = @"
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
    "DefaultConnection": "Data Source=$($DATA_DIR.Replace('\', '\\'))\\bloodtracker.db;Cache=Shared;Journal Mode=WAL;Synchronous=NORMAL;"
  },
  "Database": {
    "Provider": "SQLite"
  },
  "Urls": "http://localhost:5234;http://0.0.0.0:5234",
  "Security": {
    "RequireHttps": false,
    "EnableCors": true,
    "AllowedOrigins": [
      "http://localhost:5235",
      "http://127.0.0.1:5235"
    ]
  },
  "MedicalApplication": {
    "Name": "Blood Thinner Medication & INR Tracker",
    "Version": "1.0.0",
    "ComplianceLevel": "InternalUseOnly",
    "EnableAuditLogging": true
  }
}
"@

$apiConfig | Out-File -FilePath "$INSTALL_DIR\api\appsettings.Production.json" -Encoding UTF8 -Force
Write-Success "API configuration created"

if ($DEPLOY_WEB) {
    # Web configuration
    $webConfig = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://localhost:5235;http://0.0.0.0:5235",
  "ApiBaseUrl": "http://localhost:5234"
}
"@
    
    $webConfig | Out-File -FilePath "$INSTALL_DIR\web\appsettings.Production.json" -Encoding UTF8 -Force
    Write-Success "Web UI configuration created"
}

# Step 9: Configure Windows Firewall
Write-Step "Configuring Windows Firewall..."
$firewallRuleApi = Get-NetFirewallRule -DisplayName "Blood Thinner Tracker API" -ErrorAction SilentlyContinue
if (-not $firewallRuleApi) {
    New-NetFirewallRule -DisplayName "Blood Thinner Tracker API" `
        -Direction Inbound `
        -LocalPort 5234 `
        -Protocol TCP `
        -Action Allow `
        -Profile Any | Out-Null
    Write-Success "Firewall rule created for API (port 5234)"
} else {
    Write-Host "Firewall rule for API already exists"
}

if ($DEPLOY_WEB) {
    $firewallRuleWeb = Get-NetFirewallRule -DisplayName "Blood Thinner Tracker Web" -ErrorAction SilentlyContinue
    if (-not $firewallRuleWeb) {
        New-NetFirewallRule -DisplayName "Blood Thinner Tracker Web" `
            -Direction Inbound `
            -LocalPort 5235 `
            -Protocol TCP `
            -Action Allow `
            -Profile Any | Out-Null
        Write-Success "Firewall rule created for Web UI (port 5235)"
    } else {
        Write-Host "Firewall rule for Web UI already exists"
    }
}

# Step 10: Create/Update Windows Services
Write-Step "Setting up Windows Services..."

# Determine the executable name based on AOT or not
$apiExe = if ($ENABLE_AOT) { "BloodThinnerTracker.Api.exe" } else { "BloodThinnerTracker.Api.exe" }
$webExe = if ($ENABLE_AOT) { "BloodThinnerTracker.Web.exe" } else { "BloodThinnerTracker.Web.exe" }

# API Service
$apiServiceExists = Get-Service -Name $API_SERVICE_NAME -ErrorAction SilentlyContinue
if ($apiServiceExists) {
    Write-Host "Updating existing API service..."
    # Service exists, we'll delete and recreate to ensure config is correct
    Stop-Service -Name $API_SERVICE_NAME -Force -ErrorAction SilentlyContinue
    & sc.exe delete $API_SERVICE_NAME | Out-Null
    Start-Sleep -Seconds 2
}

Write-Host "Creating API Windows Service..."
$apiServicePath = "`"$INSTALL_DIR\api\$apiExe`""
$apiServiceArgs = @(
    "create", $API_SERVICE_NAME,
    "binPath=", $apiServicePath,
    "start=", "auto",
    "DisplayName=", "Blood Thinner Tracker API"
)

& sc.exe @apiServiceArgs | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error-Message "Failed to create API service"
    exit 1
}

# Set service description
& sc.exe description $API_SERVICE_NAME "Blood Thinner Medication & INR Tracker API Service" | Out-Null

# Set service to restart on failure
& sc.exe failure $API_SERVICE_NAME reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

# Set environment variable for production
$apiRegPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$API_SERVICE_NAME"
if (Test-Path $apiRegPath) {
    New-ItemProperty -Path $apiRegPath -Name "Environment" -PropertyType MultiString -Value "ASPNETCORE_ENVIRONMENT=Production" -Force | Out-Null
}

Write-Success "API service created: $API_SERVICE_NAME"

# Web Service (optional)
if ($DEPLOY_WEB) {
    $webServiceExists = Get-Service -Name $WEB_SERVICE_NAME -ErrorAction SilentlyContinue
    if ($webServiceExists) {
        Write-Host "Updating existing Web service..."
        Stop-Service -Name $WEB_SERVICE_NAME -Force -ErrorAction SilentlyContinue
        & sc.exe delete $WEB_SERVICE_NAME | Out-Null
        Start-Sleep -Seconds 2
    }
    
    Write-Host "Creating Web UI Windows Service..."
    $webServicePath = "`"$INSTALL_DIR\web\$webExe`""
    $webServiceArgs = @(
        "create", $WEB_SERVICE_NAME,
        "binPath=", $webServicePath,
        "start=", "auto",
        "DisplayName=", "Blood Thinner Tracker Web UI"
    )
    
    & sc.exe @webServiceArgs | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Message "Failed to create Web service"
        exit 1
    }
    
    # Set service description
    & sc.exe description $WEB_SERVICE_NAME "Blood Thinner Medication & INR Tracker Web UI Service" | Out-Null
    
    # Set service to restart on failure and depend on API
    & sc.exe failure $WEB_SERVICE_NAME reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
    & sc.exe config $WEB_SERVICE_NAME depend= $API_SERVICE_NAME | Out-Null
    
    # Set environment variable for production
    $webRegPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$WEB_SERVICE_NAME"
    if (Test-Path $webRegPath) {
        New-ItemProperty -Path $webRegPath -Name "Environment" -PropertyType MultiString -Value "ASPNETCORE_ENVIRONMENT=Production" -Force | Out-Null
    }
    
    Write-Success "Web service created: $WEB_SERVICE_NAME"
}

# Step 11: Start services
Write-Step "Starting services..."
Start-Sleep -Seconds 2

Start-Service -Name $API_SERVICE_NAME
Write-Success "API service started"

if ($DEPLOY_WEB) {
    Start-Sleep -Seconds 5  # Give API time to start
    Start-Service -Name $WEB_SERVICE_NAME
    Write-Success "Web service started"
}

# Step 12: Wait for services to initialize
Write-Host ""
Write-Step "Waiting for services to initialize (10 seconds)..."
Start-Sleep -Seconds 10

# Step 13: Check service status
Write-Step "Checking service status..."
Write-Host ""

$apiServiceStatus = Get-Service -Name $API_SERVICE_NAME
Write-Host "API Service Status: $($apiServiceStatus.Status)" -ForegroundColor $(if ($apiServiceStatus.Status -eq 'Running') { 'Green' } else { 'Red' })

if ($DEPLOY_WEB) {
    $webServiceStatus = Get-Service -Name $WEB_SERVICE_NAME
    Write-Host "Web Service Status: $($webServiceStatus.Status)" -ForegroundColor $(if ($webServiceStatus.Status -eq 'Running') { 'Green' } else { 'Red' })
}

# Step 14: Test endpoints
Write-Host ""
Write-Step "Testing endpoints..."

Start-Sleep -Seconds 3

try {
    $healthResponse = Invoke-WebRequest -Uri "http://localhost:5234/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    if ($healthResponse.StatusCode -eq 200) {
        Write-Success "API is healthy"
    } else {
        Write-Error-Message "API health check returned status: $($healthResponse.StatusCode)"
    }
} catch {
    Write-Error-Message "API health check failed: $($_.Exception.Message)"
    Write-Host "Check Event Viewer or service logs for details" -ForegroundColor Yellow
}

# Step 15: Display access information
Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Installation Details:"
Write-Host "  Install Directory: $INSTALL_DIR"
Write-Host "  Data Directory: $DATA_DIR"
Write-Host "  Database: $DATA_DIR\bloodtracker.db"
Write-Host ""
Write-Host "Access URLs:"
Write-Host "  API:     http://localhost:5234/scalar/v1"
Write-Host "  API:     http://<your-ip>:5234/scalar/v1"
if ($DEPLOY_WEB) {
    Write-Host "  Web UI:  http://localhost:5235"
    Write-Host "  Web UI:  http://<your-ip>:5235"
}
Write-Host ""
Write-Host "Windows Services:"
Write-Host "  API Service:     $API_SERVICE_NAME"
if ($DEPLOY_WEB) {
    Write-Host "  Web Service:     $WEB_SERVICE_NAME"
}
Write-Host ""
Write-Host "Useful Commands:"
Write-Host "  Check status:    Get-Service -Name $API_SERVICE_NAME"
Write-Host "  Stop service:    Stop-Service -Name $API_SERVICE_NAME"
Write-Host "  Start service:   Start-Service -Name $API_SERVICE_NAME"
Write-Host "  Restart service: Restart-Service -Name $API_SERVICE_NAME"
Write-Host "  View logs:       Get-EventLog -LogName Application -Source '$API_SERVICE_NAME' -Newest 50"
Write-Host ""
Write-Host "Database Backup:"
Write-Host "  Database location: $DATA_DIR\bloodtracker.db"
Write-Host "  This database persists across updates"
Write-Host "  To backup: Copy-Item '$DATA_DIR\bloodtracker.db' -Destination '.\backup\'"
Write-Host ""

if ($ENABLE_AOT) {
    Write-Host "Native AOT Enabled:" -ForegroundColor Cyan
    Write-Host "  - Faster startup and lower memory usage" -ForegroundColor Cyan
    Write-Host "  - No .NET runtime installation required on target machine" -ForegroundColor Cyan
}

Write-Success "Deployment successful!"
Write-Host ""
Write-Host "⚠️  SECURITY NOTE: This deployment is for internal use only" -ForegroundColor Yellow
Write-Host "    - No HTTPS configured (add reverse proxy if needed)" -ForegroundColor Yellow
Write-Host "    - Firewall rules allow access from network" -ForegroundColor Yellow
Write-Host "    - Consider restricting access via Windows Firewall rules" -ForegroundColor Yellow
