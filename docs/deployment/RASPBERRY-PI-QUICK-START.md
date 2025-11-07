# Raspberry Pi Deployment - Quick Reference

## One-Command Deployment

```bash
# Make script executable (first time only)
chmod +x tools/deploy-to-pi.sh

# Deploy API only
./tools/deploy-to-pi.sh

# Deploy API + Web UI
./tools/deploy-to-pi.sh --web

# Custom host
./tools/deploy-to-pi.sh --host 192.168.1.100 --user pi

# Single-file deployment
./tools/deploy-to-pi.sh --single-file
```

## Prerequisites

✅ .NET 10 SDK on your development machine
✅ Raspberry Pi 4/5 with 64-bit Raspberry Pi OS
✅ SSH access to Raspberry Pi
✅ At least 8GB free space on Pi

## Quick Setup (First Time)

On Raspberry Pi:
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# No other dependencies needed (self-contained deployment)
```

## Common Commands

### On Raspberry Pi

```bash
# Check status
sudo systemctl status bloodtracker-api.service

# View logs
sudo journalctl -u bloodtracker-api.service -f

# Restart
sudo systemctl restart bloodtracker-api.service

# Stop
sudo systemctl stop bloodtracker-api.service

# Start
sudo systemctl start bloodtracker-api.service
```

### Database

```bash
# Backup database
sudo cp /var/lib/bloodtracker/bloodtracker.db ~/bloodtracker-backup-$(date +%Y%m%d).db

# Check database size
du -h /var/lib/bloodtracker/bloodtracker.db

# Database is preserved between updates!
```

## Access URLs

Replace with your Pi's details:

- **Local Network**: `http://192.168.1.100:5234/scalar/v1`
- **Tailscale**: `http://100.x.x.x:5234/scalar/v1`
- **Magic DNS**: `http://raspberrypi.tailnet-name.ts.net:5234/scalar/v1`
- **Hostname**: `http://raspberrypi:5234/scalar/v1`

## Updating

Simply run the deployment script again:

```bash
./tools/deploy-to-pi.sh
```

The script will:
1. Backup current version
2. Stop services
3. Deploy new version
4. Preserve database
5. Restart services

## Troubleshooting

### Service won't start
```bash
# Check detailed logs
sudo journalctl -u bloodtracker-api.service -xe

# Check if port is in use
sudo netstat -tulpn | grep 5234

# Test manually
sudo -u bloodtracker /opt/bloodtracker/api/BloodThinnerTracker.Api
```

### Can't access from network
```bash
# Check firewall
sudo ufw status
sudo ufw allow 5234/tcp  # If needed

# Check service is listening
sudo netstat -tulpn | grep 5234
```

### Database issues
```bash
# Check database exists
ls -lh /var/lib/bloodtracker/bloodtracker.db

# Fix permissions
sudo chown bloodtracker:bloodtracker /var/lib/bloodtracker/bloodtracker.db
```

## Performance Tips

Raspberry Pi 4 (4GB) typical performance:
- Startup: 3-5 seconds
- Memory: 150-250MB
- Response time: <100ms

## Security Notes

⚠️ This deployment is for **internal use only**:
- Designed for private networks (Tailscale/home LAN)
- Do not expose directly to internet
- HTTPS disabled by default (use reverse proxy if needed)

## Full Documentation

See: `docs/deployment/RASPBERRY-PI-INTERNAL.md`

---

**Last Updated**: November 2025
