<#
.SYNOPSIS
    Import a PFX certificate to LocalMachine\My and grant private key access to a service account.

.DESCRIPTION
    This script automates certificate installation for Windows service deployment:
    - Imports a PFX file into the LocalMachine\My certificate store
    - Grants Read permission on the certificate's private key to the specified service account
    - Optionally creates an HTTP.sys netsh binding (only needed if using HttpSys server, NOT Kestrel)
    - Supports -WhatIf for dry-run testing

.PARAMETER PfxPath
    Path to the PFX file to import.

.PARAMETER Password
    Password for the PFX file (SecureString recommended).

.PARAMETER ServiceAccount
    Service account to grant private key access (e.g., 'NT SERVICE\BloodTrackerApi').

.PARAMETER AppId
    Optional application GUID for netsh HTTP.sys binding (only relevant if using HttpSys, not Kestrel).

.PARAMETER Port
    Optional port number for netsh HTTP.sys binding (only relevant if using HttpSys, not Kestrel).

.PARAMETER BindToHttpSys
    Switch to enable HTTP.sys binding via netsh. Only use if your application explicitly calls UseHttpSys().
    Default: $false (Kestrel-based apps don't need this).

.PARAMETER WhatIf
    Show what actions would be performed without executing them.

.EXAMPLE
    .\bind-windows-cert.ps1 -PfxPath "C:\certs\bloodtracker.pfx" -Password (ConvertTo-SecureString "pass" -AsPlainText -Force) -ServiceAccount "NT SERVICE\BloodTrackerApi"

.EXAMPLE
    .\bind-windows-cert.ps1 -PfxPath "C:\certs\bloodtracker.pfx" -Password (Read-Host -AsSecureString "Enter PFX password") -ServiceAccount "NT SERVICE\BloodTrackerApi" -WhatIf

.NOTES
    Requires Administrator privileges.
    For Kestrel-based applications (default for ASP.NET Core), do NOT use -BindToHttpSys.
    HTTP.sys binding is only needed if your Program.cs calls UseHttpSys() explicitly.
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [ValidateScript({Test-Path $_ -PathType Leaf})]
    [string]$PfxPath,

    [Parameter(Mandatory)]
    [SecureString]$Password,

    [Parameter(Mandatory)]
    [string]$ServiceAccount,

    [string]$AppId = [System.Guid]::NewGuid().ToString(),

    [int]$Port = 0,

    [switch]$BindToHttpSys = $false
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Ensure running as Administrator
function Test-Administrator {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-Error "This script requires Administrator privileges. Please run as Administrator."
    exit 1
}

Write-Host "=== Windows Certificate Installation & Binding ===" -ForegroundColor Cyan
Write-Host "PFX Path: $PfxPath"
Write-Host "Service Account: $ServiceAccount"
Write-Host "Bind to HTTP.sys: $BindToHttpSys"
Write-Host ""

# Step 1: Import PFX certificate to LocalMachine\My store
Write-Host "[1/3] Importing certificate to LocalMachine\My store..." -ForegroundColor Yellow

try {
    if ($PSCmdlet.ShouldProcess("LocalMachine\My", "Import certificate from $PfxPath")) {
        $cert = Import-PfxCertificate -FilePath $PfxPath `
                                      -CertStoreLocation 'Cert:\LocalMachine\My' `
                                      -Password $Password `
                                      -Exportable

        Write-Host "  [OK] Certificate imported: $($cert.Thumbprint)" -ForegroundColor Green
        Write-Host "    Subject: $($cert.Subject)"
        Write-Host "    Expiry: $($cert.NotAfter)"
    } else {
        Write-Host "  [WhatIf] Would import certificate from $PfxPath" -ForegroundColor Gray
        # For WhatIf, try to read thumbprint without importing
        $tempCert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($PfxPath, $Password)
        $cert = $tempCert
        Write-Host "  [WhatIf] Certificate thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
        $tempCert.Dispose()
    }
} catch {
    Write-Error "Failed to import certificate: $_"
    exit 1
}

# Step 2: Grant private key permissions to service account
Write-Host "`n[2/3] Granting private key access to $ServiceAccount..." -ForegroundColor Yellow

try {
    # Locate the private key file in the MachineKeys directory
    $privateKey = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($cert)

    if ($null -eq $privateKey) {
        Write-Warning "Certificate does not have a private key or it's not RSA. Skipping permission grant."
    } else {
        $privateKeyPath = $null

        # Try to get the CNG key path (modern approach)
        if ($privateKey -is [System.Security.Cryptography.RSACng]) {
            $keyName = $privateKey.Key.UniqueName
            $machineKeysPath = "$env:ProgramData\Microsoft\Crypto\Keys"
            $privateKeyPath = Join-Path $machineKeysPath $keyName

            if (-not (Test-Path $privateKeyPath)) {
                # Fallback to RSA MachineKeys path
                $machineKeysPath = "$env:ProgramData\Microsoft\Crypto\RSA\MachineKeys"
                $privateKeyPath = Join-Path $machineKeysPath $keyName
            }
        }
        # Try legacy CSP path
        elseif ($privateKey -is [System.Security.Cryptography.RSACryptoServiceProvider]) {
            $cspKeyContainerInfo = $privateKey.CspKeyContainerInfo
            $keyName = $cspKeyContainerInfo.UniqueKeyContainerName
            $machineKeysPath = "$env:ProgramData\Microsoft\Crypto\RSA\MachineKeys"
            $privateKeyPath = Join-Path $machineKeysPath $keyName
        }

        if ($null -eq $privateKeyPath -or -not (Test-Path $privateKeyPath)) {
            Write-Warning "Could not locate private key file. Permissions may need to be set manually."
            Write-Warning "Certificate thumbprint: $($cert.Thumbprint)"
        } else {
            Write-Host "  Found private key: $privateKeyPath"

            if ($PSCmdlet.ShouldProcess($privateKeyPath, "Grant Read access to $ServiceAccount")) {
                # Grant Read access using icacls
                $icaclsArgs = "`"$privateKeyPath`" /grant `"${ServiceAccount}:R`""
                $icaclsOutput = cmd /c "icacls $icaclsArgs 2>&1"

                if ($LASTEXITCODE -eq 0) {
                    Write-Host "  [OK] Granted Read access to $ServiceAccount" -ForegroundColor Green
                } else {
                    Write-Warning "icacls returned exit code $LASTEXITCODE"
                    Write-Warning "Output: $icaclsOutput"
                }
            } else {
                Write-Host "  [WhatIf] Would grant Read access to $ServiceAccount on $privateKeyPath" -ForegroundColor Gray
            }
        }
    }

    # Cleanup
    if ($privateKey -is [IDisposable]) {
        $privateKey.Dispose()
    }
} catch {
    Write-Warning "Failed to grant private key permissions: $_"
    Write-Warning "You may need to manually grant permissions using certlm.msc or icacls."
}

# Step 3: Optionally bind certificate to HTTP.sys (only if using UseHttpSys)
if ($BindToHttpSys) {
    Write-Host "`n[3/3] Creating HTTP.sys binding via netsh..." -ForegroundColor Yellow

    if ($Port -eq 0) {
        Write-Error "When using -BindToHttpSys, you must specify -Port parameter."
        exit 1
    }

    $ipPort = "0.0.0.0:$Port"

    if ($PSCmdlet.ShouldProcess($ipPort, "Bind certificate $($cert.Thumbprint) via netsh http add sslcert")) {
        try {
            # Check if binding already exists
            $existingBinding = netsh http show sslcert ipport=$ipPort 2>$null

            if ($LASTEXITCODE -eq 0 -and $existingBinding -match "Certificate Hash") {
                Write-Host "  [INFO] Existing binding found. Removing..." -ForegroundColor Cyan
                netsh http delete sslcert ipport=$ipPort | Out-Null
            }

            # Add new binding
            $netshCmd = "netsh http add sslcert ipport=$ipPort certhash=$($cert.Thumbprint) appid=`{$AppId`}"
            Write-Host "  Executing: $netshCmd"

            $netshOutput = Invoke-Expression $netshCmd 2>&1

            if ($LASTEXITCODE -eq 0) {
                Write-Host "  [OK] HTTP.sys binding created successfully" -ForegroundColor Green
            } else {
                Write-Error "netsh http add sslcert failed with exit code $LASTEXITCODE`nOutput: $netshOutput"
            }
        } catch {
            Write-Error "Failed to create HTTP.sys binding: $_"
            exit 1
        }
    } else {
        Write-Host "  [WhatIf] Would create netsh HTTP.sys binding for $ipPort" -ForegroundColor Gray
    }
} else {
    Write-Host "`n[3/3] Skipping HTTP.sys binding (not requested)" -ForegroundColor Gray
    Write-Host "  [INFO] For Kestrel-based apps, configure certificate in appsettings.json:" -ForegroundColor Cyan
    Write-Host "    `"Kestrel`": {"
    Write-Host "      `"Certificates`": {"
    Write-Host "        `"Default`": {"
    Write-Host "          `"Thumbprint`": `"$($cert.Thumbprint)`""
    Write-Host "        }"
    Write-Host "      }"
    Write-Host "    }"
}

Write-Host "`n=== Certificate installation complete ===" -ForegroundColor Green
Write-Host "Certificate Thumbprint: $($cert.Thumbprint)"
Write-Host "Private key permissions granted to: $ServiceAccount"

if ($BindToHttpSys) {
    Write-Host "HTTP.sys binding created for: 0.0.0.0:$Port"
} else {
    Write-Host "Next step: Update appsettings.json with Kestrel:Certificates:Default:Thumbprint"
}

exit 0
