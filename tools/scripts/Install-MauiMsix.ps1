#Requires -Version 7.0
# Note: we require admin privileges; check explicitly to provide a clearer message.
#Requires -Version 7.0

<#
.SYNOPSIS
    Build, sign, and install the Blood Thinner Tracker MSIX package locally.

.DESCRIPTION
    This script automates the complete MSIX development workflow:
    1. Rebuilds the MSIX package (net10.0-windows10.0.19041.0)
    2. Signs it with the test certificate (thumbprint: 0D969A392D37534744C60A63B7B8A5EF064F35AE)
    3. Uninstalls any existing version
    4. Installs the new signed MSIX
    5. Optionally launches the app

.PARAMETER Configuration
    Build configuration: Debug or Release. Default: Debug

.PARAMETER Platform
    Target platform: x64, x86, or ARM64. Default: x64

.PARAMETER Launch
    Launch the app after installation. Default: $true

.PARAMETER SkipBuild
    Skip the build step and use existing MSIX. Default: $false

.PARAMETER SkipSign
    Skip the signing step. Default: $false

.PARAMETER SkipInstall
    Skip the installation step. Default: $false

.PARAMETER CleanBuild
    Clean before building. Default: $false

.EXAMPLE
    .\Install-MauiMsix.ps1
    # Builds, signs, installs, and launches the app

.EXAMPLE
    .\Install-MauiMsix.ps1 -Configuration Release -Platform x64 -SkipBuild
    # Signs and installs an existing Release MSIX

.EXAMPLE
    .\Install-MauiMsix.ps1 -CleanBuild -SkipLaunch
    # Clean build, sign, install without launching

#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [ValidateSet('x86', 'x64', 'ARM64')]
    [string]$Platform = 'x64',

    [switch]$Launch = $true,

    [switch]$SkipBuild = $false,

    [switch]$SkipSign = $false,

    [switch]$SkipInstall = $false,

    [switch]$CleanBuild = $false
)

$ErrorActionPreference = 'Stop'
$WarningPreference = 'Continue'

# NOTE: Installing/uninstalling the MSIX requires elevation, but building and signing do not in most cases.
# To make local iteration easier, we perform an admin check only when the script is going to install or uninstall packages.

# Configuration
# Find the repository root by walking up folders until we find the solution file.
function Get-RepoRoot {
    param([string]$start)
    $cur = $start
    while ($true) {
        if (Test-Path (Join-Path $cur 'BloodThinnerTracker.sln')) { return $cur }
        $parent = Split-Path -Parent $cur
        if ([string]::IsNullOrEmpty($parent) -or $parent -eq $cur) { break }
        $cur = $parent
    }
    # Fallback: two parents up from script dir (previous heuristic)
    return Split-Path -Parent (Split-Path -Parent $start)
}

$REPO_ROOT = Get-RepoRoot -start $PSScriptRoot
$PROJECT_NAME = "BloodThinnerTracker.Mobile"
$PROJECT_PATH = "$REPO_ROOT\src\$PROJECT_NAME"
$AUMID = "MarkZither.BloodThinnerTracker.Mobile"
$CERT_THUMBPRINT = "0D969A392D37534744C60A63B7B8A5EF064F35AE"

# Calculate paths
$TargetFramework = "net10.0-windows10.0.19041.0"
$BinPath = "$PROJECT_PATH\bin\$Platform\$Configuration\$TargetFramework"
# Note: AppPackages folder contains one or more package folders; do not hardcode the package identity/version.
$PackageDir = Join-Path $BinPath 'AppPackages'
$MsixFile = $null

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Blood Thinner Tracker MSIX Installer" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration" -ForegroundColor Green
Write-Host "Platform:      $Platform" -ForegroundColor Green
Write-Host "AUMID:         $AUMID" -ForegroundColor Green
Write-Host "Repo Root:     $REPO_ROOT" -ForegroundColor Gray
Write-Host ""

# ============================================================================
# STEP 1: Clean (optional)
# ============================================================================

if ($CleanBuild -and -not $SkipBuild) {
    Write-Host "Step 1/5: Cleaning previous build..." -ForegroundColor Yellow
    try {
        dotnet clean -c $Configuration -f $TargetFramework `
            "$PROJECT_PATH\$PROJECT_NAME.csproj" `
            -p:Platform=$Platform 2>&1 | Out-Null
        Write-Host "✓ Clean completed" -ForegroundColor Green
    }
    catch {
        Write-Warning "Clean step failed (non-critical): $_"
    }
}

# ============================================================================
# STEP 2: Build MSIX
# ============================================================================

# Check for 'dotnet' only when a build will be performed. Be permissive: don't abort on unexpected output.
if (-not $SkipBuild) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Host "✗ 'dotnet' not found on PATH. Install the .NET SDK or run with -SkipBuild to use an existing MSIX." -ForegroundColor Red
        exit 1
    }

    # Capture version output for informational purposes and warn if it's older than .NET 10.
    $dotnetVersion = (& dotnet --version 2>&1 | Out-String).Trim()
    if (-not [string]::IsNullOrWhiteSpace($dotnetVersion)) {
        # Use the first token as the version (ignore any extra lines/warnings)
        $firstToken = ($dotnetVersion -split '\s+')[0]
        $major = ($firstToken.Split('.'))[0]
        # Ensure $majInt exists so [ref] can be applied without error
        $majInt = 0
        if ([int]::TryParse($major, [ref]$majInt) -and $majInt -lt 10) {
            Write-Host "⚠️  Warning: Found dotnet SDK $dotnetVersion which is older than .NET 10; publish may fail." -ForegroundColor Yellow
        }
        else {
            Write-Host "dotnet version: $dotnetVersion" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "⚠️  Warning: Unable to parse 'dotnet --version' output; proceeding but build may fail." -ForegroundColor Yellow
    }
}

if (-not $SkipBuild) {
    Write-Host "Step 2/5: Building MSIX package..." -ForegroundColor Yellow
    Write-Host "  Command: dotnet publish -c $Configuration -f $TargetFramework" -ForegroundColor Gray
    Write-Host "  Project: $PROJECT_PATH" -ForegroundColor Gray

    # Map platform to a RID for packaged apps (required for MSIX packaging with app host)
    switch ($Platform) {
        'x64' { $rid = 'win-x64' }
        'x86' { $rid = 'win-x86' }
        'ARM64' { $rid = 'win-arm64' }
        default { $rid = 'win-x64' }
    }

    # Use an empty RuntimeIdentifier to preserve prior behavior and avoid enforcing a RID-specific runtime pack
    $publishOutput = dotnet publish -c $Configuration -f $TargetFramework `
        -o "$BinPath" `
        "$PROJECT_PATH\$PROJECT_NAME.csproj" `
        -p:Platform=$Platform `
        -p:RuntimeIdentifier='' `
        -p:WindowsPackageType=MSIX `
        -p:PublishTrimmed=false `
        -p:PublishReadyToRun=false 2>&1

    # Check for errors
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Build failed" -ForegroundColor Red
        Write-Host $publishOutput | Select-String -Pattern "error|Error" | Select-Object -First 10
        exit 1
    }

    # Discover the most recently created MSIX in the AppPackages output
    $found = Get-ChildItem -Path $PackageDir -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if (-not $found) {
        Write-Host "✗ MSIX file not found under: $PackageDir" -ForegroundColor Red
        exit 1
    }

    $MsixFile = $found.FullName
    Write-Host "✓ MSIX built successfully" -ForegroundColor Green
    Write-Host "  Output: $MsixFile" -ForegroundColor Gray
}
else {
    if (-not (Test-Path $MsixFile)) {
        Write-Host "✗ MSIX file not found: $MsixFile" -ForegroundColor Red
        exit 1
    }
    Write-Host "Step 2/5: Skipped (using existing MSIX)" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# STEP 3: Sign MSIX
# ============================================================================

if (-not $SkipSign) {
    Write-Host "Step 3/5: Signing MSIX package..." -ForegroundColor Yellow

    # Find signtool.exe
    $signtool = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" `
        -Recurse -Filter "signtool.exe" `
        -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match "x64" } | Select-Object -Last 1

    if (-not $signtool) {
        Write-Host "✗ signtool.exe not found" -ForegroundColor Red
        Write-Host "  Searched: C:\Program Files (x86)\Windows Kits\10\bin" -ForegroundColor Gray
        Write-Host "  Install Windows SDK or update PATH" -ForegroundColor Gray
        exit 1
    }

    Write-Host "  Using: $($signtool.FullName)" -ForegroundColor Gray
    Write-Host "  Cert:  $CERT_THUMBPRINT" -ForegroundColor Gray

    # Verify certificate exists
    $cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Thumbprint -eq $CERT_THUMBPRINT }
    if (-not $cert) {
        Write-Host "✗ Certificate not found in Cert:\CurrentUser\My" -ForegroundColor Red
        Write-Host "  Thumbprint: $CERT_THUMBPRINT" -ForegroundColor Gray
        exit 1
    }

    # Sign (robustly retry if the file is temporarily locked by another process)
    $maxSignAttempts = 5
    $signSucceeded = $false
    for ($attempt = 1; $attempt -le $maxSignAttempts; $attempt++) {
        Write-Host "  Signing attempt $attempt/$maxSignAttempts..." -ForegroundColor Gray
        $signOutput = & $signtool.FullName sign /fd SHA256 /sha1 $CERT_THUMBPRINT "$MsixFile" 2>&1

        if ($LASTEXITCODE -eq 0 -and $signOutput -match "Successfully signed") {
            Write-Host "✓ MSIX signed successfully" -ForegroundColor Green
            $signSucceeded = $true
            break
        }

        # Detect common lock message and retry after a short backoff
        if ($signOutput -match "being used by another process" -or $signOutput -match "file is being used by another process") {
            $backoff = [Math]::Min(3000, 500 * $attempt)
            Write-Host "  Warning: MSIX file appears locked by another process; retrying in ${backoff}ms..." -ForegroundColor Yellow
            Start-Sleep -Milliseconds $backoff
            continue
        }

        # Unknown failure - break and report
        Write-Host "✗ Signing failed (attempt $attempt)" -ForegroundColor Red
        Write-Host $signOutput
        break
    }

    if (-not $signSucceeded) {
        Write-Host "✗ Signing failed after $maxSignAttempts attempts." -ForegroundColor Red
        Write-Host "Possible causes: antivirus or indexer locking the MSIX, or another process (Explorer, Defender, store tooling) is holding the file." -ForegroundColor Yellow
        if (Get-Command handle -ErrorAction SilentlyContinue) {
            Write-Host "Attempting to locate locking handles using Sysinternals 'handle' (requires admin)" -ForegroundColor Gray
            try {
                & handle -accepteula "$MsixFile" | Select-Object -First 50
            }
            catch {
                Write-Host "  Failed to run 'handle' or no results were found." -ForegroundColor Gray
            }
        }
        Write-Host "Suggested fixes: close Explorer windows showing the MSIX, disable real-time AV temporarily, or retry the script after a short wait." -ForegroundColor Gray
        exit 1
    }
}
else {
    Write-Host "Step 3/5: Skipped (signing disabled)" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# STEP 4: Install MSIX
# ============================================================================

if (-not $SkipInstall) {
    Write-Host "Step 4/5: Installing MSIX package..." -ForegroundColor Yellow

    # Require Administrator for install/uninstall operations
    try {
        $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }
    catch {
        $isAdmin = $false
    }
    if (-not $isAdmin) {
        Write-Host "This script must be run as Administrator to install or remove packages. Start PowerShell as Administrator and re-run the script." -ForegroundColor Red
        exit 1
    }

    # ------------------------------------------------------------------
    # Cleanup: remove stale LocalServer32 registrations that point to the
    # unpackaged debug EXE (leftover from developer debugging). These
    # can cause DCOM activation to fail with Access Denied when Windows
    # attempts to launch the packaged AppContainer instance.
    # ------------------------------------------------------------------
    $ToastActivatorClsid = 'A1B2C3D4-E5F6-4789-ABCD-1234567890AB'
    function Backup-AndRemove-StaleLocalServer {
        param(
            [string]$RootPath  # e.g. 'HKCR' or 'HKCU:\Software\Classes'
        )

        $regPath = Join-Path $RootPath "CLSID\{$ToastActivatorClsid}\LocalServer32"
        if (-not (Test-Path $regPath)) { return }

        try {
            $val = (Get-Item -Path $regPath).GetValue('')
        }
        catch {
            $val = $null
        }

        if (-not $val) { return }

        # Heuristic: if it points into the repo build output (contains '\bin\' or the repo root), treat it as stale
        $isStale = $false
        if ($val -match '\\bin\\' -or $val -match [regex]::Escape($REPO_ROOT)) { $isStale = $true }

        if ($isStale) {
            $timestamp = (Get-Date).ToString('yyyyMMdd_HHmmss')
            $backupFile = "C:\Dumps\clsid_backup_${RootPath.Replace(':','').Replace('\\','_')}_$timestamp.reg"
            try {
                reg export "$($regPath.Replace('HKCU:','HKCU\').Replace('HKCR:','HKCR\'))" "$backupFile" /y 2>$null
            }
            catch {
                # ignore export failure
            }

            Write-Host "Removing stale LocalServer32 at $regPath (value: $val). Backup: $backupFile" -ForegroundColor Yellow
            try { Remove-Item -Path $regPath -Recurse -Force -ErrorAction SilentlyContinue } catch { }
        }
    }

    # Run cleanup on both per-machine and per-user registration locations
    Backup-AndRemove-StaleLocalServer -RootPath 'HKCR:'
    Backup-AndRemove-StaleLocalServer -RootPath 'HKCU:\Software\Classes'

    # Uninstall existing version
    Write-Host "  Uninstalling previous version..." -ForegroundColor Gray
    $existing = Get-AppxPackage | Where-Object { $_.Name -match $PROJECT_NAME }
    if ($existing) {
        foreach ($pkg in $existing) {
            Write-Host "    Removing: $($pkg.Name)" -ForegroundColor Gray
            Remove-AppxPackage -Package $pkg.PackageFullName -ErrorAction SilentlyContinue | Out-Null
            Start-Sleep -Milliseconds 500
        }
    }

    # Install new version by delegating to the conservative installer script (install-msix.ps1)
    Write-Host "  Installing new version (delegating to install-msix.ps1)..." -ForegroundColor Gray
    $installScript = Join-Path $PSScriptRoot 'install-msix.ps1'
    if (Test-Path $installScript) {
        try {
            # Use ForceReplace so the helper can remove existing installs by identity if needed
            & $installScript -PackagePath $MsixFile -ForceReplace
        }
        catch {
            Write-Host "✗ Delegated install failed: $_" -ForegroundColor Red
            exit 1
        }

        # Verify installation
        $installed = Get-AppxPackage | Where-Object { $_.Name -match $PROJECT_NAME }
        if ($installed) {
            Write-Host "✓ MSIX installed successfully" -ForegroundColor Green
            Write-Host "  Name:    $($installed.Name)" -ForegroundColor Gray
            Write-Host "  Version: $($installed.Version)" -ForegroundColor Gray
            Write-Host "  Path:    $($installed.InstallLocation)" -ForegroundColor Gray

            # Suggest likely app log locations (packaged & unpackaged)
            $pf = $installed.PackageFamilyName
            $packagedLogs = Join-Path $env:LOCALAPPDATA "Packages\$pf\LocalState\Logs"
            Write-Host "  Likely logs (packaged): $packagedLogs" -ForegroundColor Gray
            Write-Host "  Likely logs (app data dir): %LOCALAPPDATA%\Mark Zither\com.markzither.BloodThinnerTracker.Mobile\Data\Logs" -ForegroundColor Gray
        }
        else {
            Write-Host "✗ Installation verification failed after delegated install" -ForegroundColor Red
            exit 1
        }
    }
    else {
        Write-Host "  install-msix.ps1 not found; falling back to in-script install..." -ForegroundColor Yellow
        $installOutput = Add-AppxPackage -Path $MsixFile -ErrorAction SilentlyContinue 2>&1

        # Verify installation
        $installed = Get-AppxPackage | Where-Object { $_.Name -match $PROJECT_NAME }
        if ($installed) {
            Write-Host "✓ MSIX installed successfully" -ForegroundColor Green
            Write-Host "  Name:    $($installed.Name)" -ForegroundColor Gray
            Write-Host "  Version: $($installed.Version)" -ForegroundColor Gray
            Write-Host "  Path:    $($installed.InstallLocation)" -ForegroundColor Gray
        }
        else {
            Write-Host "✗ Installation verification failed" -ForegroundColor Red
            Write-Host $installOutput | Select-String -Pattern "Error|error" | Select-Object -First 5
            exit 1
        }
    }
}
else {
    Write-Host "Step 4/5: Skipped (installation disabled)" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# STEP 5: Launch (optional)
# ============================================================================

if ($Launch) {
    Write-Host "Step 5/5: Launching application..." -ForegroundColor Yellow

    $app = Get-AppxPackage | Where-Object { $_.Name -match $PROJECT_NAME }
    if ($app) {
        $exePath = Get-ChildItem -Path $app.InstallLocation -Filter "*.exe" -Recurse |
            Where-Object { $_.Name -match "BloodThinnerTracker" } |
            Select-Object -First 1 -ExpandProperty FullName

        if ($exePath) {
            Write-Host "  Starting: $exePath" -ForegroundColor Gray
            Start-Process $exePath
            Write-Host "✓ Application launched" -ForegroundColor Green
        }
        else {
            Write-Host "✗ Executable not found in package" -ForegroundColor Red
        }
    }
    else {
        Write-Host "✗ Installed package not found" -ForegroundColor Red
    }
}
else {
    Write-Host "Step 5/5: Skipped (launch disabled)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Installation Complete!" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  • Check debug output in Event Viewer" -ForegroundColor Gray
Write-Host "  • Test notification functionality" -ForegroundColor Gray
Write-Host "  • Check logs (unpackaged): %LOCALAPPDATA%\\Mark Zither\\com.markzither.BloodThinnerTracker.Mobile\\Data\\Logs" -ForegroundColor Gray
Write-Host "  • Check logs (packaged): %LOCALAPPDATA%\\Packages\\<PackageFamilyName>\\LocalState\\Logs" -ForegroundColor Gray
Write-Host ""
