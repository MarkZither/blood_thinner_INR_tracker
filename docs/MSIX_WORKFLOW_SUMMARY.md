# MSIX Installation Workflow Summary

## What We've Created

We've set up a complete local MSIX development workflow for the Blood Thinner Tracker Windows app. This enables proper Windows notification support through MSIX packaging.

## Files Created

1. **`docs/MSIX_LOCAL_DEVELOPMENT.md`** - Complete development guide
   - Overview of why MSIX is needed
   - Detailed manual steps
   - Troubleshooting guide
   - Certificate setup instructions

2. **`tools/scripts/Install-MauiMsix.ps1`** - Automated installation script
   - One-command build, sign, and install
   - Configurable for Debug/Release and different platforms
   - Error handling and progress reporting
   - Colored output for clarity

3. **`docs/MSIX_QUICK_REFERENCE.md`** - Quick reference card
   - Common workflows
   - File locations
   - Troubleshooting table
   - Key configuration details

4. **`tools/scripts/README.md`** - Scripts directory guide
   - Overview of available tools
   - Best practices for scripts

## Key Components

### The App (MSIX Packaged)
- **Framework:** .NET MAUI (net10.0-windows10.0.19041.0)
- **Packaging:** Windows MSIX
- **AUMID:** MarkZither.BloodThinnerTracker.Mobile
- **COM GUID:** A1B2C3D4-E5F6-4789-ABCD-1234567890AB

### Notification System
- **Library:** Microsoft.Toolkit.Uwp.Notifications 7.0.2
- **Registration:** AUMID + COM server in `WindowsNotificationRegistration.cs`
- **Activation:** `NotificationActivator.cs` (COM class)
- **Manifest:** Toast notification extensions in `Package.appxmanifest`

### Signing & Installation
- **Certificate:** Self-signed test cert (thumbprint: `0D969A392D37534744C60A63B7B8A5EF064F35AE`)
- **Tool:** signtool.exe from Windows SDK
- **Installer:** Built-in `Add-AppDevPackage.ps1` or custom PowerShell script

## How to Use

### For Development

**Standard workflow (after code changes):**
```powershell
# Run as Administrator
.\tools\scripts\Install-MauiMsix.ps1
```

This will:
1. ✅ Rebuild the MSIX
2. ✅ Sign with test certificate
3. ✅ Install/update the app
4. ✅ Launch the app

### For Quick Testing

**Skip the build if you only changed manifest/config:**
```powershell
.\tools\scripts\Install-MauiMsix.ps1 -SkipBuild
```

**Use existing MSIX (skip build and sign):**
```powershell
.\tools\scripts\Install-MauiMsix.ps1 -SkipBuild -SkipSign
```

### For Release Builds

```powershell
.\tools\scripts\Install-MauiMsix.ps1 -Configuration Release
```

## What Makes This Work

### 1. MSIX Packaging
- Provides proper app identity (AUMID)
- Enables toast notifications
- Registers COM server for notification activation
- Integrates with Windows Action Center

### 2. Certificate Signing
- Required for MSIX installation
- Test certificate stored in `Cert:\CurrentUser\My`
- Signed with signtool.exe during build

### 3. Notification Registration
- `WindowsNotificationRegistration.cs` registers AUMID and COM server
- `NotificationActivator.cs` handles toast activation events
- Manifest declares toast notification capability
- App can now show Windows toast notifications

## Troubleshooting

**Most Common Issues:**
1. **"Run as Administrator"** → Right-click PowerShell and select "Run as Administrator"
2. **"signtool.exe not found"** → Install Windows SDK or check PATH
3. **"Certificate not found"** → Test certificate missing (run setup script)
4. **Installation fails** → Check Event Viewer (Applications and Services Logs)

See `docs/MSIX_LOCAL_DEVELOPMENT.md` for complete troubleshooting.

## Next Steps

1. **Test Notifications:**
   - Run the installed app
   - Check if notification system initializes correctly
   - Look for debug messages in Output window

2. **Build Real Notifications:**
   - Use `WindowsNotificationRegistration.SendTestNotification()` for testing
   - Integrate with medication reminder logic
   - Test INR schedule notifications

3. **Optimize for Production:**
   - Switch to Release configuration
   - Test with different certificate
   - Plan distribution strategy

## Technical Debt

- Test certificate has no password protection (fine for local dev)
- Production will need proper code signing certificate
- Consider using Microsoft Store for wider distribution
- May want to add certificate generation to CI/CD pipeline

## References

- **MAUI Documentation:** https://learn.microsoft.com/en-us/dotnet/maui/
- **MSIX Packaging:** https://learn.microsoft.com/en-us/windows/msix/
- **Windows Notifications:** https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/
- **Microsoft.Toolkit.Uwp.Notifications:** https://github.com/CommunityToolkit/WindowsCommunityToolkit
