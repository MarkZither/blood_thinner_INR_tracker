BeforeAll {
    $script:scriptPath = "$PSScriptRoot\..\..\tools\bind-windows-cert.ps1"
}

Describe "bind-windows-cert.ps1 Tests" {
    Context "Script Existence and Syntax" {
        It "Should exist" {
            Test-Path $script:scriptPath | Should -BeTrue
        }

        It "Should have valid PowerShell syntax" {
            $errors = $null
            $null = [System.Management.Automation.PSParser]::Tokenize(
                (Get-Content $script:scriptPath -Raw),
                [ref]$errors
            )
            $errors.Count | Should -Be 0
        }

        It "Should define required parameters" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'param\s*\('
            # Check for mandatory parameters (they span multiple lines, so use -match with singleline mode)
            $scriptContent -match '(?s)\[Parameter\(Mandatory\)\].*?\$PfxPath' | Should -BeTrue
            $scriptContent -match '(?s)\[Parameter\(Mandatory\)\].*?\$Password' | Should -BeTrue
            $scriptContent -match '(?s)\[Parameter\(Mandatory\)\].*?\$ServiceAccount' | Should -BeTrue
        }
    }

    Context "Parameter Validation" {
        It "Should support WhatIf" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match '\[CmdletBinding\(SupportsShouldProcess\)\]'
        }

        It "Should validate PfxPath as file" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match '\[ValidateScript\(\{Test-Path.*-PathType Leaf\}\)\]'
        }

        It "Should require SecureString for Password" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match '\[SecureString\]\$Password'
        }

        It "Should have BindToHttpSys switch with default false" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match '\[switch\]\$BindToHttpSys\s*=\s*\$false'
        }
    }

    Context "Administrator Check" {
        It "Should check for Administrator privileges" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'function Test-Administrator'
            $scriptContent -match '\[Security\.Principal\.WindowsBuiltInRole\]::Administrator' | Should -BeTrue
        }
    }

    Context "Certificate Import Logic" {
        It "Should use Import-PfxCertificate cmdlet" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'Import-PfxCertificate'
            $scriptContent | Should -Match 'Cert:\\LocalMachine\\My'
            $scriptContent | Should -Match '-Exportable'
        }

        It "Should respect ShouldProcess for WhatIf support" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match '\$PSCmdlet\.ShouldProcess'
        }
    }

    Context "Private Key Permissions Logic" {
        It "Should attempt to retrieve RSA private key" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'GetRSAPrivateKey'
        }

        It "Should handle both RSACng and RSACryptoServiceProvider" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'RSACng'
            $scriptContent | Should -Match 'RSACryptoServiceProvider'
        }

        It "Should use icacls to grant permissions" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'icacls'
            $scriptContent | Should -Match '/grant'
        }

        It "Should grant Read (R) permission" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match ':R'
        }
    }

    Context "HTTP.sys Binding Logic (Optional)" {
        It "Should only bind to HTTP.sys if -BindToHttpSys is specified" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'if \(\$BindToHttpSys\)'
        }

        It "Should require Port parameter when using HTTP.sys binding" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'if \(\$Port -eq 0\)'
            $scriptContent | Should -Match 'you must specify -Port parameter'
        }

        It "Should use netsh http add sslcert" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'netsh http add sslcert'
            $scriptContent | Should -Match 'ipport='
            $scriptContent | Should -Match 'certhash='
            $scriptContent | Should -Match 'appid='
        }

        It "Should check for existing binding before adding" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'netsh http show sslcert'
            $scriptContent | Should -Match 'netsh http delete sslcert'
        }
    }

    Context "Kestrel Configuration Guidance" {
        It "Should provide Kestrel configuration example when not using HTTP.sys" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'Kestrel.*Certificates.*Default.*Thumbprint'
        }

        It "Should output the certificate thumbprint for easy copy-paste" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'Certificate Thumbprint:'
        }
    }

    Context "Error Handling" {
        It "Should exit with code 1 if not Administrator" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'exit 1'
        }

        It "Should set ErrorActionPreference to Stop" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match "\`$ErrorActionPreference\s*=\s*'Stop'"
        }

        It "Should use try-catch blocks for critical operations" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            ($scriptContent | Select-String -Pattern '\btry\b').Count | Should -BeGreaterThan 0
            ($scriptContent | Select-String -Pattern '\bcatch\b').Count | Should -BeGreaterThan 0
        }
    }

    Context "Help Documentation" {
        It "Should have synopsis" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match '\.SYNOPSIS'
        }

        It "Should have description" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match '\.DESCRIPTION'
        }

        It "Should have examples" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match '\.EXAMPLE'
        }

        It "Should document Kestrel vs HTTP.sys distinction" {
            $scriptContent = Get-Content $script:scriptPath -Raw
            $scriptContent | Should -Match 'Kestrel'
            $scriptContent | Should -Match 'HTTP\.sys'
            $scriptContent | Should -Match 'UseHttpSys'
        }
    }
}

