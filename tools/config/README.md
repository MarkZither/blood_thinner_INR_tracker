# Production Configuration Templates

This directory contains template configuration files for production deployments.

## Files

- **appsettings.Production.json.template** - API production configuration template
- **appsettings.Web.Production.json.template** - Web UI production configuration template

## Usage

### For Raspberry Pi Deployment

The `deploy-to-pi.sh` script automatically creates production configuration files with sensible defaults for internal use.

**Manual configuration** (if needed):

```bash
# On Raspberry Pi
cp /opt/bloodtracker/api/appsettings.Production.json.template /opt/bloodtracker/api/appsettings.Production.json

# Edit the file
nano /opt/bloodtracker/api/appsettings.Production.json

# Secure permissions
chmod 600 /opt/bloodtracker/api/appsettings.Production.json
```

### Configuration Options

#### Database Connection String

SQLite (recommended for Raspberry Pi):
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=/var/lib/bloodtracker/bloodtracker.db;Cache=Shared;Journal Mode=WAL;Synchronous=NORMAL;"
}
```

PostgreSQL (for cloud deployments):
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=bloodtracker;Username=bloodtracker_user;Password=your_secure_password;SSL Mode=Require;"
}
```

#### Network Binding

Internal only (Raspberry Pi):
```json
"Urls": "http://0.0.0.0:5234"
```

With HTTPS (production):
```json
"Urls": "http://0.0.0.0:5234;https://0.0.0.0:7234"
```

#### CORS Configuration

For internal use with Web UI:
```json
"Security": {
  "AllowedOrigins": [
    "http://localhost:5235",
    "http://raspberrypi:5235",
    "http://192.168.1.100:5235"
  ]
}
```

For production (specific domains):
```json
"Security": {
  "AllowedOrigins": [
    "https://bloodtracker.com",
    "https://app.bloodtracker.com"
  ]
}
```

## Security Notes

⚠️ **Never commit production configuration files to git**

These templates are provided as a reference. Actual production files should:
- Be created on the target system
- Contain secure passwords/secrets
- Have restricted file permissions (`chmod 600`)
- Be excluded from version control

## Environment-Specific Settings

Different environments may require different settings:

| Setting | Development | Raspberry Pi | Cloud Production |
|---------|-------------|--------------|------------------|
| Database | SQLite (temp) | SQLite (persistent) | PostgreSQL |
| HTTPS | Optional | No | Required |
| CORS | Permissive | Internal only | Specific domains |
| Logging | Debug | Information | Warning |
| OAuth | Optional | Optional | Required |
