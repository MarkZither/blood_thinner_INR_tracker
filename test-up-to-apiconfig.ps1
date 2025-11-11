# This script automates the deployment to Raspberry Pi for internal use
# Run from Windows machine
#Requires -Version 5.1

function Write-Step {
    param([string]$Message)
    Write-Host ">>> $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "âœ“ $Message" -ForegroundColor Green
}

function Write-Error-Message {
    param([string]$Message)
    Write-Host "âœ— $Message" -ForegroundColor Red
}

# Stop on errors
$ErrorActionPreference = "Stop"


# Configuration
$DEFAULT_PI_HOST = "raspberrypi"
$DEFAULT_PI_USER = "pi"
$PUBLISH_DIR = ".\publish"
$API_PROJ = "src\BloodThinnerTracker.Api\BloodThinnerTracker.Api.csproj"
$WEB_PROJ = "src\BloodThinnerTracker.Web\BloodThinnerTracker.Web.csproj"

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
    [switch]$SingleFile,
    [string]$Host = $DEFAULT_PI_HOST,
    [string]$User = $DEFAULT_PI_USER,
    [switch]$Help
)

if ($Help) {
    Write-Host 'Usage: .\deploy-to-pi.ps1 [OPTIONS]'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  -Web           Deploy Web UI in addition to API'
    Write-Host '  -SingleFile    Publish as single executable file'
    Write-Host '  -Host HOST     Raspberry Pi hostname or IP (default: raspberrypi)'
    Write-Host '  -User USER     SSH user (default: pi)'
    Write-Host '  -Help          Show this help message'
    Write-Host ''
    Write-Host 'Examples:'
    Write-Host '  .\deploy-to-pi.ps1                                 # Deploy API only'
    Write-Host '  .\deploy-to-pi.ps1 -Web                            # Deploy API and Web'
    Write-Host '  .\deploy-to-pi.ps1 -Host 192.168.1.100 -User pi    # Custom host'
    Write-Host '  .\deploy-to-pi.ps1 -SingleFile                     # Single-file deployment'
    exit 0
}

$PI_HOST = $Host
$PI_USER = $User
$DEPLOY_WEB = $Web.IsPresent
$SINGLE_FILE = $SingleFile.IsPresent

Write-Host ""
Write-Host "Deployment Configuration:"
Write-Host "  Target: $PI_USER@$PI_HOST"
Write-Host "  Deploy Web: $DEPLOY_WEB"
Write-Host "  Single File: $SINGLE_FILE"
Write-Host ""

# Confirm deployment
$confirmation = Read-Host "Continue with deployment? (y/N)"
if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
    Write-Error-Message "Deployment cancelled"
    exit 1
}

# Step 1: Clean previous publish
Write-Step "Cleaning previous publish directory..."
if (Test-Path $PUBLISH_DIR) {
    Remove-Item -Path $PUBLISH_DIR -Recurse -Force
}
New-Item -Path "$PUBLISH_DIR\api" -ItemType Directory -Force | Out-Null
if ($DEPLOY_WEB) {
    New-Item -Path "$PUBLISH_DIR\web" -ItemType Directory -Force | Out-Null
}
Write-Success "Cleaned publish directory"

# Step 2: Publish API
Write-Step "Publishing API..."

$publishArgs = @(
    "publish", $API_PROJ,
    "--configuration", "Release",
    "--runtime", "linux-arm64",
    "--self-contained", "true",
    "--output", "$PUBLISH_DIR\api"
)

if ($SINGLE_FILE) {
    $publishArgs += "-p:PublishSingleFile=true"
    $publishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
    $publishArgs += "-p:PublishTrimmed=false"
} else {
    $publishArgs += "-p:PublishSingleFile=false"
    $publishArgs += "-p:PublishTrimmed=false"
}

& dotnet $publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error-Message "API publish failed"
    exit 1
}

Write-Success "API published to $PUBLISH_DIR\api"

# Step 3: Publish Web (optional)
if ($DEPLOY_WEB) {
    Write-Step "Publishing Web UI..."

    $webPublishArgs = @(
        "publish", $WEB_PROJ,
        "--configuration", "Release",
        "--runtime", "linux-arm64",
        "--self-contained", "true",
        "--output", "$PUBLISH_DIR\web"
    )

    if ($SINGLE_FILE) {
        $webPublishArgs += "-p:PublishSingleFile=true"
        $webPublishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
        $webPublishArgs += "-p:PublishTrimmed=false"
    } else {
        $webPublishArgs += "-p:PublishSingleFile=false"
        $webPublishArgs += "-p:PublishTrimmed=false"
    }

    & dotnet $webPublishArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Message "Web UI publish failed"
        exit 1
    }

    Write-Success "Web UI published to $PUBLISH_DIR\web"
}

# Check if ssh is available (requires OpenSSH Client on Windows)
Write-Step "Checking SSH availability..."
$sshAvailable = $null
try {
    $sshAvailable = Get-Command ssh -ErrorAction SilentlyContinue
} catch {
    Write-Error-Message 'SSH client not found. Please install OpenSSH Client for Windows.'
    Write-Host 'To install: Settings > Apps > Optional Features > Add OpenSSH Client'
    exit 1
}

# Check if scp is available
$scpAvailable = $null
try {
    $scpAvailable = Get-Command scp -ErrorAction SilentlyContinue
} catch {
    Write-Error-Message 'SCP not found. Please install OpenSSH Client for Windows.'
    exit 1
}

Write-Success "SSH and SCP are available"

# Step 4: Check SSH connectivity
Write-Step "Testing SSH connection to ${PI_USER}@${PI_HOST}..."
$sshTest = ssh -o ConnectTimeout=5 "${PI_USER}@${PI_HOST}" "echo 'SSH connection successful'" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error-Message "Cannot connect to ${PI_USER}@${PI_HOST}"
    Write-Host "Please ensure:"
    Write-Host "  1. Raspberry Pi is powered on and connected to network"
    Write-Host "  2. SSH is enabled on Raspberry Pi"
    Write-Host "  3. Hostname/IP is correct"
    Write-Host "  4. SSH key is configured or password is available"
    exit 1
}
Write-Success "SSH connection successful"

# Step 5: Create directories on Raspberry Pi
Write-Step "Creating directories on Raspberry Pi..."
ssh "${PI_USER}@${PI_HOST}" 'sudo mkdir -p /opt/bloodtracker/api /opt/bloodtracker/web /var/lib/bloodtracker; sudo chown -R ${USER}:${USER} /opt/bloodtracker /var/lib/bloodtracker'
if ($LASTEXITCODE -ne 0) {
    Write-Error-Message 'Failed to create directories'
    exit 1
}
Write-Success 'Directories created'

# Step 6: Backup current deployment (if exists)
Write-Step "Backing up current deployment..."
ssh "${PI_USER}@${PI_HOST}" "if [ -d /opt/bloodtracker/api/BloodThinnerTracker.Api ]; then sudo cp -r /opt/bloodtracker/api /opt/bloodtracker/api.backup.`$(date +%Y%m%d_%H%M%S); echo 'Backup created'; else echo 'No existing deployment to backup'; fi"

# Step 7: Stop services if running
Write-Step "Stopping services..."
ssh "${PI_USER}@${PI_HOST}" 'sudo systemctl stop bloodtracker-api.service 2>/dev/null; sudo systemctl stop bloodtracker-web.service 2>/dev/null'
Write-Success 'Services stopped'

# Step 8: Transfer API files
Write-Step "Transferring API files to Raspberry Pi..."
# Use scp for Windows (recursive copy)
scp -r "$PUBLISH_DIR\api\*" "${PI_USER}@${PI_HOST}:/opt/bloodtracker/api/"
if ($LASTEXITCODE -ne 0) {
    Write-Error-Message "Failed to transfer API files"
    exit 1
}
Write-Success "API files transferred"

# Step 9: Transfer Web files (optional)
if ($DEPLOY_WEB) {
    Write-Step "Transferring Web UI files to Raspberry Pi..."
    scp -r "$PUBLISH_DIR\web\*" "${PI_USER}@${PI_HOST}:/opt/bloodtracker/web/"
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Message "Failed to transfer Web UI files"
        exit 1
    }
    Write-Success "Web UI files transferred"
}

# Step 10: Create/update configuration files
Write-Step "Creating configuration files..."

# API configuration
$apiConfig = @'
{
