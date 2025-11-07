#!/bin/bash
# Blood Thinner Tracker - Raspberry Pi Deployment Script
# This script automates the deployment to Raspberry Pi for internal use

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
DEFAULT_PI_HOST="raspberrypi"
DEFAULT_PI_USER="pi"
PUBLISH_DIR="./publish"
API_PROJ="src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj"
WEB_PROJ="src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj"

echo -e "${GREEN}=== Blood Thinner Tracker - Raspberry Pi Deployment ===${NC}"
echo ""

# Function to print step
print_step() {
    echo -e "${YELLOW}>>> $1${NC}"
}

# Function to print success
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

# Function to print error
print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    print_error "dotnet SDK not found. Please install .NET 10 SDK."
    exit 1
fi

print_success "dotnet SDK found: $(dotnet --version)"

# Parse command line arguments
DEPLOY_WEB=false
SINGLE_FILE=false
PI_HOST="${DEFAULT_PI_HOST}"
PI_USER="${DEFAULT_PI_USER}"

while [[ $# -gt 0 ]]; do
    case $1 in
        --web)
            DEPLOY_WEB=true
            shift
            ;;
        --single-file)
            SINGLE_FILE=true
            shift
            ;;
        --host)
            PI_HOST="$2"
            shift 2
            ;;
        --user)
            PI_USER="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --web           Deploy Web UI in addition to API"
            echo "  --single-file   Publish as single executable file"
            echo "  --host HOST     Raspberry Pi hostname or IP (default: raspberrypi)"
            echo "  --user USER     SSH user (default: pi)"
            echo "  --help          Show this help message"
            echo ""
            echo "Examples:"
            echo "  $0                                 # Deploy API only"
            echo "  $0 --web                           # Deploy API and Web"
            echo "  $0 --host 192.168.1.100 --user pi  # Custom host"
            echo "  $0 --single-file                   # Single-file deployment"
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo ""
echo "Deployment Configuration:"
echo "  Target: ${PI_USER}@${PI_HOST}"
echo "  Deploy Web: ${DEPLOY_WEB}"
echo "  Single File: ${SINGLE_FILE}"
echo ""

# Confirm deployment
read -p "Continue with deployment? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    print_error "Deployment cancelled"
    exit 1
fi

# Step 1: Clean previous publish
print_step "Cleaning previous publish directory..."
rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR/api"
if [ "$DEPLOY_WEB" = true ]; then
    mkdir -p "$PUBLISH_DIR/web"
fi
print_success "Cleaned publish directory"

# Step 2: Publish API
print_step "Publishing API..."

if [ "$SINGLE_FILE" = true ]; then
    dotnet publish "$API_PROJ" \
        --configuration Release \
        --runtime linux-arm64 \
        --self-contained true \
        --output "$PUBLISH_DIR/api" \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:PublishTrimmed=false
else
    dotnet publish "$API_PROJ" \
        --configuration Release \
        --runtime linux-arm64 \
        --self-contained true \
        --output "$PUBLISH_DIR/api" \
        -p:PublishSingleFile=false \
        -p:PublishTrimmed=false
fi

print_success "API published to $PUBLISH_DIR/api"

# Step 3: Publish Web (optional)
if [ "$DEPLOY_WEB" = true ]; then
    print_step "Publishing Web UI..."
    
    if [ "$SINGLE_FILE" = true ]; then
        dotnet publish "$WEB_PROJ" \
            --configuration Release \
            --runtime linux-arm64 \
            --self-contained true \
            --output "$PUBLISH_DIR/web" \
            -p:PublishSingleFile=true \
            -p:IncludeNativeLibrariesForSelfExtract=true \
            -p:PublishTrimmed=false
    else
        dotnet publish "$WEB_PROJ" \
            --configuration Release \
            --runtime linux-arm64 \
            --self-contained true \
            --output "$PUBLISH_DIR/web" \
            -p:PublishSingleFile=false \
            -p:PublishTrimmed=false
    fi
    
    print_success "Web UI published to $PUBLISH_DIR/web"
fi

# Step 4: Check SSH connectivity
print_step "Testing SSH connection to ${PI_USER}@${PI_HOST}..."
if ! ssh -o ConnectTimeout=5 "${PI_USER}@${PI_HOST}" "echo 'SSH connection successful'" &> /dev/null; then
    print_error "Cannot connect to ${PI_USER}@${PI_HOST}"
    echo "Please ensure:"
    echo "  1. Raspberry Pi is powered on and connected to network"
    echo "  2. SSH is enabled on Raspberry Pi"
    echo "  3. Hostname/IP is correct"
    echo "  4. SSH key is configured or password is available"
    exit 1
fi
print_success "SSH connection successful"

# Step 5: Create directories on Raspberry Pi
print_step "Creating directories on Raspberry Pi..."
ssh "${PI_USER}@${PI_HOST}" "sudo mkdir -p /opt/bloodtracker/api /opt/bloodtracker/web /var/lib/bloodtracker && \
    sudo chown -R \$USER:\$USER /opt/bloodtracker /var/lib/bloodtracker"
print_success "Directories created"

# Step 6: Backup current deployment (if exists)
print_step "Backing up current deployment..."
ssh "${PI_USER}@${PI_HOST}" "if [ -d /opt/bloodtracker/api/BloodThinnerTracker.Api ]; then \
    sudo cp -r /opt/bloodtracker/api /opt/bloodtracker/api.backup.\$(date +%Y%m%d_%H%M%S); \
    echo 'Backup created'; \
else \
    echo 'No existing deployment to backup'; \
fi"

# Step 7: Stop services if running
print_step "Stopping services..."
ssh "${PI_USER}@${PI_HOST}" "sudo systemctl stop bloodtracker-api.service 2>/dev/null || true; \
    sudo systemctl stop bloodtracker-web.service 2>/dev/null || true"
print_success "Services stopped"

# Step 8: Transfer API files
print_step "Transferring API files to Raspberry Pi..."
rsync -avz --progress "$PUBLISH_DIR/api/" "${PI_USER}@${PI_HOST}:/opt/bloodtracker/api/"
print_success "API files transferred"

# Step 9: Transfer Web files (optional)
if [ "$DEPLOY_WEB" = true ]; then
    print_step "Transferring Web UI files to Raspberry Pi..."
    rsync -avz --progress "$PUBLISH_DIR/web/" "${PI_USER}@${PI_HOST}:/opt/bloodtracker/web/"
    print_success "Web UI files transferred"
fi

# Step 10: Create/update configuration files
print_step "Creating configuration files..."

# API configuration
ssh "${PI_USER}@${PI_HOST}" "cat > /opt/bloodtracker/api/appsettings.Production.json << 'EOFCONFIG'
{
  \"Logging\": {
    \"LogLevel\": {
      \"Default\": \"Information\",
      \"Microsoft.AspNetCore\": \"Warning\",
      \"Microsoft.EntityFrameworkCore\": \"Warning\"
    }
  },
  \"AllowedHosts\": \"*\",
  \"ConnectionStrings\": {
    \"DefaultConnection\": \"Data Source=/var/lib/bloodtracker/bloodtracker.db;Cache=Shared;Journal Mode=WAL;Synchronous=NORMAL;\"
  },
  \"Database\": {
    \"Provider\": \"SQLite\"
  },
  \"Urls\": \"http://0.0.0.0:5234\",
  \"Security\": {
    \"RequireHttps\": false,
    \"EnableCors\": true,
    \"AllowedOrigins\": [
      \"http://localhost:5235\",
      \"http://raspberrypi:5235\",
      \"http://raspberrypi.local:5235\"
    ]
  },
  \"MedicalApplication\": {
    \"Name\": \"Blood Thinner Medication & INR Tracker\",
    \"Version\": \"1.0.0\",
    \"ComplianceLevel\": \"InternalUseOnly\",
    \"EnableAuditLogging\": true
  }
}
EOFCONFIG
"

if [ "$DEPLOY_WEB" = true ]; then
    # Web configuration
    ssh "${PI_USER}@${PI_HOST}" "cat > /opt/bloodtracker/web/appsettings.Production.json << 'EOFCONFIG'
{
  \"Logging\": {
    \"LogLevel\": {
      \"Default\": \"Information\",
      \"Microsoft.AspNetCore\": \"Warning\"
    }
  },
  \"AllowedHosts\": \"*\",
  \"Urls\": \"http://0.0.0.0:5235\",
  \"ApiBaseUrl\": \"http://localhost:5234\"
}
EOFCONFIG
"
fi

print_success "Configuration files created"

# Step 11: Setup systemd services
print_step "Setting up systemd services..."

# Create bloodtracker user if doesn't exist
ssh "${PI_USER}@${PI_HOST}" "sudo useradd -r -s /bin/false bloodtracker 2>/dev/null || true"

# API service
ssh "${PI_USER}@${PI_HOST}" "sudo tee /etc/systemd/system/bloodtracker-api.service > /dev/null << 'EOFSERVICE'
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
"

if [ "$DEPLOY_WEB" = true ]; then
    # Web service
    ssh "${PI_USER}@${PI_HOST}" "sudo tee /etc/systemd/system/bloodtracker-web.service > /dev/null << 'EOFSERVICE'
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
"
fi

print_success "Systemd services created"

# Step 12: Set permissions
print_step "Setting permissions..."
ssh "${PI_USER}@${PI_HOST}" "sudo chown -R bloodtracker:bloodtracker /opt/bloodtracker /var/lib/bloodtracker && \
    sudo chmod +x /opt/bloodtracker/api/BloodThinnerTracker.Api && \
    sudo chmod 600 /opt/bloodtracker/api/appsettings.Production.json"

if [ "$DEPLOY_WEB" = true ]; then
    ssh "${PI_USER}@${PI_HOST}" "sudo chmod +x /opt/bloodtracker/web/BloodThinnerTracker.Web && \
        sudo chmod 600 /opt/bloodtracker/web/appsettings.Production.json"
fi

print_success "Permissions set"

# Step 13: Reload systemd and enable services
print_step "Enabling services..."
ssh "${PI_USER}@${PI_HOST}" "sudo systemctl daemon-reload && \
    sudo systemctl enable bloodtracker-api.service"

if [ "$DEPLOY_WEB" = true ]; then
    ssh "${PI_USER}@${PI_HOST}" "sudo systemctl enable bloodtracker-web.service"
fi

print_success "Services enabled"

# Step 14: Start services
print_step "Starting services..."
ssh "${PI_USER}@${PI_HOST}" "sudo systemctl start bloodtracker-api.service"

if [ "$DEPLOY_WEB" = true ]; then
    ssh "${PI_USER}@${PI_HOST}" "sudo systemctl start bloodtracker-web.service"
fi

print_success "Services started"

# Step 15: Wait for services to start
echo ""
print_step "Waiting for services to start (10 seconds)..."
sleep 10

# Step 16: Check service status
print_step "Checking service status..."
echo ""
echo "API Service Status:"
ssh "${PI_USER}@${PI_HOST}" "sudo systemctl status bloodtracker-api.service --no-pager" || true

if [ "$DEPLOY_WEB" = true ]; then
    echo ""
    echo "Web Service Status:"
    ssh "${PI_USER}@${PI_HOST}" "sudo systemctl status bloodtracker-web.service --no-pager" || true
fi

# Step 17: Test endpoints
echo ""
print_step "Testing endpoints..."

# Get Raspberry Pi IPs
PI_LOCAL_IP=$(ssh "${PI_USER}@${PI_HOST}" "hostname -I | awk '{print \$1}'")
PI_TAILSCALE_IP=$(ssh "${PI_USER}@${PI_HOST}" "tailscale ip -4 2>/dev/null || echo '(Tailscale not installed)'")

echo ""
echo "Testing API health endpoint..."
if ssh "${PI_USER}@${PI_HOST}" "curl -s http://localhost:5234/health" | grep -q "Healthy"; then
    print_success "API is healthy"
else
    print_error "API health check failed"
fi

# Step 18: Display access information
echo ""
echo -e "${GREEN}=== Deployment Complete ===${NC}"
echo ""
echo "Access URLs:"
echo "  Local network: http://${PI_LOCAL_IP}:5234/scalar/v1 (API)"
if [ "$PI_TAILSCALE_IP" != "(Tailscale not installed)" ]; then
    echo "  Tailscale:     http://${PI_TAILSCALE_IP}:5234/scalar/v1 (API)"
fi
echo "  Hostname:      http://${PI_HOST}:5234/scalar/v1 (API)"
echo ""

if [ "$DEPLOY_WEB" = true ]; then
    echo "  Local network: http://${PI_LOCAL_IP}:5235 (Web UI)"
    if [ "$PI_TAILSCALE_IP" != "(Tailscale not installed)" ]; then
        echo "  Tailscale:     http://${PI_TAILSCALE_IP}:5235 (Web UI)"
    fi
    echo "  Hostname:      http://${PI_HOST}:5235 (Web UI)"
    echo ""
fi

echo "Database location: /var/lib/bloodtracker/bloodtracker.db"
echo ""
echo "Useful commands:"
echo "  Check status:   ssh ${PI_USER}@${PI_HOST} 'sudo systemctl status bloodtracker-api.service'"
echo "  View logs:      ssh ${PI_USER}@${PI_HOST} 'sudo journalctl -u bloodtracker-api.service -f'"
echo "  Restart:        ssh ${PI_USER}@${PI_HOST} 'sudo systemctl restart bloodtracker-api.service'"
echo ""

print_success "Deployment successful!"
