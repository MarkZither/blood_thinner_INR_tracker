.Describe 'deploy-windows-baremetal.ps1 logic (scaffold)' {
    It 'Should exist in tools folder' {
        Test-Path -Path (Join-Path C:\Source\github\blood_thinner_INR_tracker\tests\DeployScripts.Tests '..\..\tools\deploy-windows-baremetal.ps1') | Should -BeTrue
    }

    It 'Should contain a check for .NET or runtime info' {
         = Get-Content -Path (Join-Path C:\Source\github\blood_thinner_INR_tracker\tests\DeployScripts.Tests '..\..\tools\deploy-windows-baremetal.ps1') -ErrorAction SilentlyContinue
         -match 'dotnet|DotNet|Get-Command' | Should -BeTrue
    }
}
