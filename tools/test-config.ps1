# Quick test for config file creation
$PI_HOST = "192.168.1.10"
$PI_USER = "mark"
$SSH_KEY_PATH = "C:\Users\markb\.ssh\id_rsa"

# Test the config command
$apiConfigCmd = @"
cat > /tmp/test-appsettings.json << 'EOFCONFIG'
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "Database": {
        "Provider": "SQLite"
    }
}
EOFCONFIG
"@

Write-Host "Testing config file creation..."
Write-Host "Command being sent:"
Write-Host $apiConfigCmd
Write-Host ""
Write-Host "Executing via SSH..."

& ssh -i $SSH_KEY_PATH "${PI_USER}@${PI_HOST}" $apiConfigCmd

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] Config file created!" -ForegroundColor Green
    Write-Host "Verifying content..."
    & ssh -i $SSH_KEY_PATH "${PI_USER}@${PI_HOST}" "cat /tmp/test-appsettings.json"
} else {
    Write-Host "`n[FAILED] Error creating config file" -ForegroundColor Red
}
