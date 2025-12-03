<#
Installs an MSIX package locally for debugging.

Usage:
  .\install-msix.ps1 -PackagePath "C:\path\to\MyApp_1.0.0.0_x64_Debug.msix"

This script is intentionally conservative — it does not try to guess package family names.
It will attempt to remove an installed package with the same full name if `-ForceReplace` is supplied.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$PackagePath,
    [switch]$ForceReplace
)

if (-not $PackagePath) {
    $PackagePath = Read-Host "Enter path to MSIX package"
}

if (-not (Test-Path $PackagePath)) {
    Write-Error "Package not found: $PackagePath"
    exit 2
}

try {
    if ($ForceReplace) {
        # Try to read package identity from the MSIX (AppxManifest) - best-effort
        try {
            $zip = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
            $entry = $zip.Entries | Where-Object { $_.FullName -ieq "AppxManifest.xml" } | Select-Object -First 1
            if ($entry) {
                $xml = [xml](New-Object System.IO.StreamReader($entry.Open()).ReadToEnd())
                $identity = $xml.Package.Identity
                if ($identity -ne $null) {
                    $name = $identity.name
                    Write-Host "Found package identity name: $name"
                    $installed = Get-AppxPackage -Name $name -ErrorAction SilentlyContinue
                    if ($installed) {
                        Write-Host "Removing previously installed package: $name"
                        Remove-AppxPackage -Package $installed.PackageFullName -AllUsers -ErrorAction SilentlyContinue
                    }
                }
            }
            $zip.Dispose()
        } catch {
            Write-Warning "Could not parse manifest to determine package identity. Continuing with install."
        }
    }

    Write-Host "Installing package: $PackagePath"
    Add-AppxPackage -Path $PackagePath -ForceApplicationShutdown -ErrorAction Stop
    Write-Host "Install succeeded."

    # Try to discover installed package family name and primary app id
    try {
        if ($identity -ne $null -and $name) {
            $pkg = Get-AppxPackage -Name $name -ErrorAction SilentlyContinue
            if ($pkg) {
                $pf = $pkg.PackageFamilyName
                Write-Host "PackageFamilyName: $pf"

                # Attempt to list app entries in the package to help the user attach
                $manifest = [xml](Get-AppxPackageManifest -Package $pkg.PackageFullName)
                $apps = $manifest.Package.Applications.Application
                foreach ($app in $apps) {
                    $appId = $app.Id
                    Write-Host "Found application Id: $appId"
                    Write-Host "You can launch via shell:AppsFolder\$pf!$appId"
                }

                # Optionally launch the app to prime background registration
                Write-Host "Launching first application entry to prime registration..."
                if ($apps.Count -gt 0) {
                    $firstId = $apps[0].Id
                    Start-Process -FilePath "explorer.exe" -ArgumentList "shell:AppsFolder\$pf!$firstId"
                }
            }
            else {
                Write-Warning "Installed package not found via Get-AppxPackage for identity name '$name'"
            }
        }
    } catch {
        Write-Warning "Could not determine PackageFamilyName or launch app: $_"
    }

    Write-Host "\nTips to debug background tasks:"
    Write-Host " - Launch the app at least once from Start Menu so the package activates (script attempted to launch it)."
    Write-Host " - In Visual Studio: Debug → Attach to Process... then look for BackgroundTaskHost.exe or RuntimeBroker.exe when the task runs."
    Write-Host " - Use verbose logging in the app (Serilog) to confirm registration and run events."
    Write-Host " - If registration still isn't happening, open the packaged app and ensure the code path for background registration runs in the packaged startup path."
}
catch {
    Write-Error "Install failed: $_"
    exit 1
}
