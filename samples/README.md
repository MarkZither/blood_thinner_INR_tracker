# Sample Configurations - Blood Thinner & INR Tracker

This directory contains sample configuration files for different deployment scenarios.

**⚠️ SECURITY WARNING**: These are examples only. Never use default values in production!

## Configuration Files

### Development

- `appsettings.Development.json` - Local development settings
- `docker-compose.dev.yml` - Development Docker setup
- `local.env` - Local environment variables

### Production

- `appsettings.Production.json` - Production configuration template
- `docker-compose.prod.yml` - Production Docker setup
- `kubernetes/` - Kubernetes deployment manifests
- `azure/` - Azure-specific configurations

### Testing

- `appsettings.Testing.json` - Test environment settings
- `test.env` - Testing environment variables

## Quick Start

### 1. Copy Configuration

```bash
# Copy sample to your project
cp samples/appsettings.Development.json src/BloodThinnerTracker.Api/
cp samples/local.env .env

# Update values for your environment
```

### 2. Required Changes

Before running, you MUST update:

- Database connection strings
- JWT secret keys (use strong, random values)
- OAuth2 client IDs and secrets
- Encryption keys (32 characters minimum)
- CORS origins for your domains

### 3. Security Checklist

- [ ] Changed all default passwords
- [ ] Updated JWT secrets
- [ ] Configured SSL certificates
- [ ] Set strong encryption keys
- [ ] Verified OAuth2 configurations
- [ ] Restricted CORS origins
- [ ] Enabled audit logging

## Environment-Specific Notes

### Development
- Uses SQLite database by default
- Relaxed CORS policy for local testing
- Detailed logging enabled
- Development certificates for HTTPS

### Production
- PostgreSQL database required
- Strict CORS policy
- Minimal logging (PII protection)
- Commercial SSL certificates
- Rate limiting enabled
- Health checks configured

### Testing
- In-memory database
- Mock OAuth2 providers
- Comprehensive logging
- Test data seeding enabled

## Support

For configuration questions:
- Documentation: https://docs.bloodtracker.com/configuration
- Examples: https://github.com/bloodtracker/examples
- Support: config-support@bloodtracker.com