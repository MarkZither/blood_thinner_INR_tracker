# Capture-AppCrash.ps1
# Helper to capture a full process dump for the installed MSIX app when it crashes.
# Usage: Run as Administrator. It will prefer ProcDump (if on PATH), otherwise it will configure LocalDumps.

param(
    [string]$PackageName = 'com.markzither.BloodThinnerTracker.Mobile',
    [string]$ExeName = 'BloodThinnerTracker.Mobile.exe',
    [string]$Argument = '-ToastActivated',
    [string]$DumpFolder = 'C:\Dumps',
    [int]$WaitSeconds = 20
)

Write-Host "Capture helper (requires Administrator for some operations)" -ForegroundColor Cyan
Write-Host "Target package: $PackageName" -ForegroundColor Gray
Write-Host "Target exe:     $ExeName" -ForegroundColor Gray
Write-Host "Dumps folder:   $DumpFolder" -ForegroundColor Gray

if (-not (Test-Path $DumpFolder)) { New-Item -Path $DumpFolder -ItemType Directory | Out-Null }

# Resolve installed app
$app = Get-AppxPackage -Name $PackageName -ErrorAction SilentlyContinue
if (-not $app) {
    Write-Host "✗ Installed package not found: $PackageName" -ForegroundColor Red
    exit 1
}

$exePath = Join-Path $app.InstallLocation $ExeName
if (-not (Test-Path $exePath)) {
    Write-Host "✗ Executable not found in package: $exePath" -ForegroundColor Red
    exit 1
}

function Show-Dumps { Get-ChildItem -Path $DumpFolder -Filter "*.dmp" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object FullName,Length,LastWriteTime }

if (Get-Command procdump -ErrorAction SilentlyContinue) {
    Write-Host "Using ProcDump to wait for process and capture on exception..." -ForegroundColor Green
    Write-Host "ProcDump will wait for process '$ExeName' and capture an exception dump in $DumpFolder" -ForegroundColor Gray
    Start-Process -FilePath procdump -ArgumentList ('-accepteula','-e','-ma','-w', $ExeName, $DumpFolder) -NoNewWindow

    Write-Host "Triggering app (will not sign/install) — starting: $exePath $Argument" -ForegroundColor Gray
    Start-Process -FilePath $exePath -ArgumentList $Argument

    Write-Host "Waiting $WaitSeconds seconds for crash/dump..." -ForegroundColor Gray
    Start-Sleep -Seconds $WaitSeconds

    Write-Host "Dumps found:" -ForegroundColor Cyan
    Show-Dumps
}
else {
    Write-Host "ProcDump not found — configuring LocalDumps (WER) and triggering app to capture dump" -ForegroundColor Yellow

    New-Item -Path $DumpFolder -ItemType Directory -Force | Out-Null
    $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\$ExeName"
    New-Item -Path $regPath -Force | Out-Null
    New-ItemProperty -Path $regPath -Name DumpFolder -Value $DumpFolder -Type String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name DumpType -Value 2 -Type DWord -Force | Out-Null
    New-ItemProperty -Path $regPath -Name DumpCount -Value 5 -Type DWord -Force | Out-Null

    Write-Host "Configured LocalDumps for $ExeName -> $DumpFolder" -ForegroundColor Gray
    Write-Host "Triggering app: $exePath $Argument" -ForegroundColor Gray
    Start-Process -FilePath $exePath -ArgumentList $Argument

    Write-Host "Waiting $WaitSeconds seconds for crash/dump..." -ForegroundColor Gray
    Start-Sleep -Seconds $WaitSeconds

    Write-Host "Dumps found:" -ForegroundColor Cyan
    Show-Dumps

    Write-Host "Note: LocalDumps configuration persists until removed. Remove the key if you do not want persistent dumps:" -ForegroundColor Yellow
    Write-Host "  Remove-Item -Path '$regPath' -Recurse -Force" -ForegroundColor Gray
}

Write-Host "If a dump file was captured, analyze with Visual Studio or 'dotnet-dump analyze'" -ForegroundColor Cyan
