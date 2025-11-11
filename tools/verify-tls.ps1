<#
Verify TLS 1.3 support for a host:port.
Attempts to use OpenSSL if available; falls back to curl --tlsv1.3 if present.
Usage: .\tools\verify-tls.ps1 -Host example.local -Port 443
Exit codes: 0 = TLS1.3 supported and cert present; 1 = failure/unsupported
#>
param(
    [Parameter(Mandatory=$true)] [string]$TargetHost,
    [int]$TargetPort = 443
)

function Use-OpenSsl {
    param(
        [string]$hostName,
        [int]$portNumber
    )
    if (Get-Command openssl -ErrorAction SilentlyContinue) {
        Write-Host "Using openssl to test TLS 1.3"
        $cmd = "openssl s_client -connect $hostName`:$portNumber -tls1_3 -servername $hostName < /dev/null"
        try {
            $out = & sh -c $cmd 2>&1
            if ($LASTEXITCODE -eq 0 -or $out -match 'Protocol  : TLSv1.3') {
                Write-Host "Server negotiated TLS 1.3" -ForegroundColor Green
                return 0
            }
        } catch {
            # fallthrough
        }
        return 1
    }
    return 2
}

function Use-Curl {
    param(
        [string]$hostName,
        [int]$portNumber
    )
    if (Get-Command curl -ErrorAction SilentlyContinue) {
        Write-Host "Using curl to test TLS 1.3 negotiation"
        $url = "https://$hostName`:$portNumber/"
        try {
            # Use full command name to avoid alias issues
            $out = & (Get-Command curl).Source --silent --show-error --insecure --tlsv1.3 --head $url 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "curl connected using TLS 1.3" -ForegroundColor Green
                return 0
            }
        } catch {
            # fallthrough
        }
        return 1
    }
    return 2
}

# Try OpenSSL first (if available)
 $ossl = Use-OpenSsl -hostName $TargetHost -portNumber $TargetPort
if ($ossl -eq 0) { exit 0 }

 # Then try curl
$curlR = Use-Curl -hostName $TargetHost -portNumber $TargetPort
if ($curlR -eq 0) { exit 0 }

Write-Host "Unable to verify TLS 1.3 using available tools. Install OpenSSL or curl with TLS 1.3 support on the operator host." -ForegroundColor Yellow
exit 1
