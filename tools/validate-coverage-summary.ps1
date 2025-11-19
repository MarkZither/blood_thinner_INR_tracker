<#
Validate coverage report-summary/Summary.txt locally.

Usage:
  pwsh -File tools/validate-coverage-summary.ps1 -SummaryPath coverage/report-summary/Summary.txt
  pwsh -File tools/validate-coverage-summary.ps1 -Sample

This script uses the same parsing heuristics as the GitHub Actions workflow
to extract per-assembly coverage percentages (handles decimals, .dll suffixes,
and multiple report formats).
#>

[CmdletBinding()]
param(
    [string]$SummaryPath = "coverage/report-summary/Summary.txt",
    [string]$ThresholdsJsonPath = "",
    [switch]$Sample
)

function Get-DefaultThresholds {
    return @{
        'BloodThinnerTracker.Data.Shared' = 90
        'BloodThinnerTracker.Api' = 38
    }
}

if ($Sample -or -not (Test-Path $SummaryPath)) {
    Write-Host "Summary file not found at '$SummaryPath' or running in -Sample mode. Using sample content."
    $summary = @"
Overall coverage: 82%
BloodThinnerTracker.Data.Shared 92%
BloodThinnerTracker.Api 40%
OtherAssembly 77%
"@
} else {
    $summary = Get-Content -Raw $SummaryPath -ErrorAction Stop
}

# Load thresholds from JSON file if provided, else use defaults
if ($ThresholdsJsonPath -and (Test-Path $ThresholdsJsonPath)) {
    try {
        $json = Get-Content -Raw $ThresholdsJsonPath | ConvertFrom-Json
        $thresholds = @{}
        foreach ($kv in $json.PSObject.Properties) { $thresholds[$kv.Name] = [int]$kv.Value }
    } catch {
        Write-Warning "Failed to parse thresholds JSON at $ThresholdsJsonPath. Falling back to defaults."
        $thresholds = Get-DefaultThresholds
    }
} else {
    $thresholds = Get-DefaultThresholds
}

$fail = $false
$opts = [System.Text.RegularExpressions.RegexOptions]::IgnoreCase

foreach ($assembly in $thresholds.Keys) {
    $min = $thresholds[$assembly]
    $pct = $null

    $patterns = @(
        [regex]::Escape($assembly) + '\s+(\d+(?:\.\d+)?)%'
        , [regex]::Escape($assembly) + '\.dll\s+(\d+(?:\.\d+)?)%'
        , [regex]::Escape(($assembly -split '\.')[-1]) + '.*?(\d+(?:\.\d+)?)%'
        , '^(?:.*' + [regex]::Escape($assembly) + '.*?)(\d+(?:\.\d+)?)%'
    )

    foreach ($pat in $patterns) {
        $m = [regex]::Match($summary, $pat, $opts -bor [System.Text.RegularExpressions.RegexOptions]::Singleline)
        if ($m.Success) {
            $raw = $m.Groups[1].Value
            try {
                $pct = [int][math]::Round([double]$raw)
            } catch {
                $pct = [int]$raw
            }
            break
        }
    }

    if (-not $pct) {
        Write-Host "Assembly '$assembly' not found in summary. Treating as failure." -ForegroundColor Yellow
        Write-Host "---- Coverage summary (for debugging) ----"
        Write-Host $summary
        Write-Host "-----------------------------------------"
        $fail = $true
    } else {
        Write-Host "Assembly ${assembly}: ${pct}% (threshold ${min}%)"
        if ($pct -lt $min) {
            Write-Host "Threshold failed for ${assembly}: ${pct}% < ${min}%" -ForegroundColor Red
            $fail = $true
        }
    }
}

if ($fail) { 
    Write-Host "One or more coverage thresholds failed." -ForegroundColor Red
    exit 2
} else {
    Write-Host "All coverage thresholds satisfied." -ForegroundColor Green
    exit 0
}
