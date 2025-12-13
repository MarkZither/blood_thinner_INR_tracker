# Tools and Scripts

This directory contains utility scripts and tools for the Blood Thinner Tracker project.

## MSIX Development Scripts

### Install-MauiMsix.ps1

One-command script to build, sign, and install the Windows MSIX package locally.

**Requirements:**
- PowerShell 7.0+ (run as Administrator)
- Windows 10/11
- Windows SDK (for signtool.exe)
- .NET 10 SDK

**Usage:**

```powershell
# Run as Administrator
.\scripts\Install-MauiMsix.ps1
```

**Options:**

```powershell
# Release build with x64 platform
.\scripts\Install-MauiMsix.ps1 -Configuration Release -Platform x64

# Clean rebuild
.\scripts\Install-MauiMsix.ps1 -CleanBuild

# Build and sign only (skip installation)
.\scripts\Install-MauiMsix.ps1 -SkipInstall

# Use existing MSIX (skip build and sign)
.\scripts\Install-MauiMsix.ps1 -SkipBuild -SkipSign

# Install without launching app
.\scripts\Install-MauiMsix.ps1 -Launch:$false
```

See `docs/MSIX_LOCAL_DEVELOPMENT.md` for complete documentation.

## Other Scripts

Add additional scripts to this directory as needed. Keep utilities organized by their purpose:

- **MSIX/packaging**: `Install-*.ps1`, `Build-*.ps1`
- **Database**: `migrate-*.ps1`, `seed-*.ps1`
- **Testing**: `run-tests.ps1`, `coverage-report.ps1`
- **Deployment**: `deploy-*.ps1`

## Best Practices

1. **Include help documentation**: Use `#Requires`, `.SYNOPSIS`, `.DESCRIPTION`, `.EXAMPLE`
2. **Error handling**: Use `$ErrorActionPreference = 'Stop'` and proper try/catch
3. **Color output**: Use `Write-Host -ForegroundColor` for clarity
4. **Logging**: Log important steps for troubleshooting
5. **Exit codes**: Return proper exit codes for CI/CD integration
6. **Documentation**: Always include a README or inline comments
