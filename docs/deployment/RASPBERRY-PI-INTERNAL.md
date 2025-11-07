# Raspberry Pi Internal Deployment Guide

## Overview

This guide covers deploying the Blood Thinner & INR Tracker to a Raspberry Pi for **internal use only** on a private network (Tailscale/local LAN). This deployment is optimized for:

- ✅ **Bare metal** (no Docker required)
- ✅ **SQLite database** (persisted between updates)
- ✅ **Self-contained .NET** (no framework dependencies)
- ✅ **Internal network access** (192.168.x.x, 100.x.x.x, or Tailscale Magic DNS)
- ✅ **Systemd service** (automatic startup and management)

**⚠️ SECURITY NOTE**: This configuration is designed for internal-only use on a trusted network. Do not expose directly to the internet without additional security measures.

## Prerequisites

### Hardware Requirements
- **Raspberry Pi 4** (4GB+ RAM recommended) or **Raspberry Pi 5**
- **16GB+ SD card** or USB drive
- **Wired or WiFi network** connection
- **Power supply** (official recommended)

### Software Prerequisites
- **Raspberry Pi OS** (64-bit recommended for .NET 10)
  - Bullseye or Bookworm
  - Desktop or Lite edition
- **SSH access** enabled
- **Tailscale** (optional, for Magic DNS and remote access)

### Network Setup
Choose one or more access methods:
- **Local LAN**: Access via `192.168.x.x:5234`
- **Tailscale**: Access via `100.x.x.x:5234` or `hostname.tailnet-name.ts.net:5234`
- **Magic DNS**: Access via `raspberrypi:5234` (if enabled on Tailscale)

## Installation

### Step 1: Prepare Raspberry Pi

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install required dependencies (minimal - most dependencies included in self-contained)
sudo apt install -y curl wget unzip

# Create application directory
sudo mkdir -p /opt/bloodtracker
sudo chown $USER:$USER /opt/bloodtracker

# Create data directory for SQLite database
sudo mkdir -p /var/lib/bloodtracker
sudo chown $USER:$USER /var/lib/bloodtracker
```

### Step 2: Build Self-Contained Application

**On your development machine** (not the Raspberry Pi):

```bash
# Clone repository
git clone https://github.com/MarkZither/blood_thinner_INR_tracker.git
cd blood_thinner_INR_tracker

# Publish API as self-contained for Linux ARM64 (Raspberry Pi 4/5)
dotnet publish src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj \
  --configuration Release \
  --runtime linux-arm64 \
  --self-contained true \
  --output ./publish/api \
  -p:PublishSingleFile=false \
  -p:PublishTrimmed=false

# Publish Web as self-contained for Linux ARM64
dotnet publish src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj \
  --configuration Release \
  --runtime linux-arm64 \
  --self-contained true \
  --output ./publish/web \
  -p:PublishSingleFile=false \
  -p:PublishTrimmed=false
```

**Notes on build options:**
- `--self-contained true`: Includes .NET runtime (no SDK required on Pi)
- `--runtime linux-arm64`: Targets 64-bit Raspberry Pi
- `PublishSingleFile=false`: Multiple files for easier updates
- `PublishTrimmed=false`: Avoids potential trimming issues

**Alternative: Single-file deployment** (optional, for simpler deployment):
```bash
# Single-file API
dotnet publish src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj \
  --configuration Release \
  --runtime linux-arm64 \
  --self-contained true \
  --output ./publish/api-single \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

# Single-file Web
dotnet publish src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj \
  --configuration Release \
  --runtime linux-arm64 \
  --self-contained true \
  --output ./publish/web-single \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

### Step 3: Transfer Files to Raspberry Pi

```bash
# From your development machine, copy files to Raspberry Pi
# Replace 'raspberrypi' with your Pi's IP or hostname

# Copy API
rsync -avz --progress ./publish/api/ pi@raspberrypi:/opt/bloodtracker/api/

# Copy Web (optional - for Blazor UI)
rsync -avz --progress ./publish/web/ pi@raspberrypi:/opt/bloodtracker/web/

# Alternative: Using scp
scp -r ./publish/api/* pi@raspberrypi:/opt/bloodtracker/api/
scp -r ./publish/web/* pi@raspberrypi:/opt/bloodtracker/web/
```

### Step 4: Configure Application Settings

**On the Raspberry Pi:**

```bash
# Create production configuration for API
cat > /opt/bloodtracker/api/appsettings.Production.json << 'EOF'
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
  "Urls": "http://0.0.0.0:5234;https://0.0.0.0:7234",
  "Security": {
    "RequireHttps": false,
    "EnableCors": true,
    "AllowedOrigins": [
      "http://localhost:5235",
      "https://localhost:7235",
      "http://raspberrypi:5235",
      "http://raspberrypi.local:5235"
    ]
  },
  "MedicalApplication": {
    "Name": "Blood Thinner Medication & INR Tracker",
    "Version": "1.0.0",
    "ComplianceLevel": "InternalUseOnly",
    "EnableAuditLogging": true
  }
}
EOF

# Create production configuration for Web (optional)
cat > /opt/bloodtracker/web/appsettings.Production.json << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://0.0.0.0:5235;https://0.0.0.0:7235",
  "ApiBaseUrl": "http://localhost:5234"
}
EOF

# Set proper permissions
chmod 600 /opt/bloodtracker/api/appsettings.Production.json
chmod 600 /opt/bloodtracker/web/appsettings.Production.json
```

**Important Configuration Notes:**
- **SQLite path**: `/var/lib/bloodtracker/bloodtracker.db` - persists between updates
- **Binding**: `0.0.0.0` allows access from all network interfaces
- **HTTPS**: Disabled by default (use reverse proxy if needed)
- **CORS**: Allows Web UI to call API

### Step 5: Create Systemd Service Files

**API Service:**

```bash
sudo tee /etc/systemd/system/bloodtracker-api.service > /dev/null << 'EOF'
[Unit]
Description=Blood Thinner Tracker API
After=network.target
Wants=network-online.target

[Service]
Type=notify
# User and group to run as (create if needed)
User=bloodtracker
Group=bloodtracker

# Working directory
WorkingDirectory=/opt/bloodtracker/api

# Start the self-contained executable
ExecStart=/opt/bloodtracker/api/BloodThinnerTracker.Api
Environment=ASPNETCORE_ENVIRONMENT=Production

# Restart policy
Restart=always
RestartSec=10

# Resource limits (optional)
MemoryLimit=512M
CPUQuota=80%

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=bloodtracker-api

[Install]
WantedBy=multi-user.target
EOF
```

**Web Service (optional):**

```bash
sudo tee /etc/systemd/system/bloodtracker-web.service > /dev/null << 'EOF'
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
EOF
```

### Step 6: Create User and Set Permissions

```bash
# Create dedicated user for running the service
sudo useradd -r -s /bin/false bloodtracker

# Set ownership of application directories
sudo chown -R bloodtracker:bloodtracker /opt/bloodtracker
sudo chown -R bloodtracker:bloodtracker /var/lib/bloodtracker

# Make executables runnable
sudo chmod +x /opt/bloodtracker/api/BloodThinnerTracker.Api
sudo chmod +x /opt/bloodtracker/web/BloodThinnerTracker.Web
```

### Step 7: Initialize Database

```bash
# Switch to bloodtracker user
sudo -u bloodtracker bash

# Run API once to initialize database (will create SQLite file)
cd /opt/bloodtracker/api
ASPNETCORE_ENVIRONMENT=Production ./BloodThinnerTracker.Api &

# Wait a few seconds for database initialization
sleep 10

# Stop the test run
pkill -f BloodThinnerTracker.Api

# Exit back to pi user
exit

# Verify database was created
ls -lh /var/lib/bloodtracker/bloodtracker.db
```

### Step 8: Start Services

```bash
# Reload systemd to pick up new service files
sudo systemctl daemon-reload

# Enable services to start on boot
sudo systemctl enable bloodtracker-api.service
sudo systemctl enable bloodtracker-web.service  # Optional

# Start services
sudo systemctl start bloodtracker-api.service
sudo systemctl start bloodtracker-web.service  # Optional

# Check status
sudo systemctl status bloodtracker-api.service
sudo systemctl status bloodtracker-web.service
```

### Step 9: Verify Deployment

```bash
# Check if services are running
sudo systemctl status bloodtracker-api.service

# View logs
sudo journalctl -u bloodtracker-api.service -f

# Test API endpoint (from Raspberry Pi)
curl http://localhost:5234/health

# Test API endpoint (from another device on network)
curl http://raspberrypi:5234/health
# or
curl http://192.168.1.100:5234/health  # Replace with actual IP
# or (if using Tailscale)
curl http://100.x.x.x:5234/health
```

**Expected response:**
```
Healthy
```

### Step 10: Access from Network

**From your local network:**
```
http://raspberrypi:5234/scalar/v1    # API Documentation
http://raspberrypi:5235               # Web UI (if deployed)
```

**From Tailscale network:**
```
http://100.x.x.x:5234/scalar/v1       # Use actual Tailscale IP
http://raspberrypi.tailnet-name.ts.net:5234/scalar/v1  # Magic DNS
```

## Updating the Application

### Manual Update Process

```bash
# On development machine: Build new version
dotnet publish src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj \
  --configuration Release \
  --runtime linux-arm64 \
  --self-contained true \
  --output ./publish/api

# Stop services on Raspberry Pi
ssh pi@raspberrypi "sudo systemctl stop bloodtracker-api.service bloodtracker-web.service"

# Backup current version (optional)
ssh pi@raspberrypi "sudo cp -r /opt/bloodtracker/api /opt/bloodtracker/api.backup.$(date +%Y%m%d)"

# Transfer new files
rsync -avz --progress ./publish/api/ pi@raspberrypi:/opt/bloodtracker/api/

# Restore configuration (important!)
ssh pi@raspberrypi "sudo cp /opt/bloodtracker/api.backup.*/appsettings.Production.json /opt/bloodtracker/api/"

# Fix permissions
ssh pi@raspberrypi "sudo chown -R bloodtracker:bloodtracker /opt/bloodtracker/api && sudo chmod +x /opt/bloodtracker/api/BloodThinnerTracker.Api"

# Start services
ssh pi@raspberrypi "sudo systemctl start bloodtracker-api.service bloodtracker-web.service"

# Verify
ssh pi@raspberrypi "sudo systemctl status bloodtracker-api.service"
```

**Important**: The SQLite database at `/var/lib/bloodtracker/bloodtracker.db` is preserved during updates.

### Automated Update Script

Create this script on the Raspberry Pi:

```bash
sudo tee /usr/local/bin/bloodtracker-update.sh > /dev/null << 'EOF'
#!/bin/bash
set -e

# Configuration
APP_DIR="/opt/bloodtracker/api"
BACKUP_DIR="/opt/bloodtracker/backups"
SERVICE_NAME="bloodtracker-api.service"

echo "=== Blood Thinner Tracker Update Script ==="

# Create backup directory
mkdir -p "$BACKUP_DIR"

# Backup current version
BACKUP_PATH="$BACKUP_DIR/api.$(date +%Y%m%d_%H%M%S)"
echo "Creating backup at $BACKUP_PATH"
cp -r "$APP_DIR" "$BACKUP_PATH"

# Stop service
echo "Stopping service..."
systemctl stop "$SERVICE_NAME"

# Update files (assumes new files are in /tmp/bloodtracker-update/)
if [ -d "/tmp/bloodtracker-update" ]; then
    echo "Copying new files..."
    cp -r /tmp/bloodtracker-update/* "$APP_DIR/"
    
    # Restore configuration
    cp "$BACKUP_PATH/appsettings.Production.json" "$APP_DIR/"
    
    # Fix permissions
    chown -R bloodtracker:bloodtracker "$APP_DIR"
    chmod +x "$APP_DIR/BloodThinnerTracker.Api"
    
    # Start service
    echo "Starting service..."
    systemctl start "$SERVICE_NAME"
    
    # Check status
    sleep 3
    systemctl status "$SERVICE_NAME" --no-pager
    
    echo "Update completed successfully!"
    echo "Database preserved at /var/lib/bloodtracker/bloodtracker.db"
else
    echo "ERROR: Update files not found at /tmp/bloodtracker-update/"
    echo "Restoring from backup..."
    systemctl start "$SERVICE_NAME"
    exit 1
fi
EOF

sudo chmod +x /usr/local/bin/bloodtracker-update.sh
```

## Tailscale Configuration

### Install Tailscale on Raspberry Pi

```bash
# Install Tailscale
curl -fsSL https://tailscale.com/install.sh | sh

# Authenticate and connect
sudo tailscale up

# Enable MagicDNS (optional - for easy hostname access)
sudo tailscale up --accept-dns

# Get Tailscale IP
tailscale ip -4
```

### Access via Tailscale

After setup, you can access from any device on your Tailnet:

```
http://100.x.x.x:5234              # Tailscale IP
http://raspberrypi.tailnet.ts.net:5234  # MagicDNS (if enabled)
```

### Tailscale ACLs (Optional Security)

Restrict access to specific devices/users in Tailscale ACL editor:

```json
{
  "acls": [
    {
      "action": "accept",
      "src": ["user@example.com"],
      "dst": ["raspberrypi:5234", "raspberrypi:5235"]
    }
  ]
}
```

## Monitoring and Maintenance

### View Logs

```bash
# Real-time logs
sudo journalctl -u bloodtracker-api.service -f

# Last 100 lines
sudo journalctl -u bloodtracker-api.service -n 100

# Since last boot
sudo journalctl -u bloodtracker-api.service -b

# Filter by date
sudo journalctl -u bloodtracker-api.service --since "2025-01-01" --until "2025-01-02"
```

### Check Service Status

```bash
# Status
sudo systemctl status bloodtracker-api.service

# Is running?
sudo systemctl is-active bloodtracker-api.service

# Is enabled at boot?
sudo systemctl is-enabled bloodtracker-api.service
```

### Restart Service

```bash
sudo systemctl restart bloodtracker-api.service
```

### Database Backup

```bash
# Manual backup
sudo cp /var/lib/bloodtracker/bloodtracker.db /var/lib/bloodtracker/bloodtracker.db.backup.$(date +%Y%m%d)

# Automated daily backup (add to crontab)
sudo crontab -e
# Add line:
0 2 * * * cp /var/lib/bloodtracker/bloodtracker.db /var/lib/bloodtracker/bloodtracker.db.backup.$(date +\%Y\%m\%d) && find /var/lib/bloodtracker/ -name "*.backup.*" -mtime +30 -delete
```

### Performance Monitoring

```bash
# Check memory usage
free -h

# Check disk usage
df -h /var/lib/bloodtracker

# Check service resource usage
systemd-cgtop
```

## Troubleshooting

### Service Won't Start

```bash
# Check detailed logs
sudo journalctl -u bloodtracker-api.service -xe

# Check executable permissions
ls -l /opt/bloodtracker/api/BloodThinnerTracker.Api

# Test manual start
sudo -u bloodtracker /opt/bloodtracker/api/BloodThinnerTracker.Api
```

### Can't Access from Network

```bash
# Check if service is listening
sudo netstat -tulpn | grep 5234

# Check firewall (if enabled)
sudo ufw status
sudo ufw allow 5234/tcp  # If needed

# Test from Raspberry Pi itself
curl http://localhost:5234/health
```

### Database Issues

```bash
# Check database file exists
ls -lh /var/lib/bloodtracker/bloodtracker.db

# Check permissions
sudo chown bloodtracker:bloodtracker /var/lib/bloodtracker/bloodtracker.db
sudo chmod 644 /var/lib/bloodtracker/bloodtracker.db

# View database schema (requires sqlite3)
sudo apt install sqlite3
sqlite3 /var/lib/bloodtracker/bloodtracker.db ".schema"
```

### High Memory Usage

```bash
# Adjust memory limit in service file
sudo nano /etc/systemd/system/bloodtracker-api.service
# Change: MemoryLimit=256M  (from 512M)

sudo systemctl daemon-reload
sudo systemctl restart bloodtracker-api.service
```

## Security Considerations

### Internal Network Only

This deployment is designed for **internal use only**:

✅ **Safe on private network**: Tailscale, home LAN
❌ **NOT for public internet**: No advanced security hardening

### Additional Security (Optional)

If you need to expose externally:

1. **Enable HTTPS** with Let's Encrypt
2. **Add authentication** (configure OAuth in appsettings.json)
3. **Use reverse proxy** (nginx, Caddy)
4. **Enable firewall** (ufw)
5. **Regular updates** (OS and application)

### Firewall Configuration (Optional)

```bash
# Enable firewall
sudo ufw enable

# Allow SSH
sudo ufw allow 22/tcp

# Allow API and Web ports
sudo ufw allow 5234/tcp
sudo ufw allow 5235/tcp

# Check status
sudo ufw status
```

## Performance Optimization

### Raspberry Pi Tuning

```bash
# Reduce swappiness (for SD card longevity)
echo "vm.swappiness=10" | sudo tee -a /etc/sysctl.conf

# Increase file handles
echo "fs.file-max = 100000" | sudo tee -a /etc/sysctl.conf

# Apply changes
sudo sysctl -p
```

### Database Optimization

```bash
# SQLite PRAGMA settings (add to connection string)
# In appsettings.Production.json:
"DefaultConnection": "Data Source=/var/lib/bloodtracker/bloodtracker.db;Cache=Shared;Journal Mode=WAL;Synchronous=NORMAL;"
```

**Explanation:**
- `Journal Mode=WAL`: Write-Ahead Logging (better concurrency)
- `Synchronous=NORMAL`: Balanced durability/performance
- `Cache=Shared`: Share cache between connections

## Appendix

### Alternative Deployment: NativeAOT (Advanced)

For even faster startup and lower memory usage, consider NativeAOT:

```bash
# Publish with NativeAOT (requires .NET 10)
dotnet publish src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj \
  --configuration Release \
  --runtime linux-arm64 \
  -p:PublishAot=true \
  --output ./publish/api-aot
```

**Considerations:**
- ✅ Faster startup
- ✅ Lower memory usage
- ❌ Longer build time
- ❌ Some .NET features not supported (reflection, etc.)
- ❌ Requires testing for compatibility

### System Resource Requirements

**Minimum:**
- 1GB RAM (with swap enabled)
- 2GB storage
- 1 CPU core

**Recommended:**
- 2GB+ RAM
- 8GB+ storage
- 2+ CPU cores (Raspberry Pi 4/5)

### Estimated Performance

On Raspberry Pi 4 (4GB):
- **API startup**: ~3-5 seconds
- **Memory usage**: ~150-250MB per service
- **Response time**: <100ms for typical requests
- **Concurrent users**: 5-10 (internal use)

---

**Deployment Type**: Bare Metal (Self-Contained)
**Platform**: Raspberry Pi 4/5 (Linux ARM64)
**Database**: SQLite (persisted)
**Network**: Internal only (Tailscale/LAN)
**Created**: November 2025
