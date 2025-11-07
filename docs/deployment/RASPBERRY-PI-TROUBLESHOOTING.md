# Raspberry Pi Deployment - Troubleshooting Guide

This guide helps resolve common issues when deploying Blood Thinner Tracker to Raspberry Pi.

## Quick Diagnostics

Run this script on your Raspberry Pi to check the deployment health:

```bash
#!/bin/bash
echo "=== Blood Thinner Tracker Diagnostics ==="
echo ""

echo "1. Service Status:"
sudo systemctl status bloodtracker-api.service --no-pager
echo ""

echo "2. Service Logs (last 20 lines):"
sudo journalctl -u bloodtracker-api.service -n 20 --no-pager
echo ""

echo "3. Network Listening:"
sudo netstat -tulpn | grep -E '5234|5235' || echo "Not listening on ports 5234/5235"
echo ""

echo "4. Database Status:"
if [ -f /var/lib/bloodtracker/bloodtracker.db ]; then
    ls -lh /var/lib/bloodtracker/bloodtracker.db
else
    echo "Database file not found!"
fi
echo ""

echo "5. Application Files:"
ls -lh /opt/bloodtracker/api/ | head -10
echo ""

echo "6. Local API Health Check:"
curl -s http://localhost:5234/health || echo "API not responding"
echo ""

echo "7. Memory Usage:"
free -h
echo ""

echo "8. Disk Usage:"
df -h /var/lib/bloodtracker
echo ""
```

Save as `/usr/local/bin/bloodtracker-diag.sh`, make executable with `chmod +x`, then run with `bloodtracker-diag.sh`.

---

## Common Issues and Solutions

### Issue 1: Service Won't Start

**Symptoms:**
- `sudo systemctl status bloodtracker-api.service` shows "failed" or "inactive"
- API not accessible

**Diagnostic Steps:**

```bash
# Check detailed service status
sudo systemctl status bloodtracker-api.service -l

# View full error logs
sudo journalctl -u bloodtracker-api.service -xe

# Check if executable exists and has correct permissions
ls -l /opt/bloodtracker/api/BloodThinnerTracker.Api

# Try running manually to see errors
sudo -u bloodtracker /opt/bloodtracker/api/BloodThinnerTracker.Api
```

**Common Causes and Fixes:**

1. **Missing executable permissions:**
   ```bash
   sudo chmod +x /opt/bloodtracker/api/BloodThinnerTracker.Api
   ```

2. **Wrong ownership:**
   ```bash
   sudo chown -R bloodtracker:bloodtracker /opt/bloodtracker
   ```

3. **Missing configuration file:**
   ```bash
   ls -l /opt/bloodtracker/api/appsettings.Production.json
   # If missing, recreate using deploy script or template
   ```

4. **Port already in use:**
   ```bash
   sudo netstat -tulpn | grep 5234
   # If occupied, stop conflicting service or change port
   ```

5. **Database permission issues:**
   ```bash
   sudo chown bloodtracker:bloodtracker /var/lib/bloodtracker
   sudo chmod 755 /var/lib/bloodtracker
   ```

---

### Issue 2: Can't Access API from Network

**Symptoms:**
- `curl http://localhost:5234/health` works on Pi
- `curl http://192.168.1.100:5234/health` fails from another device

**Diagnostic Steps:**

```bash
# Check if service is listening on all interfaces (0.0.0.0)
sudo netstat -tulpn | grep 5234

# Should show: 0.0.0.0:5234 (not 127.0.0.1:5234)

# Check firewall status
sudo ufw status

# Test connectivity from Pi to itself using external IP
PI_IP=$(hostname -I | awk '{print $1}')
curl http://$PI_IP:5234/health
```

**Common Causes and Fixes:**

1. **Firewall blocking connections:**
   ```bash
   sudo ufw allow 5234/tcp
   sudo ufw allow 5235/tcp  # If using Web UI
   sudo ufw reload
   ```

2. **Wrong binding in configuration:**
   Edit `/opt/bloodtracker/api/appsettings.Production.json`:
   ```json
   "Urls": "http://0.0.0.0:5234"  // Not "http://localhost:5234"
   ```
   Then restart:
   ```bash
   sudo systemctl restart bloodtracker-api.service
   ```

3. **Network interface issues:**
   ```bash
   # Check network interfaces
   ip addr show
   
   # Ensure wlan0 or eth0 has IP address assigned
   ```

4. **Router firewall (rare):**
   - Check your router's firewall settings
   - Ensure local network traffic is allowed

---

### Issue 3: Database Errors

**Symptoms:**
- Errors mentioning "SQLite" or "database locked"
- API starts but crashes when accessing endpoints

**Diagnostic Steps:**

```bash
# Check database file exists
ls -lh /var/lib/bloodtracker/bloodtracker.db

# Check database permissions
sudo -u bloodtracker sqlite3 /var/lib/bloodtracker/bloodtracker.db ".schema"

# Check disk space
df -h /var/lib/bloodtracker
```

**Common Causes and Fixes:**

1. **Database file doesn't exist:**
   ```bash
   # Stop service
   sudo systemctl stop bloodtracker-api.service
   
   # Create directory if needed
   sudo mkdir -p /var/lib/bloodtracker
   sudo chown bloodtracker:bloodtracker /var/lib/bloodtracker
   
   # Start service (will create database)
   sudo systemctl start bloodtracker-api.service
   ```

2. **Database locked:**
   ```bash
   # Check for stale lock files
   ls -la /var/lib/bloodtracker/
   
   # Remove lock files if service is stopped
   sudo systemctl stop bloodtracker-api.service
   sudo rm -f /var/lib/bloodtracker/*.db-shm
   sudo rm -f /var/lib/bloodtracker/*.db-wal
   sudo systemctl start bloodtracker-api.service
   ```

3. **Disk full:**
   ```bash
   df -h /var/lib/bloodtracker
   
   # If full, clean up old backups
   sudo find /opt/bloodtracker -name "*.backup.*" -mtime +30 -delete
   ```

4. **Corrupted database:**
   ```bash
   # Backup current database
   sudo cp /var/lib/bloodtracker/bloodtracker.db /var/lib/bloodtracker/bloodtracker.db.corrupted
   
   # Check integrity
   sudo -u bloodtracker sqlite3 /var/lib/bloodtracker/bloodtracker.db "PRAGMA integrity_check;"
   
   # If corrupted, restore from backup or recreate
   ```

---

### Issue 4: High Memory Usage

**Symptoms:**
- Raspberry Pi becomes slow
- Service crashes or restarts frequently
- `free -h` shows high memory usage

**Diagnostic Steps:**

```bash
# Check memory usage
free -h

# Check service memory limit
systemctl show bloodtracker-api.service | grep MemoryLimit

# Monitor real-time memory
top -u bloodtracker
```

**Common Causes and Fixes:**

1. **Lower memory limit in systemd:**
   Edit `/etc/systemd/system/bloodtracker-api.service`:
   ```ini
   [Service]
   MemoryLimit=256M  # Reduced from 512M
   ```
   
   Reload and restart:
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl restart bloodtracker-api.service
   ```

2. **Enable swap if not present:**
   ```bash
   # Check swap
   swapon --show
   
   # Create 1GB swap file if needed
   sudo fallocate -l 1G /swapfile
   sudo chmod 600 /swapfile
   sudo mkswap /swapfile
   sudo swapon /swapfile
   
   # Make permanent
   echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
   ```

3. **Optimize SQLite connection pooling:**
   Edit appsettings.Production.json:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=/var/lib/bloodtracker/bloodtracker.db;Cache=Shared;Mode=ReadWriteCreate;Cache Size=-8000;"
   }
   ```

---

### Issue 5: Deployment Script Fails

**Symptoms:**
- `./tools/deploy-to-pi.sh` exits with errors
- Files not transferred to Raspberry Pi

**Diagnostic Steps:**

```bash
# Test SSH connection
ssh pi@raspberrypi "echo 'Connection successful'"

# Check if rsync is installed locally
rsync --version

# Check available space on Pi
ssh pi@raspberrypi "df -h /opt"

# Verify dotnet SDK version
dotnet --version
```

**Common Causes and Fixes:**

1. **SSH connection issues:**
   ```bash
   # Add SSH key if using password authentication
   ssh-copy-id pi@raspberrypi
   
   # Or specify identity file
   ./tools/deploy-to-pi.sh --host raspberrypi --user pi
   ```

2. **rsync not installed:**
   ```bash
   # On macOS
   brew install rsync
   
   # On Ubuntu/Debian
   sudo apt install rsync
   ```

3. **Wrong .NET SDK version:**
   ```bash
   # Check version
   dotnet --version
   
   # Install .NET 10 SDK if needed
   # Follow: https://dotnet.microsoft.com/download/dotnet/10.0
   ```

4. **Insufficient disk space:**
   ```bash
   # Clean old backups on Pi
   ssh pi@raspberrypi "sudo find /opt/bloodtracker -name '*.backup.*' -delete"
   
   # Check space
   ssh pi@raspberrypi "df -h /opt"
   ```

---

### Issue 6: Tailscale Access Not Working

**Symptoms:**
- Can access via local IP but not Tailscale IP
- Magic DNS not resolving

**Diagnostic Steps:**

```bash
# Check Tailscale status
tailscale status

# Check Tailscale IP
tailscale ip -4

# Test access to Tailscale IP from Pi itself
curl http://$(tailscale ip -4):5234/health
```

**Common Causes and Fixes:**

1. **Tailscale not running:**
   ```bash
   sudo systemctl status tailscaled
   sudo systemctl start tailscaled
   sudo tailscale up
   ```

2. **Firewall blocking Tailscale:**
   ```bash
   # Allow Tailscale interface
   sudo ufw allow in on tailscale0
   ```

3. **Magic DNS not enabled:**
   ```bash
   sudo tailscale up --accept-dns
   ```

4. **Tailscale ACLs blocking access:**
   - Check Tailscale admin console
   - Verify ACL rules allow access to ports 5234/5235

---

### Issue 7: Update Breaks Deployment

**Symptoms:**
- After running deploy script, service won't start
- Lost configuration or database

**Diagnostic Steps:**

```bash
# Check if backup exists
ls -l /opt/bloodtracker/api.backup.*

# Check database is still there
ls -l /var/lib/bloodtracker/bloodtracker.db

# Compare old and new configurations
diff /opt/bloodtracker/api.backup.*/appsettings.Production.json \
     /opt/bloodtracker/api/appsettings.Production.json
```

**Common Causes and Fixes:**

1. **Configuration lost during update:**
   ```bash
   # Restore from backup
   sudo cp /opt/bloodtracker/api.backup.*/appsettings.Production.json \
           /opt/bloodtracker/api/
   
   sudo systemctl restart bloodtracker-api.service
   ```

2. **Rollback to previous version:**
   ```bash
   # Stop service
   sudo systemctl stop bloodtracker-api.service
   
   # Find latest backup
   LATEST_BACKUP=$(ls -td /opt/bloodtracker/api.backup.* | head -1)
   
   # Restore
   sudo rm -rf /opt/bloodtracker/api
   sudo cp -r $LATEST_BACKUP /opt/bloodtracker/api
   
   # Fix permissions
   sudo chown -R bloodtracker:bloodtracker /opt/bloodtracker/api
   sudo chmod +x /opt/bloodtracker/api/BloodThinnerTracker.Api
   
   # Start service
   sudo systemctl start bloodtracker-api.service
   ```

3. **Database permissions changed:**
   ```bash
   sudo chown bloodtracker:bloodtracker /var/lib/bloodtracker/bloodtracker.db
   sudo chmod 644 /var/lib/bloodtracker/bloodtracker.db
   ```

---

## Performance Issues

### Slow API Response Times

**Diagnostic:**
```bash
# Test response time
time curl http://localhost:5234/health

# Check CPU usage
top -u bloodtracker

# Check database size
ls -lh /var/lib/bloodtracker/bloodtracker.db
```

**Optimizations:**

1. **Optimize SQLite:**
   ```bash
   # Vacuum database to reclaim space
   sudo -u bloodtracker sqlite3 /var/lib/bloodtracker/bloodtracker.db "VACUUM;"
   
   # Analyze for query optimization
   sudo -u bloodtracker sqlite3 /var/lib/bloodtracker/bloodtracker.db "ANALYZE;"
   ```

2. **Reduce logging:**
   Edit appsettings.Production.json:
   ```json
   "Logging": {
     "LogLevel": {
       "Default": "Warning",
       "Microsoft": "Error"
     }
   }
   ```

3. **Enable connection pooling:**
   In connection string:
   ```
   "Data Source=/var/lib/bloodtracker/bloodtracker.db;Cache=Shared;Pooling=True;"
   ```

---

## Getting Help

If you're still experiencing issues:

1. **Collect diagnostic information:**
   ```bash
   # Save system info
   uname -a > ~/bloodtracker-issue.txt
   free -h >> ~/bloodtracker-issue.txt
   df -h >> ~/bloodtracker-issue.txt
   
   # Save service logs
   sudo journalctl -u bloodtracker-api.service -n 100 >> ~/bloodtracker-issue.txt
   
   # Save configuration (remove secrets first!)
   cat /opt/bloodtracker/api/appsettings.Production.json | \
     sed 's/"Password": ".*"/"Password": "REDACTED"/g' >> ~/bloodtracker-issue.txt
   ```

2. **Check existing documentation:**
   - [Full Deployment Guide](RASPBERRY-PI-INTERNAL.md)
   - [Quick Start Guide](RASPBERRY-PI-QUICK-START.md)
   - [Configuration Templates](../tools/config/)

3. **Open an issue:**
   - Include diagnostic information
   - Describe what you tried
   - Include error messages

---

**Last Updated**: November 2025
