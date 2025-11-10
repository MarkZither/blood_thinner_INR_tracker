# Blood Thinner Tracker - Raspberry Pi Deployment Script (PowerShell)
# This script automates the deployment to Raspberry Pi for internal use
# Run from Windows machine
#Requires -Version 5.1

# Parse command line arguments
param(
    [switch]$Web,
    [switch]$SingleFile,
    [string]$PiHost = "raspberrypi",
    [string]$User = "pi",
    [string]$SshKey = "",
    [switch]$Help
)

function Write-Step {
    param([string]$Message)
    Write-Host ">>> $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Error-Message {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Helper function to execute SSH command with optional key
function Invoke-RemoteCommand {
    param([string]$Command)
    # Allocate TTY if using doas (required for interactive password prompt)
    $ttyFlag = ""
    if ($SUDO_CMD -eq "doas") {
        $ttyFlag = "-t"
    }

    if ($SSH_KEY_ARG -ne "") {
        Invoke-Expression "ssh $ttyFlag $SSH_KEY_ARG `"${PI_USER}@${PI_HOST}`" `"$Command`""
    } else {
        Invoke-Expression "ssh $ttyFlag `"${PI_USER}@${PI_HOST}`" `"$Command`""
    }
}

# Helper function to execute SCP command with optional key
function Invoke-SecureCopy {
    param([string]$Source, [string]$Destination)
    if ($SSH_KEY_ARG -ne "") {
        Invoke-Expression "scp $SSH_KEY_ARG -r `"$Source`" `"${PI_USER}@${PI_HOST}:$Destination`""
    } else {
        Invoke-Expression "scp -r `"$Source`" `"${PI_USER}@${PI_HOST}:$Destination`""
    }
}# Stop on errors
$ErrorActionPreference = "Stop"

# Configuration
$PUBLISH_DIR = ".\publish"
$API_PROJ = "src\BloodThinnerTracker.Api\BloodThinnerTracker.Api.csproj"
$WEB_PROJ = "src\BloodThinnerTracker.Web\BloodThinnerTracker.Web.csproj"

if ($Help) {
    Write-Host 'Usage: .\deploy-to-pi.ps1 [OPTIONS]'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  -Web           Deploy Web UI in addition to API'
    Write-Host '  -SingleFile    Publish as single executable file'
    Write-Host '  -PiHost HOST   Raspberry Pi hostname or IP (default: raspberrypi)'
    Write-Host '  -User USER     SSH user (default: pi)'
    Write-Host '  -SshKey PATH   Path to SSH private key file (optional, uses password auth if not provided)'
    Write-Host '  -Help          Show this help message'
    Write-Host ''
    Write-Host 'Examples:'
    Write-Host '  .\deploy-to-pi.ps1                                           # Deploy API only (password auth)'
    Write-Host '  .\deploy-to-pi.ps1 -Web                                      # Deploy API and Web'
    Write-Host '  .\deploy-to-pi.ps1 -PiHost 192.168.1.100 -User pi            # Custom host'
    Write-Host '  .\deploy-to-pi.ps1 -SingleFile                               # Single-file deployment'
    Write-Host '  .\deploy-to-pi.ps1 -SshKey "$env:USERPROFILE\.ssh\id_rsa"  # Use SSH key auth'
    Write-Host '  .\deploy-to-pi.ps1 -Web -SshKey "C:\keys\pi_key"            # SSH key with Web deployment'
    exit 0
}

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

# Validate SSH key if provided
$SSH_KEY_ARG = ""
$SSH_KEY_PATH = ""
if ($SshKey -ne "") {
    if (-not (Test-Path $SshKey)) {
        Write-Error-Message "SSH key file not found: $SshKey"
        exit 1
    }
    $SSH_KEY_ARG = "-i `"$SshKey`""
    $SSH_KEY_PATH = $SshKey
    Write-Success "Using SSH key authentication: $SshKey"
} else {
    Write-Host "Using password authentication" -ForegroundColor Yellow
    Write-Host "Note: You will be prompted for password multiple times during deployment" -ForegroundColor Yellow
    Write-Host "      Consider using -SshKey for key-based auth to avoid password prompts" -ForegroundColor Yellow
}

$PI_HOST = $PiHost
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
if ($SSH_KEY_ARG -ne "") {
    $sshTestCmd = "ssh $SSH_KEY_ARG -o ConnectTimeout=5 `"${PI_USER}@${PI_HOST}`" `"echo 'SSH connection successful'`""
} else {
    $sshTestCmd = "ssh -o ConnectTimeout=5 `"${PI_USER}@${PI_HOST}`" `"echo 'SSH connection successful'`""
}
$sshTest = Invoke-Expression $sshTestCmd 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error-Message "Cannot connect to ${PI_USER}@${PI_HOST}"
    Write-Host "Please ensure:"
    Write-Host "  1. Raspberry Pi is powered on and connected to network"
    Write-Host "  2. SSH is enabled on Raspberry Pi"
    Write-Host "  3. Hostname/IP is correct"
    Write-Host "  4. SSH key is configured or password is available"
    Write-Host ""
    Write-Host "Error details: $sshTest"
    exit 1
}
Write-Success "SSH connection successful"

# Detect sudo or doas
Write-Step "Detecting privilege escalation command (sudo/doas)..."
$sudoDetection = Invoke-RemoteCommand "if command -v sudo >/dev/null 2>&1; then echo 'sudo'; elif command -v doas >/dev/null 2>&1; then echo 'doas'; else echo 'none'; fi"

if (-not $sudoDetection) {
    Write-Error-Message "Failed to detect privilege escalation command (connection error)"
    Write-Host "The SSH connection may have been interrupted. Please try again."
    exit 1
}

$SUDO_CMD = $sudoDetection.Trim()

if ($SUDO_CMD -eq "none" -or $SUDO_CMD -eq "") {
    Write-Error-Message "Neither sudo nor doas found on target system"
    Write-Host "Please install either sudo or doas, or run deployment as root"
    exit 1
}

Write-Success "Using privilege escalation: $SUDO_CMD"

# Configure doas for non-interactive use if needed
if ($SUDO_CMD -eq "doas") {
    Write-Step "Configuring doas for non-interactive deployment..."
    # Check if user already has nopass configured
    $doasCheck = Invoke-RemoteCommand "doas -n true 2>&1"
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "DOAS CONFIGURATION REQUIRED:" -ForegroundColor Cyan
        Write-Host "  You will be prompted for your password multiple times by doas during deployment." -ForegroundColor Yellow
        Write-Host "  This is normal - SSH key authentication only handles SSH login, not privilege escalation." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  To avoid password prompts in future deployments, you can configure doas with nopass:" -ForegroundColor Cyan
        Write-Host "    1. SSH to your Alpine system: ssh ${PI_USER}@${PI_HOST}" -ForegroundColor White
        Write-Host "    2. Edit doas config: doas vi /etc/doas.conf" -ForegroundColor White
        Write-Host "    3. Add this line: permit nopass ${PI_USER}" -ForegroundColor White
        Write-Host ""
        Write-Host "  Press Enter to continue with deployment (you'll need to enter password when prompted)..."
        $null = Read-Host
    } else {
        Write-Success "doas configured for passwordless operation"
    }
}

Write-Success "Using privilege escalation: $SUDO_CMD"

# Step 5: Create directories on Raspberry Pi
Write-Step "Creating directories on Raspberry Pi..."
Invoke-RemoteCommand "$SUDO_CMD mkdir -p /opt/bloodtracker/api /opt/bloodtracker/web /var/lib/bloodtracker; $SUDO_CMD chown -R `${USER}:`${USER} /opt/bloodtracker /var/lib/bloodtracker"
if ($LASTEXITCODE -ne 0) {
    Write-Error-Message 'Failed to create directories'
    exit 1
}
Write-Success 'Directories created'

# Step 6: Backup current deployment (if exists)
Write-Step "Backing up current deployment..."
# Note: Using direct SSH call to avoid PowerShell parsing the date command
# Build the command carefully to avoid shell syntax errors
$backupShellCmd = 'if [ -d /opt/bloodtracker/api/BloodThinnerTracker.Api ]; then ' + $SUDO_CMD + ' cp -r /opt/bloodtracker/api /opt/bloodtracker/api.backup.$(date +%Y%m%d_%H%M%S); echo "Backup created"; else echo "No existing deployment to backup"; fi'
if ($SSH_KEY_PATH -ne "") {
    & ssh -i $SSH_KEY_PATH "${PI_USER}@${PI_HOST}" $backupShellCmd
} else {
    & ssh "${PI_USER}@${PI_HOST}" $backupShellCmd
}

# Step 7: Stop services if running
Write-Step "Stopping services..."
Invoke-RemoteCommand "$SUDO_CMD systemctl stop bloodtracker-api.service 2>/dev/null; $SUDO_CMD systemctl stop bloodtracker-web.service 2>/dev/null"
Write-Success 'Services stopped'

# Step 8: Transfer API files
Write-Step "Transferring API files to Raspberry Pi..."
# Use scp for Windows (recursive copy)
Invoke-SecureCopy "$PUBLISH_DIR\api\*" "/opt/bloodtracker/api/"
if ($LASTEXITCODE -ne 0) {
    Write-Error-Message "Failed to transfer API files"
    exit 1
}
Write-Success "API files transferred"

# Step 9: Transfer Web files (optional)
if ($DEPLOY_WEB) {
    Write-Step "Transferring Web UI files to Raspberry Pi..."
    Invoke-SecureCopy "$PUBLISH_DIR\web\*" "/opt/bloodtracker/web/"
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Message "Failed to transfer Web UI files"
        exit 1
    }
    Write-Success "Web UI files transferred"
}

# Step 10: Create/update configuration files
Write-Step "Creating configuration files..."

# API configuration - Using direct SSH to avoid Invoke-Expression parsing issues
$apiConfigCmd = @"
cat > /opt/bloodtracker/api/appsettings.Production.json << 'EOFCONFIG'
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
        "DefaultConnection": "Data Source=/var/lib/bloodtracker/bloodtracker.db;Cache=Shared;"
    },
    "Database": {
        "Provider": "SQLite"
    },
    "Urls": "http://0.0.0.0:5234",
    "Security": {
        "RequireHttps": false,
        "EnableCors": true,
        "AllowedOrigins": [
            "http://localhost:5235",
            "http://raspberrypi:5235",
            "http://raspberrypi.local:5235"
        ]
    },
    "MedicalApplication": {
        "Name": "Blood Thinner Medication and INR Tracker",
        "Version": "1.0.0",
        "ComplianceLevel": "InternalUseOnly",
        "EnableAuditLogging": true
    }
}
EOFCONFIG
"@

# Use direct SSH call instead of Invoke-RemoteCommand to avoid parsing issues
if ($SSH_KEY_PATH -ne "") {
    & ssh -i $SSH_KEY_PATH "${PI_USER}@${PI_HOST}" $apiConfigCmd
} else {
    & ssh "${PI_USER}@${PI_HOST}" $apiConfigCmd
}

if ($DEPLOY_WEB) {
    # Web configuration
    $webConfigCmd = @"
cat > /opt/bloodtracker/web/appsettings.Production.json << 'EOFCONFIG'
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "AllowedHosts": "*",
    "Urls": "http://0.0.0.0:5235",
    "ApiBaseUrl": "http://localhost:5234"
}
EOFCONFIG
"@

    # Use direct SSH call
    if ($SSH_KEY_PATH -ne "") {
        & ssh -i $SSH_KEY_PATH "${PI_USER}@${PI_HOST}" $webConfigCmd
    } else {
        & ssh "${PI_USER}@${PI_HOST}" $webConfigCmd
    }
}

Write-Success "Configuration files created"# Step 11: Setup systemd services

# Step 11: Setup service (systemd or OpenRC)
Write-Step "Detecting init system and setting up service files..."


# Detect init system (systemd or openrc)
$initSystem = Invoke-RemoteCommand "if [ -d /run/openrc ]; then echo 'openrc'; elif pidof systemd > /dev/null; then echo 'systemd'; else echo 'unknown'; fi".Trim()
Write-Host "Detected init system: $initSystem"


# Create bloodtracker user if doesn't exist
# Alpine Linux uses adduser (not useradd) with different syntax
Invoke-RemoteCommand "$SUDO_CMD adduser -D -s /sbin/nologin -H bloodtracker 2>/dev/null || $SUDO_CMD useradd -r -s /bin/false bloodtracker 2>/dev/null || true"

if ($initSystem -eq "openrc") {
    # OpenRC init scripts - use cat with here-document for proper shell handling
    $apiOpenRcCmd = @"
$SUDO_CMD sh -c 'cat > /etc/init.d/bloodtracker-api << '\''EOFSCRIPT'\''
#!/sbin/openrc-run
name="Blood Thinner Tracker API"
description="Blood Thinner Tracker API Service"
command="/opt/bloodtracker/api/BloodThinnerTracker.Api"
command_user="bloodtracker:bloodtracker"
directory="/opt/bloodtracker/api"
pidfile="/run/bloodtracker-api.pid"
command_background=true

depend() {
    need net
}

start_pre() {
    export ASPNETCORE_ENVIRONMENT=Production
}
EOFSCRIPT
chmod +x /etc/init.d/bloodtracker-api'
"@

    # Use direct SSH call to avoid Invoke-Expression parsing
    if ($SSH_KEY_PATH -ne "") {
        & ssh -i $SSH_KEY_PATH "${PI_USER}@${PI_HOST}" $apiOpenRcCmd
    } else {
        & ssh "${PI_USER}@${PI_HOST}" $apiOpenRcCmd
    }

    if ($DEPLOY_WEB) {
        $webOpenRcCmd = @"
$SUDO_CMD sh -c 'cat > /etc/init.d/bloodtracker-web << '\''EOFSCRIPT'\''
#!/sbin/openrc-run
name="Blood Thinner Tracker Web UI"
description="Blood Thinner Tracker Web UI Service"
command="/opt/bloodtracker/web/BloodThinnerTracker.Web"
command_user="bloodtracker:bloodtracker"
directory="/opt/bloodtracker/web"
pidfile="/run/bloodtracker-web.pid"
command_background=true

depend() {
    need net bloodtracker-api
}

start_pre() {
    export ASPNETCORE_ENVIRONMENT=Production
}
EOFSCRIPT
chmod +x /etc/init.d/bloodtracker-web'
"@

        # Use direct SSH call
        if ($SSH_KEY_PATH -ne "") {
            & ssh -i $SSH_KEY_PATH "${PI_USER}@${PI_HOST}" $webOpenRcCmd
        } else {
            & ssh "${PI_USER}@${PI_HOST}" $webOpenRcCmd
        }
    }
    Write-Success "OpenRC service scripts created"

    # Verify service files were created
    Write-Step "Verifying OpenRC service files..."
    $apiServiceCheck = Invoke-RemoteCommand "test -f /etc/init.d/bloodtracker-api && echo 'exists' || echo 'missing'"
    if ($apiServiceCheck -notmatch "exists") {
        Write-Error-Message "API service file was not created at /etc/init.d/bloodtracker-api"
        exit 1
    }
    if ($DEPLOY_WEB) {
        $webServiceCheck = Invoke-RemoteCommand "test -f /etc/init.d/bloodtracker-web && echo 'exists' || echo 'missing'"
        if ($webServiceCheck -notmatch "exists") {
            Write-Error-Message "Web service file was not created at /etc/init.d/bloodtracker-web"
            exit 1
        }
    }
    Write-Success "Service files verified"
} elseif ($initSystem -eq "systemd") {
    # systemd unit files - use cat with here-document for proper shell handling
    $apiServiceCmd = @"
$SUDO_CMD sh -c 'cat > /etc/systemd/system/bloodtracker-api.service << '\''EOFSERVICE'\''
[Unit]
Description=Blood Thinner Tracker API
After=network.target
Wants=network-online.target

[Service]
Type=notify
User=bloodtracker
Group=bloodtracker
WorkingDirectory=/opt/bloodtracker/api
ExecStart=/opt/bloodtracker/api/BloodThinnerTracker.Api
Environment=ASPNETCORE_ENVIRONMENT=Production

Restart=always
RestartSec=10

MemoryLimit=512M
CPUQuota=80%

StandardOutput=journal
StandardError=journal
SyslogIdentifier=bloodtracker-api

[Install]
WantedBy=multi-user.target
EOFSERVICE
'
"@

    # Use direct SSH call
    if ($SSH_KEY_PATH -ne "") {
        & ssh -i $SSH_KEY_PATH "${PI_USER}@${PI_HOST}" $apiServiceCmd
    } else {
        & ssh "${PI_USER}@${PI_HOST}" $apiServiceCmd
    }

    if ($DEPLOY_WEB) {
        $webServiceCmd = @"
$SUDO_CMD sh -c 'cat > /etc/systemd/system/bloodtracker-web.service << '\''EOFSERVICE'\''
[Unit]
Description=Blood Thinner Tracker Web UI
After=network.target bloodtracker-api.service
Wants=network-online.target
Requires=bloodtracker-api.service

[Service]
Type=notify
User=bloodtracker
Group=bloodtracker
WorkingDirectory=/opt/bloodtracker/web
ExecStart=/opt/bloodtracker/web/BloodThinnerTracker.Web
Environment=ASPNETCORE_ENVIRONMENT=Production

Restart=always
RestartSec=10

MemoryLimit=512M
CPUQuota=80%

StandardOutput=journal
StandardError=journal
SyslogIdentifier=bloodtracker-web

[Install]
WantedBy=multi-user.target
EOFSERVICE
'
"@

        # Use direct SSH call
        if ($SSH_KEY_PATH -ne "") {
            & ssh -i $SSH_KEY_PATH "${PI_USER}@${PI_HOST}" $webServiceCmd
        } else {
            & ssh "${PI_USER}@${PI_HOST}" $webServiceCmd
        }
    }
    Write-Success "Systemd services created"
} else {
    Write-Error-Message "Unknown init system. Please create service files manually."
}


# Step 12: Set permissions
Write-Step "Setting permissions..."
Invoke-RemoteCommand "$SUDO_CMD chown -R bloodtracker:bloodtracker /opt/bloodtracker /var/lib/bloodtracker; $SUDO_CMD chmod +x /opt/bloodtracker/api/BloodThinnerTracker.Api; $SUDO_CMD chmod 600 /opt/bloodtracker/api/appsettings.Production.json"

if ($DEPLOY_WEB) {
    Invoke-RemoteCommand "$SUDO_CMD chmod +x /opt/bloodtracker/web/BloodThinnerTracker.Web; $SUDO_CMD chmod 600 /opt/bloodtracker/web/appsettings.Production.json"
}


Write-Success "Permissions set"

# Step 13: Enable and start services based on init system
if ($initSystem -eq "openrc") {
    Write-Step "Enabling OpenRC services..."
    Invoke-RemoteCommand "$SUDO_CMD /sbin/rc-update add bloodtracker-api default"
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Message "Failed to enable API service"
        exit 1
    }

    if ($DEPLOY_WEB) {
        Invoke-RemoteCommand "$SUDO_CMD /sbin/rc-update add bloodtracker-web default"
        if ($LASTEXITCODE -ne 0) {
            Write-Error-Message "Failed to enable Web service"
            exit 1
        }
    }

    Write-Success "Services enabled"

    # Step 14: Start services
    Write-Step "Starting services..."
    Invoke-RemoteCommand "$SUDO_CMD /sbin/rc-service bloodtracker-api start"
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Message "Failed to start API service"
        Write-Host "Check service file: ssh ${PI_USER}@${PI_HOST} 'cat /etc/init.d/bloodtracker-api'"
        exit 1
    }

    if ($DEPLOY_WEB) {
        Invoke-RemoteCommand "$SUDO_CMD /sbin/rc-service bloodtracker-web start"
        if ($LASTEXITCODE -ne 0) {
            Write-Error-Message "Failed to start Web service"
            exit 1
        }
    }

    Write-Success "Services started"

    # Step 15: Wait for services to start
    Write-Host ""
    Write-Step "Waiting for services to start (10 seconds)..."
    Start-Sleep -Seconds 10

    # Step 16: Check service status
    Write-Step "Checking service status..."
    Write-Host ""
    Write-Host "API Service Status:"
    $apiStatus = Invoke-RemoteCommand "$SUDO_CMD /sbin/rc-service bloodtracker-api status"
    Write-Host $apiStatus

    if ($apiStatus -notmatch "started") {
        Write-Host "[WARNING] API service may not be running properly" -ForegroundColor Yellow
    }

    if ($DEPLOY_WEB) {
        Write-Host ""
        Write-Host "Web Service Status:"
        $webStatus = Invoke-RemoteCommand "$SUDO_CMD /sbin/rc-service bloodtracker-web status"
        Write-Host $webStatus

        if ($webStatus -notmatch "started") {
            Write-Host "[WARNING] Web service may not be running properly" -ForegroundColor Yellow
        }
    }
} elseif ($initSystem -eq "systemd") {
    Write-Step "Enabling systemd services..."
    Invoke-RemoteCommand "$SUDO_CMD systemctl daemon-reload; $SUDO_CMD systemctl enable bloodtracker-api.service"

    if ($DEPLOY_WEB) {
        Invoke-RemoteCommand "$SUDO_CMD systemctl enable bloodtracker-web.service"
    }

    Write-Success "Services enabled"

    # Step 14: Start services
    Write-Step "Starting services..."
    Invoke-RemoteCommand "$SUDO_CMD systemctl start bloodtracker-api.service"

    if ($DEPLOY_WEB) {
        Invoke-RemoteCommand "$SUDO_CMD systemctl start bloodtracker-web.service"
    }

    Write-Success "Services started"

    # Step 15: Wait for services to start
    Write-Host ""
    Write-Step "Waiting for services to start (10 seconds)..."
    Start-Sleep -Seconds 10

    # Step 16: Check service status
    Write-Step "Checking service status..."
    Write-Host ""
    Write-Host "API Service Status:"
    Invoke-RemoteCommand "$SUDO_CMD systemctl status bloodtracker-api.service --no-pager" 2>$null

    if ($DEPLOY_WEB) {
        Write-Host ""
        Write-Host "Web Service Status:"
        Invoke-RemoteCommand "$SUDO_CMD systemctl status bloodtracker-web.service --no-pager" 2>$null
    }
} else {
    Write-Error-Message "Unknown init system - cannot manage services"
    exit 1
}

# Step 17: Test endpoints
Write-Host ""
Write-Step "Testing endpoints..."

# Get Raspberry Pi IPs
$PI_LOCAL_IP = Invoke-RemoteCommand "hostname -I | awk '{print `$1}'"
$PI_TAILSCALE_IP = Invoke-RemoteCommand "tailscale ip -4 2>/dev/null; echo '(Tailscale not installed)'"

Write-Host ""
Write-Host "Testing API health endpoint..."
$healthCheck = Invoke-RemoteCommand "curl -s http://localhost:5234/health"
if ($healthCheck -match "Healthy") {
    Write-Success "API is healthy"
} else {
    Write-Error-Message "API health check failed"
}

# Step 18: Display access information
Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Access URLs:"
Write-Host "  Local network: http://${PI_LOCAL_IP}:5234/scalar/v1 (API)"
if ($PI_TAILSCALE_IP -ne "(Tailscale not installed)") {
    Write-Host "  Tailscale:     http://${PI_TAILSCALE_IP}:5234/scalar/v1 (API)"
}
Write-Host "  Hostname:      http://${PI_HOST}:5234/scalar/v1 (API)"
Write-Host ""

if ($DEPLOY_WEB) {
    Write-Host "  Local network: http://${PI_LOCAL_IP}:5235 (Web UI)"
    if ($PI_TAILSCALE_IP -ne "(Tailscale not installed)") {
        Write-Host "  Tailscale:     http://${PI_TAILSCALE_IP}:5235 (Web UI)"
    }
    Write-Host "  Hostname:      http://${PI_HOST}:5235 (Web UI)"
    Write-Host ""
}

Write-Host "Database location: /var/lib/bloodtracker/bloodtracker.db"
Write-Host ""
Write-Host "Useful commands:"
if ($initSystem -eq "openrc") {
    Write-Host "  Check status:   ssh ${PI_USER}@${PI_HOST} '$SUDO_CMD /sbin/rc-service bloodtracker-api status'"
    Write-Host "  View logs:      ssh ${PI_USER}@${PI_HOST} '$SUDO_CMD tail -f /var/log/messages | grep bloodtracker'"
    Write-Host "  Restart:        ssh ${PI_USER}@${PI_HOST} '$SUDO_CMD /sbin/rc-service bloodtracker-api restart'"
} else {
    Write-Host "  Check status:   ssh ${PI_USER}@${PI_HOST} '$SUDO_CMD systemctl status bloodtracker-api.service'"
    Write-Host "  View logs:      ssh ${PI_USER}@${PI_HOST} '$SUDO_CMD journalctl -u bloodtracker-api.service -f'"
    Write-Host "  Restart:        ssh ${PI_USER}@${PI_HOST} '$SUDO_CMD systemctl restart bloodtracker-api.service'"
}
Write-Host ""

Write-Success "Deployment successful!"
