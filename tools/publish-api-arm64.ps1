<#
Placeholder publish script for API linux-arm64.
This is a scaffold for task T011 and T005.
#>
param(
    [string]$ProjectPath = "src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj",
    [string]$Output = "artifacts/linux-arm64/api"
)

Write-Host "[Placeholder] dotnet publish $ProjectPath -f net10.0 -r linux-arm64 -c Release -o $Output"

# Real implementation should run dotnet publish with appropriate RID and handle versioned output.
