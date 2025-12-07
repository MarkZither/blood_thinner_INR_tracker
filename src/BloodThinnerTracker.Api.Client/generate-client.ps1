param(
    [switch]$WhatIf
)

# Determine script directory and switch to it
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host "[generate-client] Working directory: $ScriptDir"

# Find the project file
$csproj = Get-ChildItem -Filter *.csproj -File -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $csproj) {
    Write-Error "No .csproj file found in $ScriptDir. Aborting."
    exit 1
}

$scriptName = Split-Path -Leaf $MyInvocation.MyCommand.Path
$keep = @($csproj.Name, 'kiota.log', 'kiota-lock.json', $scriptName)

Write-Host "[generate-client] Keeping: $($keep -join ', ')"

# Remove everything else in the folder
Get-ChildItem -Force | ForEach-Object {
    if ($keep -contains $_.Name) {
        Write-Host "[generate-client] Keeping: $($_.Name)"
    }
    else {
        Write-Host "[generate-client] Removing: $($_.Name)"
        if (-not $WhatIf) {
            try {
                if ($_.PSIsContainer) {
                    Remove-Item $_.FullName -Recurse -Force -ErrorAction Stop
                }
                else {
                    Remove-Item $_.FullName -Force -ErrorAction Stop
                }
            }
            catch {
                Write-Warning "[generate-client] Failed to remove $($_.FullName): $_"
            }
        }
    }
}

if ($WhatIf) {
    Write-Host "[generate-client] Dry-run complete. Re-run without -WhatIf to apply changes and regenerate client."
    exit 0
}

# Ensure Kiota CLI is available
try {
    $kiotaPath = Get-Command kiota -ErrorAction Stop | Select-Object -First 1
}
catch {
    Write-Error "'kiota' CLI not found in PATH. Install Kiota CLI before running this script. See https://aka.ms/kiota for instructions."
    exit 2
}

# Generate client using local API OpenAPI endpoint
$openapi = 'https://localhost:7234/openapi/v1.json'
$arguments = @( 'generate', '--openapi', $openapi, '--language', 'csharp', '--output', '.' )

Write-Host "[generate-client] Running: kiota $($arguments -join ' ')"

$proc = Start-Process -FilePath $kiotaPath.Path -ArgumentList $arguments -NoNewWindow -Wait -PassThru
if ($proc.ExitCode -ne 0) {
    Write-Error "[generate-client] Kiota exited with code $($proc.ExitCode)"
    exit $proc.ExitCode
}

Write-Host "[generate-client] Kiota generation complete."
exit 0
