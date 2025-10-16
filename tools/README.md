# Blood Thinner Tracker - Development Tools

This directory contains development tools, scripts, and utilities for the Blood Thinner Medication & INR Tracker application.

## ⚠️ Medical Application Disclaimer

**IMPORTANT**: These tools handle medical application infrastructure and data. Always follow healthcare data protection regulations (HIPAA, GDPR, etc.) and ensure proper security measures are in place.

## Directory Structure

```
tools/
├── scripts/              # Build, deployment, and database scripts
│   ├── build.sh          # Cross-platform build script (Bash)
│   ├── build.ps1         # Windows build script (PowerShell)
│   ├── deploy.sh         # Deployment automation script
│   └── database.sh       # Database management script
├── generators/           # Code generation utilities
│   └── generate.csx      # C# script for generating medical entities
└── README.md            # This file
```

## Scripts Overview

### Build Scripts

#### `build.sh` / `build.ps1`
Unified build scripts for all platforms with medical compliance checking.

**Features:**
- Clean, restore, build, test, package workflows
- Security analysis with medical compliance validation
- Docker image building
- Comprehensive CI/CD pipeline support
- Medical disclaimer and safety warnings

**Usage:**
```bash
# Linux/macOS
./tools/scripts/build.sh [command]

# Windows
.\tools\scripts\build.ps1 [command]
```

**Commands:**
- `clean` - Clean build artifacts
- `restore` - Restore NuGet packages
- `build` - Build solution (Debug)
- `build-release` - Build solution (Release)
- `test` - Run tests with coverage
- `package` - Package applications
- `docker` - Build Docker images
- `security` - Run security analysis
- `ci` - Full CI pipeline
- `all` - Complete build with Docker

### Deployment Script

#### `deploy.sh`
Automated deployment to dev, staging, and production environments.

**Features:**
- Multi-environment deployment (dev/staging/production)
- Docker image building and registry pushing
- Kubernetes deployment with Helm
- Database migration management
- Health checks and validation
- Rollback capabilities
- Slack/Teams notifications

**Prerequisites:**
- Docker
- kubectl (configured for target clusters)
- Helm 3+
- Azure CLI (optional)

**Usage:**
```bash
./tools/scripts/deploy.sh deploy <environment> <version>
./tools/scripts/deploy.sh rollback <environment> [revision]
./tools/scripts/deploy.sh status <environment>
./tools/scripts/deploy.sh health <environment>
```

**Examples:**
```bash
# Deploy version 1.2.3 to development
./tools/scripts/deploy.sh deploy dev v1.2.3

# Check production status
./tools/scripts/deploy.sh status production

# Rollback staging to previous version
./tools/scripts/deploy.sh rollback staging 1
```

### Database Management Script

#### `database.sh`
Comprehensive database operations for medical data with compliance features.

**Features:**
- Entity Framework Core migration management
- Automated backups (SQLite and PostgreSQL)
- Schema validation
- Migration script generation
- Rollback capabilities with safety checks
- Medical data retention compliance
- Backup rotation and cleanup

**Usage:**
```bash
# Migration commands
./tools/scripts/database.sh create "Add medication dosage tracking"
./tools/scripts/database.sh apply Development
./tools/scripts/database.sh list
./tools/scripts/database.sh rollback InitialCreate Development

# Backup commands
./tools/scripts/database.sh backup-sqlite Production
./tools/scripts/database.sh backup-postgresql Production
./tools/scripts/database.sh cleanup-backups 30

# Utility commands
./tools/scripts/database.sh info Development
./tools/scripts/database.sh validate Production
./tools/scripts/database.sh script InitialCreate AddMedications
```

## Code Generators

### Entity Generator

#### `generate.csx`
C# script for generating medical entities, controllers, and services with built-in compliance features.

**Features:**
- Complete CRUD entity generation
- Medical compliance attributes and validations
- Audit trails and soft deletion
- Service layer with business rule validation
- RESTful API controllers with security
- Medical disclaimer integration

**Prerequisites:**
- .NET Script (`dotnet-script` global tool)

**Installation:**
```bash
dotnet tool install -g dotnet-script
```

**Usage:**
```bash
# Generate complete CRUD stack
dotnet script tools/generators/generate.csx all MedicationDose DosageAmount:decimal,TakenAt:DateTime,Notes:string

# Generate individual components
dotnet script tools/generators/generate.csx entity BloodPressure SystolicPressure:int,DiastolicPressure:int
dotnet script tools/generators/generate.csx controller MedicationDose
dotnet script tools/generators/generate.csx service INRTest
```

**Supported Property Types:**
- `string` - Text fields with length validation
- `int` - Integer with range validation
- `decimal` - Decimal with precision validation
- `DateTime` - Date/time fields
- `bool` - Boolean flags
- `Guid` - Unique identifiers

**Generated Components:**
1. **Entity Class** (`src/BloodThinnerTracker.Shared/Models/`)
   - Audit fields (CreatedAt, UpdatedAt, UserId)
   - Soft deletion support
   - Medical compliance attributes
   - Data annotations for validation

2. **API Controller** (`src/BloodThinnerTracker.Api/Controllers/`)
   - RESTful endpoints (GET, POST, PUT, DELETE)
   - Authentication and authorization
   - User isolation (can only access own data)
   - Medical safety validations
   - Comprehensive error handling

3. **Service Layer** (`src/BloodThinnerTracker.Api/Services/`)
   - Business logic implementation
   - Medical validation rules framework
   - Audit logging
   - Data access abstraction

## Development Workflow

### 1. Entity Development
```bash
# Generate new medical entity
dotnet script tools/generators/generate.csx all MedicationReminder \
  MedicationName:string,ScheduledTime:DateTime,IsCompleted:bool,DosageAmount:decimal

# Review generated code for medical compliance
# Add specific medical validation rules
# Update DbContext to include new entity
```

### 2. Database Updates
```bash
# Create migration for new entity
./tools/scripts/database.sh create "Add medication reminder functionality"

# Apply to development database
./tools/scripts/database.sh apply Development

# Validate schema
./tools/scripts/database.sh validate Development
```

### 3. Build and Test
```bash
# Full build with tests and security analysis
./tools/scripts/build.sh ci

# Run specific test suites
./tools/scripts/build.sh test
```

### 4. Deployment
```bash
# Deploy to development environment
./tools/scripts/deploy.sh deploy dev v1.3.0

# Run health checks
./tools/scripts/deploy.sh health dev

# Deploy to production (requires approval)
./tools/scripts/deploy.sh deploy production v1.3.0
```

## Medical Compliance Features

All tools include medical application specific features:

### Data Protection
- Encryption support for sensitive medical data
- Audit trails for all data modifications
- Soft deletion for compliance with retention policies
- User data isolation and access controls

### Safety Validations
- Medical value range validations
- Date/time constraint checking
- Cross-reference validations
- Business rule enforcement

### Compliance Reporting
- Medical disclaimer integration
- Audit logging and reporting
- Security scanning and validation
- Healthcare data protection compliance

## Security Considerations

### Authentication & Authorization
- JWT token validation
- User-based data isolation
- Role-based access control
- Secure API endpoints

### Data Security
- Connection string encryption
- Sensitive data masking in logs
- Secure backup procedures
- HTTPS enforcement

### Infrastructure Security
- Container security scanning
- Dependency vulnerability checking
- OWASP compliance validation
- Security policy enforcement

## Troubleshooting

### Common Issues

**Build Script Permission Denied**
```bash
chmod +x tools/scripts/*.sh
```

**EF Tools Not Found**
```bash
dotnet tool install --global dotnet-ef --version 10.0.0-*
```

**Docker Permission Issues**
```bash
# Add user to docker group (Linux)
sudo usermod -aG docker $USER
# Logout and login again
```

**Migration Conflicts**
```bash
# Check current migrations
./tools/scripts/database.sh list

# Remove conflicting migration
./tools/scripts/database.sh remove
```

### Medical Data Considerations

**Database Backup Failures**
- Ensure proper permissions for backup directories
- Verify connection strings are properly configured
- Check disk space for backup storage

**Migration Rollback Issues**
- Always backup before migrations
- Test rollbacks in development environment
- Document migration dependencies

**Compliance Validation Failures**
- Review medical business rules
- Check data validation constraints
- Ensure audit trail completeness

## Support and Documentation

For detailed technical documentation, see:
- [API Documentation](../docs/api/)
- [Deployment Guide](../docs/deployment/)
- [User Guide](../docs/user-guide/)

For medical compliance and safety information:
- Review medical disclaimers in generated code
- Consult healthcare data protection regulations
- Validate with medical domain experts

---

**⚠️ Medical Application Notice**: This software is for informational purposes only and should not replace professional medical advice. Always consult with healthcare providers regarding medication schedules and medical decisions.