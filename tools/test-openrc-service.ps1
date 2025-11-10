# Quick test to verify OpenRC service file creation
param(
    [string]$PiHost = "192.168.1.10",
    [string]$User = "mark",
    [string]$SshKey = "C:\Users\markb\.ssh\id_rsa"
)

Write-Host "Testing OpenRC service file creation..." -ForegroundColor Yellow
Write-Host ""

# Check if service files exist
Write-Host "1. Checking if service files exist:"
& ssh -i $SshKey "${User}@${PiHost}" "ls -la /etc/init.d/bloodtracker-* 2>&1"

Write-Host ""
Write-Host "2. Checking service file contents:"
& ssh -i $SshKey "${User}@${PiHost}" "cat /etc/init.d/bloodtracker-api 2>&1"

Write-Host ""
Write-Host "3. Testing rc-service directly (without doas):"
& ssh -i $SshKey "${User}@${PiHost}" "/sbin/rc-service bloodtracker-api status 2>&1"

Write-Host ""
Write-Host "4. Testing rc-service with doas:"
& ssh -i $SshKey "${User}@${PiHost}" "doas /sbin/rc-service bloodtracker-api status 2>&1"

Write-Host ""
Write-Host "5. Checking if rc-service exists:"
& ssh -i $SshKey "${User}@${PiHost}" "which rc-service; ls -la /sbin/rc-service"
