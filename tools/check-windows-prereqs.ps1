<#
Windows prerequisites checker.

Per feature T033 this script performs lightweight checks on a Windows operator/target host:
- .NET SDK/runtime version (looks for major version 10)
- Available free disk on the install drive (default C:) in MB
- Port availability for API (5234) and Web (5235)
- Presence of basic firewall rules allowing the ports (display names used by deploy script)

Usage:
  # Human readable summary
  .\tools\check-windows-prereqs.ps1

  # JSON output for automation
  .\tools\check-windows-prereqs.ps1 -Json

Exit codes:
 0 = All checks passed
 1 = One or more checks failed (see JSON output or console)

#>

param(
	[switch]$Json,
	[int]$MinFreeMb = 1024,
	[int[]]$PortsToCheck = @(5234, 5235)
)

function Get-DotnetMajorVersion {
	try {
		$ver = (& dotnet --version) -as [string]
		if ([string]::IsNullOrWhiteSpace($ver)) { return $null }
		# version format: major.minor.build
		$parts = $ver.Split('.')
		return [int]$parts[0]
	} catch {
		return $null
	}
}

function Get-FreeSpaceMb([string]$driveLetter) {
	try {
		$drive = Get-PSDrive -Name $driveLetter.TrimEnd(':') -ErrorAction Stop
		return [math]::Floor($drive.Free / 1MB)
	} catch {
		return $null
	}
}

function Test-PortOpen([int]$port) {
	# Test listening on 0.0.0.0/127.0.0.1 using Get-NetTCPConnection (PowerShell 5.1+) or netstat fallback
	try {
		if (Get-Command Get-NetTCPConnection -ErrorAction SilentlyContinue) {
			$conn = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
			if ($conn) { return $true } else { return $false }
		} else {
			$out = netstat -ano | Select-String -Pattern ":$port\s"
			return ($out -ne $null)
		}
	} catch {
		return $false
	}
}

function Test-FirewallRulePresent([int]$port, [string]$displayName) {
	try {
		if (Get-Command Get-NetFirewallRule -ErrorAction SilentlyContinue) {
			$rule = Get-NetFirewallRule -DisplayName $displayName -ErrorAction SilentlyContinue
			if ($rule) { return $true }
			# fallback: search by port in rules
			$matches = Get-NetFirewallRule -ErrorAction SilentlyContinue | ForEach-Object {
				$r = $_
				$props = Get-NetFirewallPortFilter -AssociatedNetFirewallRule $r -ErrorAction SilentlyContinue
				foreach ($p in $props) { if ($p.LocalPort -eq $port) { return $true } }
			}
			return $false
		} else {
			# Older Windows: cannot reliably query firewall rules; report Unknown
			return $null
		}
	} catch {
		return $null
	}
}

# Main
$results = [ordered]@{}

$results.DotnetMajor = Get-DotnetMajorVersion
$results.DotnetOk = ($results.DotnetMajor -ge 10)

$results.FreeMb = Get-FreeSpaceMb -driveLetter 'C:'
$results.DiskOk = ($results.FreeMb -ne $null -and $results.FreeMb -ge $MinFreeMb)

$results.PortChecks = @{}
foreach ($p in $PortsToCheck) {
	$open = Test-PortOpen -port $p
	if ($p -eq 5234) { $displayName = 'Blood Thinner Tracker API' } else { $displayName = 'Blood Thinner Tracker Web' }
	$fw = Test-FirewallRulePresent -port $p -displayName $displayName
	$results.PortChecks[$p] = @{ Open = $open; FirewallRule = $fw }
}

$results.OverallOk = $results.DotnetOk -and $results.DiskOk -and ($results.PortChecks.Values | ForEach-Object { $_.Open } | Where-Object { $_ -eq $false } | Measure-Object).Count -eq 0

if ($Json) {
	$results | ConvertTo-Json -Depth 5
} else {
	Write-Host "Windows prerequisites summary:
  .NET major: $($results.DotnetMajor) (OK: $($results.DotnetOk))
  Free space (C:): $($results.FreeMb) MB (OK: $($results.DiskOk))"
	Write-Host "Port checks:"
	foreach ($kv in $results.PortChecks.GetEnumerator()) {
		$p = $kv.Key
		$info = $kv.Value
		Write-Host "  Port $p - Listening: $($info.Open)  Firewall rule present: $($info.FirewallRule)"
	}
	if ($results.OverallOk) { Write-Host "Result: PASS" -ForegroundColor Green } else { Write-Host "Result: FAIL" -ForegroundColor Red }
}

if ($results.OverallOk) { exit 0 } else { exit 1 }
