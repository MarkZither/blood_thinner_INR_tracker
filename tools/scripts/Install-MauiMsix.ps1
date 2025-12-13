#Requires -Version 7.0
#Requires -RunAsAdministrator

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

# Configuration
$REPO_ROOT = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$PROJECT_NAME = "BloodThinnerTracker.Mobile"
$PROJECT_PATH = "$REPO_ROOT\src\$PROJECT_NAME"
$AUMID = "MarkZither.BloodThinnerTracker.Mobile"
$CERT_THUMBPRINT = "0D969A392D37534744C60A63B7B8A5EF064F35AE"

# Calculate paths
$TargetFramework = "net10.0-windows10.0.19041.0"
$BinPath = "$PROJECT_PATH\bin\$Platform\$Configuration\$TargetFramework"
$PackageDir = "$BinPath\AppPackages\${PROJECT_NAME}_1.0.0.0_${Configuration}_Test"
$MsixFile = "$PackageDir\${PROJECT_NAME}_1.0.0.0_${Platform}_${Configuration}.msix"

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

if (-not $SkipBuild) {
    Write-Host "Step 2/5: Building MSIX package..." -ForegroundColor Yellow
    Write-Host "  Command: dotnet publish -c $Configuration -f $TargetFramework" -ForegroundColor Gray
    Write-Host "  Project: $PROJECT_PATH" -ForegroundColor Gray
    
    $publishOutput = dotnet publish -c $Configuration -f $TargetFramework `
        -o "$BinPath" `
        "$PROJECT_PATH\$PROJECT_NAME.csproj" `
        -p:Platform=$Platform `
        -p:WindowsPackageType=MSIX `
        -p:PublishTrimmed=false `
        -p:PublishReadyToRun=false 2>&1

    # Check for errors
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Build failed" -ForegroundColor Red
        Write-Host $publishOutput | Select-String -Pattern "error|Error" | Select-Object -First 10
        exit 1
    }

    if (-not (Test-Path $MsixFile)) {
        Write-Host "✗ MSIX file not found at: $MsixFile" -ForegroundColor Red
        exit 1
    }

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

    # Sign
    $signOutput = & $signtool.FullName sign /fd SHA256 /sha1 $CERT_THUMBPRINT "$MsixFile" 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Signing failed" -ForegroundColor Red
        Write-Host $signOutput
        exit 1
    }

    if ($signOutput -match "Successfully signed") {
        Write-Host "✓ MSIX signed successfully" -ForegroundColor Green
    }
    else {
        Write-Host "✗ Signing may have failed (no success message)" -ForegroundColor Red
        Write-Host $signOutput
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

    # Install new version
    Write-Host "  Installing new version..." -ForegroundColor Gray
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
Write-Host "  • Check logs at: %APPDATA%\BloodThinnerTracker\logs\" -ForegroundColor Gray
Write-Host ""
