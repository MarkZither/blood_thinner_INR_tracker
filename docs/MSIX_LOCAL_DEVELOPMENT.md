# MSIX Local Development Guide

## Overview

This guide explains how to build, sign, and install the Windows MSIX package locally for development and testing.

## Prerequisites

- .NET 10 SDK installed
- Windows 10/11 with developer mode enabled
- Windows SDK (for signtool.exe)
- Test certificate installed in your certificate store (thumbprint: `0D969A392D37534744C60A63B7B8A5EF064F35AE`)

## Key Concepts

### Why MSIX?

The Blood Thinner Tracker mobile app uses MSIX packaging for Windows to enable:
- **Windows Toast Notifications**: MSIX packages have proper notification support
- **COM Activation**: Desktop notification manager requires COM server registration
- **Proper App Identity**: MSIX provides an App User Model ID (AUMID) for the notification system

### The Build Pipeline

```
1. Publish MSIX (dotnet publish with WindowsPackageType=MSIX)
   ↓
2. Sign MSIX (signtool.exe with test certificate)
   ↓
3. Install MSIX (Add-AppDevPackage.ps1 or manual Add-AppxPackage)
   ↓
4. Run App and Test Notifications
```

## Quick Start: One-Command Installation

The easiest way is to use the provided PowerShell script:

```powershell
.\tools\scripts\Install-MauiMsix.ps1
```

This script:
- Rebuilds the MSIX from scratch
- Signs it with the test certificate
- Installs it (uninstalling old version if present)
- Launches the app

## Manual Build/Sign/Install

### Step 1: Publish MSIX

```powershell
cd c:\Source\github\blood_thinner_INR_tracker

dotnet publish -c Debug -f net10.0-windows10.0.19041.0 `
  src/BloodThinnerTracker.Mobile/BloodThinnerTracker.Mobile.csproj `
  -p:Platform=x64 `
  -p:WindowsPackageType=MSIX
```

Output location:
```
src\BloodThinnerTracker.Mobile\bin\x64\Debug\net10.0-windows10.0.19041.0\AppPackages\
  BloodThinnerTracker.Mobile_1.0.0.0_Debug_Test\
  BloodThinnerTracker.Mobile_1.0.0.0_x64_Debug.msix
```

### Step 2: Sign MSIX with Test Certificate

```powershell
# Find signtool.exe
$signtool = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" `
  -Recurse -Filter "signtool.exe" | Where-Object { $_.FullName -match "x64" } | Select-Object -Last 1

# Sign the MSIX
$msixPath = "c:\Source\github\blood_thinner_INR_tracker\src\BloodThinnerTracker.Mobile\bin\x64\Debug\net10.0-windows10.0.19041.0\AppPackages\BloodThinnerTracker.Mobile_1.0.0.0_Debug_Test\BloodThinnerTracker.Mobile_1.0.0.0_x64_Debug.msix"

& $signtool.FullName sign /fd SHA256 /sha1 0D969A392D37534744C60A63B7B8A5EF064F35AE "$msixPath"
```

Expected output:
```
Done Adding Additional Store
Successfully signed: <path to msix>
```

### Step 3: Install MSIX

**Option A: Using the built-in installer script (recommended)**

```powershell
cd "src\BloodThinnerTracker.Mobile\bin\x64\Debug\net10.0-windows10.0.19041.0\AppPackages\BloodThinnerTracker.Mobile_1.0.0.0_Debug_Test"

.\Add-AppDevPackage.ps1 -Force
```

**Option B: Using PowerShell directly**

```powershell
# Remove old version if present
Remove-AppxPackage -Package "com.markzither.BloodThinnerTracker.Mobile_1.0.0.0_x64__xmejymz0d062r" -ErrorAction SilentlyContinue

# Install new version
Add-AppxPackage -Path "src\BloodThinnerTracker.Mobile\bin\x64\Debug\net10.0-windows10.0.19041.0\AppPackages\BloodThinnerTracker.Mobile_1.0.0.0_Debug_Test\BloodThinnerTracker.Mobile_1.0.0.0_x64_Debug.msix"
```

### Step 4: Verify Installation

```powershell
# List installed package
Get-AppxPackage | Where-Object { $_.Name -match "BloodThinnerTracker" } | Select-Object Name, Version, InstallLocation

# Launch the app
$app = Get-AppxPackage | Where-Object { $_.Name -match "BloodThinnerTracker" }
$exePath = Get-ChildItem -Path $app.InstallLocation -Filter "BloodThinnerTracker.Mobile.exe" -Recurse | Select-Object -First 1 -ExpandProperty FullName
Start-Process $exePath
```

## Troubleshooting

### Certificate Issues

**Problem**: "The certificate specified is not valid for signing"

**Solution**: Ensure the test certificate is installed:
```powershell
Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Thumbprint -eq "0D969A392D37534744C60A63B7B8A5EF064F35AE" }
```

If not found, create it:
```powershell
# Create self-signed cert
$cert = New-SelfSignedCertificate -CertStoreLocation "Cert:\CurrentUser\My" `
  -Subject "CN=BloodThinnerTracker Test" `
  -FriendlyName "BloodThinnerTracker Test Certificate" `
  -Type CodeSigningCert `
  -KeyUsage DigitalSignature `
  -NotAfter (Get-Date).AddYears(5)

Write-Host "Certificate thumbprint: $($cert.Thumbprint)"
```

### Installation Issues

**Problem**: "The package or bundle is not digitally signed"

**Solution**: 
- Ensure signtool signing completed successfully (no errors)
- Try uninstalling and reinstalling

**Problem**: "The splash screen image [SplashScreen.png] cannot be located"

**Solution**: Update manifest to use correct splash path:
```xml
<uap:SplashScreen Image="splash_windowsSplashScreen.png" />
```

### signtool.exe Not Found

**Solution**: Install Windows SDK or update PATH:
```powershell
# Find the latest version
Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" -Directory | Sort-Object Name -Descending | Select-Object -First 1
```

## Testing Notifications

Once the app is installed and running:

1. Check debug output for registration messages
2. Look for notification events in Event Viewer (Applications and Services Logs > Microsoft > Windows > AppWidgetHost)
3. The app will show notifications for:
   - Medication reminders
   - INR test schedule notifications
   - System alerts

## Environment Variables

You can control the build process with:

```powershell
# Use Release configuration
$env:Configuration = "Release"

# Specify different platform
$env:Platform = "x86"  # or "x64", "ARM64"

# Override AUMID
$env:AUMID = "MarkZither.BloodThinnerTracker.Mobile"
```

## Next Steps

After installation, see:
- `docs/AUTHENTICATION_TESTING_GUIDE.md` - OAuth flow testing
- `docs/DEVELOPMENT_SETUP.md` - Full development environment setup
- `.github/copilot-instructions.md` - Project standards and architecture
