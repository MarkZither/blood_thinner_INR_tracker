# Deployment Guide - Blood Thinner & INR Tracker

## Overview

This guide covers deployment options for the Blood Thinner & INR Tracker across different environments and platforms.

**⚠️ SECURITY NOTE**: This application handles sensitive medical data. Ensure all deployments follow healthcare data protection regulations (HIPAA, GDPR, etc.).

## Prerequisites

- **.NET 10 SDK** (10.0.100-preview.7.25380.108 or later)
- **Database**: PostgreSQL 13+ (production) or SQLite (development)
- **SSL Certificate** (required for production)
- **OAuth2 Setup**: Azure AD and/or Google OAuth applications

## Environment Configuration

### Development

```bash
# Clone repository
git clone https://github.com/bloodtracker/api.git
cd blood_thinner_INR_tracker

# Restore packages
dotnet restore

# Set up local database
dotnet ef database update --project src/BloodThinnerTracker.Api

# Run with hot reload
dotnet watch run --project src/BloodThinnerTracker.Api
```

### Production

#### Docker Deployment

```dockerfile
# Use official .NET 10 runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY publish/ .
EXPOSE 80 443
ENTRYPOINT ["dotnet", "BloodThinnerTracker.Api.dll"]
```

```bash
# Build and run
docker build -t bloodtracker-api .
docker run -p 5000:80 -p 5001:443 bloodtracker-api
```

#### Azure App Service

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
      - main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '10.0.x'
    includePreviewVersions: true

- script: dotnet publish --configuration Release --output $(Build.ArtifactStagingDirectory)
  displayName: 'Build and Publish'

- task: AzureWebApp@1
  inputs:
    azureSubscription: 'YourSubscription'
    appName: 'bloodtracker-api'
    package: '$(Build.ArtifactStagingDirectory)'
```

#### Kubernetes

```yaml
# k8s-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: bloodtracker-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: bloodtracker-api
  template:
    metadata:
      labels:
        app: bloodtracker-api
    spec:
      containers:
      - name: api
        image: bloodtracker/api:latest
        ports:
        - containerPort: 80
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
        - name: Authentication__JwtSecret
          valueFrom:
            secretKeyRef:
              name: jwt-secret
              key: secret-key
---
apiVersion: v1
kind: Service
metadata:
  name: bloodtracker-api-service
spec:
  selector:
    app: bloodtracker-api
  ports:
  - port: 80
    targetPort: 80
  type: LoadBalancer
```

## Database Setup

### PostgreSQL (Production)

```sql
-- Create database
CREATE DATABASE bloodtracker_prod;
CREATE USER bloodtracker_user WITH PASSWORD 'secure_password';
GRANT ALL PRIVILEGES ON DATABASE bloodtracker_prod TO bloodtracker_user;

-- Enable required extensions
\c bloodtracker_prod;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
```

### SQLite (Development)

```bash
# Database will be created automatically on first run
# Location: ./bloodtracker.db
```

## Environment Variables

### Required

```bash
# Database
ConnectionStrings__DefaultConnection="Host=localhost;Database=bloodtracker;Username=user;Password=pass"

# Authentication
Authentication__JwtSecret="your-super-secure-secret-key-change-in-production"
Authentication__AzureAd__ClientId="your-azure-client-id"
Authentication__AzureAd__ClientSecret="your-azure-client-secret"
Authentication__Google__ClientId="your-google-client-id"
Authentication__Google__ClientSecret="your-google-client-secret"

# Encryption
Encryption__AesKey="your-32-character-encryption-key"

# CORS
AllowedOrigins="https://app.bloodtracker.com,https://bloodtracker.com"
```

### Optional

```bash
# Logging
Logging__LogLevel__Default="Information"
Logging__LogLevel__Microsoft="Warning"

# Health Checks
HealthChecks__Enabled="true"
HealthChecks__Endpoint="/health"

# Rate Limiting
RateLimit__RequestsPerMinute="100"
RateLimit__MedicationLogging="10"
```

## SSL Configuration

### Development

```bash
# Generate development certificate
dotnet dev-certs https --trust
```

### Production

```bash
# Use Let's Encrypt or commercial certificate
# Place certificates in /etc/ssl/certs/
```

## Monitoring and Logging

### Application Insights (Azure)

```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-instrumentation-key"
  }
}
```

### Serilog Configuration

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { 
        "Name": "File", 
        "Args": { 
          "path": "logs/bloodtracker-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

## Security Checklist

- [ ] SSL/TLS enabled (HTTPS only)
- [ ] Strong JWT secret keys (32+ characters)
- [ ] Database connection encryption enabled
- [ ] CORS properly configured
- [ ] Rate limiting implemented
- [ ] Sensitive data encrypted at rest (AES-256)
- [ ] Regular security updates applied
- [ ] OAuth2 redirect URLs validated
- [ ] HIPAA compliance measures in place
- [ ] Data backup and recovery tested

## Performance Optimization

### Caching

```csharp
// Redis cache for session data
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

### Database Indexes

```sql
-- Optimize common queries
CREATE INDEX idx_medication_logs_user_time ON medication_logs(user_id, logged_at);
CREATE INDEX idx_inr_tests_user_date ON inr_tests(user_id, test_date);
CREATE INDEX idx_reminders_user_active ON reminders(user_id, is_active);
```

## Backup and Recovery

### Database Backup

```bash
# PostgreSQL backup
pg_dump -h localhost -U bloodtracker_user bloodtracker_prod > backup_$(date +%Y%m%d).sql

# Automated daily backup
0 2 * * * /usr/local/bin/backup-bloodtracker.sh
```

### Disaster Recovery

- **RTO (Recovery Time Objective)**: 4 hours
- **RPO (Recovery Point Objective)**: 1 hour
- **Backup frequency**: Daily full, hourly incremental
- **Backup retention**: 30 days online, 1 year offline

## Troubleshooting

### Common Issues

1. **Database Connection Errors**
   - Check connection string format
   - Verify database server accessibility
   - Confirm user permissions

2. **Authentication Failures**
   - Validate OAuth2 configuration
   - Check JWT secret key
   - Verify redirect URLs

3. **Performance Issues**
   - Monitor database query performance
   - Check memory usage and garbage collection
   - Review caching effectiveness

### Health Check Endpoints

- `GET /health` - Overall application health
- `GET /health/ready` - Readiness probe (K8s)
- `GET /health/live` - Liveness probe (K8s)

## Support

For deployment issues:
- Documentation: https://docs.bloodtracker.com/deployment
- Issues: https://github.com/bloodtracker/api/issues
- DevOps Support: devops@bloodtracker.com

---

**Remember**: Medical applications require extra attention to security, compliance, and reliability. Always test deployments thoroughly before going live.