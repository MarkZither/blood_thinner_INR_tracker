<#
Verify encryption-at-rest on local or remote hosts.
- On Windows: checks BitLocker status via Get-BitLockerVolume (requires admin).
- On Linux (local): checks for LUKS devices via cryptsetup and /etc/crypttab entries.
- For remote Linux hosts: can run via SSH if provided (requires OpenSSH client).

Usage examples:
  # Local Windows check (run as admin)
  .\tools\verify-encryption.ps1

  # Remote Linux check via SSH
  .\tools\verify-encryption.ps1 -SshHost pi.local -SshUser pi

Exit codes:
  0 = encryption detected and verified
  1 = encryption not detected or verification failed
  2 = usage / argument error
#>
param(
    [string]$RemoteHost,
    [string]$RemoteUser = 'pi'
)

function Test-LocalWindows {
    try {
        $volumes = Get-BitLockerVolume -ErrorAction Stop
    } catch {
        Write-Host "BitLocker API not available or requires admin. Try running as Administrator." -ForegroundColor Yellow
        return 1
    }

    $unencrypted = @()
    foreach ($v in $volumes) {
        if ($v.VolumeStatus -ne 'FullyEncrypted' -and $v.VolumeStatus -ne 'FullyDecrypted') {
            # Some platforms report different statuses; consider 'UsedSpaceOnlyEncrypted'
        }
        if ($v.VolumeStatus -eq 'FullyDecrypted' -or $v.VolumeStatus -eq 'Unknown') {
            $unencrypted += $v
        }
    }

    if ($unencrypted.Count -eq 0) {
        Write-Host "All volumes appear encrypted (BitLocker)." -ForegroundColor Green
        return 0
    } else {
        Write-Host "Found volumes not reported as encrypted:" -ForegroundColor Red
        $unencrypted | Format-Table -Property MountPoint,VolumeStatus,KeyProtector
        return 1
    }
}

function Test-LocalLinux {
    # Check for cryptsetup and crypttab
    $luks = $false
    try {
        if (Get-Command cryptsetup -ErrorAction SilentlyContinue) {
            # Look for LUKS devices
            $blk = & lsblk -o NAME,FSTYPE | Out-String
            if ($blk -match 'crypto_LUKS') { $luks = $true }
        }
    } catch {
        # ignore
    }

    try {
        if (-Not $luks -and (Test-Path '/etc/crypttab')) {
            $content = Get-Content -Path '/etc/crypttab' -ErrorAction SilentlyContinue
            if ($null -ne $content -and $content.Length -gt 0) { $luks = $true }
        }
    } catch {
        # ignore
    }

    if ($luks) {
        Write-Host "Detected LUKS/encrypted filesystem entries on this host." -ForegroundColor Green
        return 0
    } else {
        Write-Host "No LUKS or crypttab entries found. Full-disk encryption not detected." -ForegroundColor Yellow
        Write-Host "Check for LUKS with 'sudo cryptsetup isLuks /dev/sdXN' or check cloud disk encryption." -ForegroundColor Yellow
        return 1
    }
}

function Test-RemoteLinuxViaSsh {
    param(
        [string]$remoteHost,
        [string]$remoteUser
    )
    Write-Host "Running remote check via SSH: $remoteUser@$remoteHost"
    try {
        $out = & ssh "$remoteUser@$remoteHost" "lsblk -o NAME,FSTYPE | grep -E 'crypto_LUKS'" 2>$null
        if ($out) {
            Write-Host "Remote host reports LUKS-encrypted block devices." -ForegroundColor Green
            return 0
        } else {
            Write-Host "Remote host did not report LUKS devices. Check /etc/crypttab on that host." -ForegroundColor Yellow
            return 1
        }
    } catch {
        Write-Host "SSH remote check failed: $_" -ForegroundColor Red
        return 1
    }
}

# Main
if ($RemoteHost) {
    exit (Test-RemoteLinuxViaSsh -remoteHost $RemoteHost -remoteUser $RemoteUser)
} else {
    if ($IsWindows) {
        exit (Test-LocalWindows)
    } else {
        exit (Test-LocalLinux)
    }
}
