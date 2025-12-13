# MSIX Quick Reference

## TL;DR - One Command Install

```powershell
# Run as Administrator
.\tools\scripts\Install-MauiMsix.ps1
```

## Common Workflows

### Fresh Build & Install
```powershell
.\tools\scripts\Install-MauiMsix.ps1
```

### Re-sign & Reinstall (Code changed, just recompile)
```powershell
.\tools\scripts\Install-MauiMsix.ps1
```

### Skip Build (Only sign & install)
```powershell
.\tools\scripts\Install-MauiMsix.ps1 -SkipBuild
```

### Release Build
```powershell
.\tools\scripts\Install-MauiMsix.ps1 -Configuration Release
```

### Build Only (No install)
```powershell
dotnet publish -c Debug -f net10.0-windows10.0.19041.0 `
  src/BloodThinnerTracker.Mobile/BloodThinnerTracker.Mobile.csproj `
  -p:Platform=x64 -p:WindowsPackageType=MSIX
```

## Verify Installation

```powershell
# List installed app
Get-AppxPackage | Where-Object { $_.Name -match "BloodThinnerTracker" }

# Launch app manually
$app = Get-AppxPackage | Where-Object { $_.Name -match "BloodThinnerTracker" }
$exe = Get-ChildItem -Path $app.InstallLocation -Filter "*.exe" -Recurse | Select-Object -First 1
Start-Process $exe.FullName
```

## Uninstall

```powershell
# Remove installed package
Remove-AppxPackage -Package "com.markzither.BloodThinnerTracker.Mobile_1.0.0.0_x64__xmejymz0d062r"

# Or by name pattern
$app = Get-AppxPackage | Where-Object { $_.Name -match "BloodThinnerTracker" }
Remove-AppxPackage -Package $app.PackageFullName
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "Run as Administrator" error | Right-click PowerShell → Run as Administrator |
| "signtool.exe not found" | Install Windows SDK or check PATH |
| "Certificate not found" | Create test cert: `New-SelfSignedCertificate -CertStoreLocation "Cert:\CurrentUser\My" -Subject "CN=BloodThinnerTracker Test" -Type CodeSigningCert` |
| "Cannot find package to remove" | Check app is installed: `Get-AppxPackage \| Where-Object { $_.Name -match "BloodThinnerTracker" }` |
| "Splash screen not found" | Manifest references `splash_windowsSplashScreen.png` (not `SplashScreen.png`) |

## File Locations

**Output MSIX:**
```
src\BloodThinnerTracker.Mobile\bin\x64\Debug\net10.0-windows10.0.19041.0\AppPackages\
  BloodThinnerTracker.Mobile_1.0.0.0_Debug_Test\
  BloodThinnerTracker.Mobile_1.0.0.0_x64_Debug.msix
```

**Installed Location:**
```
C:\Program Files\WindowsApps\com.markzither.BloodThinnerTracker.Mobile_1.0.0.0_x64__xmejymz0d062r\
```

**App Logs:**
```
%APPDATA%\BloodThinnerTracker\logs\
```

## Key Files

- **Manifest:** `src\BloodThinnerTracker.Mobile\Platforms\Windows\Package.appxmanifest`
- **Notification Registration:** `src\BloodThinnerTracker.Mobile\Platforms\Windows\WindowsNotificationRegistration.cs`
- **COM Activator:** `src\BloodThinnerTracker.Mobile\Platforms\Windows\NotificationActivator.cs`
- **Build Config:** `src\BloodThinnerTracker.Mobile\BloodThinnerTracker.Mobile.csproj`

## Certificate Details

- **Type:** Self-signed for development
- **Subject:** CN=BloodThinnerTracker Test
- **Thumbprint:** `0D969A392D37534744C60A63B7B8A5EF064F35AE`
- **Store:** `Cert:\CurrentUser\My`
- **Use:** Signing MSIX packages for local testing

## Manifest Configuration

**Toast Notification Support:**
```xml
<!-- In Package.appxmanifest -->
<uap:SplashScreen Image="splash_windowsSplashScreen.png" />

<Extension Category="windows.toastNotificationActivation">
  <desktop:ToastNotificationActivation 
    ToastActivatorCLSID="A1B2C3D4-E5F6-4789-ABCD-1234567890AB" />
</Extension>

<com:Extension Category="windows.comServer">
  <com:ComServer>
    <com:ExeServer Executable="$targetnametoken$.exe" Arguments="-ToastActivated">
      <com:Class Id="A1B2C3D4-E5F6-4789-ABCD-1234567890AB" />
    </com:ExeServer>
  </com:ComServer>
</com:Extension>
```

**Capabilities:**
```xml
<Capability Name="internetClient" />
<Capability Name="privateNetworkClientServer" />
<rescap:Capability Name="runFullTrust" />
```
