<#
Placeholder publish script for Web linux-arm64.
Scaffold for task T019 and T006.
#>
param(
    [string]$ProjectPath = "src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj",
    [string]$Output = "artifacts/linux-arm64/web"
)

Write-Host "[Placeholder] dotnet publish $ProjectPath -f net10.0 -r linux-arm64 -c Release -o $Output"

# Real implementation: ensure static assets are published and optional reverse-proxy notes.
