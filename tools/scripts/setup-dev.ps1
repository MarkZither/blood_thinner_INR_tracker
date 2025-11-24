<#
.SYNOPSIS
    Initialize development environment from template files and add secrets.

.DESCRIPTION
    Creates local configuration files (.vscode/launch.json, .vscode/tasks.json, etc.)
    from template files and optionally adds OAuth secrets. Template files are committed
    to git and safe to share. Local files are .gitignore'd.

.EXAMPLE
    .\tools\scripts\setup-dev.ps1
    # Copies templates, prompts for optional secrets interactively

    .\tools\scripts\setup-dev.ps1 -AzureClientId "your-id" -GoogleClientId "your-id"
    # Copies templates and adds secrets automatically

.PARAMETER AzureClientId
    Azure AD OAuth Client ID (optional - will be prompted if not provided)

.PARAMETER GoogleClientId
    Google OAuth Client ID (optional - will be prompted if not provided)

.PARAMETER DbPassword
    Database password for local PostgreSQL (default: "postgres")

.PARAMETER Force
    Overwrite existing local files (default: skip if exists)
#>

param(
    [string]$AzureClientId = "",
    [string]$GoogleClientId = "",
    [string]$DbPassword = "postgres",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$WarningPreference = "SilentlyContinue"

Write-Host @"
╔════════════════════════════════════════════════════════════════╗
║  Blood Thinner Tracker - Development Setup                    ║
║  Creating local configuration from templates...               ║
╚════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

$templateFiles = @(
    @{
        Template = ".vscode/launch.json.template"
        Target = ".vscode/launch.json"
        Description = "VS Code debug configurations"
    }
)

# Create local config files from templates
foreach ($file in $templateFiles) {
    $templatePath = $file.Template
    $targetPath = $file.Target
    $description = $file.Description

    if (-not (Test-Path $templatePath)) {
        Write-Host "❌ Template not found: $templatePath" -ForegroundColor Red
        Write-Host "   Make sure you're in the repository root directory" -ForegroundColor Yellow
        exit 1
    }

    if (Test-Path $targetPath) {
        if ($Force) {
            Write-Host "⚠️  Overwriting $targetPath" -ForegroundColor Yellow
            Remove-Item $targetPath
        }
        else {
            Write-Host "⏭️  Skipping $targetPath - already exists (use -Force to overwrite)" -ForegroundColor Yellow
            continue
        }
    }

    Copy-Item $templatePath $targetPath
    Write-Host "✅ Created $targetPath ($description)" -ForegroundColor Green
}

# Prompt for secrets if not provided
if (-not $AzureClientId) {
    Write-Host "`n📝 Enter your OAuth credentials (or press Enter to skip):" -ForegroundColor Cyan
    $AzureClientId = Read-Host "   Azure Client ID"
}

if (-not $GoogleClientId) {
    $GoogleClientId = Read-Host "   Google Client ID"
}

# Update launch.json with secrets if provided
if ($AzureClientId -or $GoogleClientId) {
    Write-Host "`n🔐 Updating .vscode/launch.json with secrets..." -ForegroundColor Cyan

    $launchJsonPath = ".vscode/launch.json"
    if (-not (Test-Path $launchJsonPath)) {
        Write-Host "❌ launch.json not found at $launchJsonPath" -ForegroundColor Red
        exit 1
    }

    $content = Get-Content $launchJsonPath -Raw

    if ($AzureClientId) {
        $content = $content -replace '\$\{REPLACE_WITH_AZURE_CLIENT_ID\}', $AzureClientId
        Write-Host "✅ Added Azure Client ID" -ForegroundColor Green
    }
    if ($GoogleClientId) {
        $content = $content -replace '\$\{REPLACE_WITH_GOOGLE_CLIENT_ID\}', $GoogleClientId
        Write-Host "✅ Added Google Client ID" -ForegroundColor Green
    }

    Set-Content $launchJsonPath $content -Encoding UTF8
    Write-Host "✅ launch.json updated (file is .gitignore'd - safe!)" -ForegroundColor Green
}

# Optional: Set up dotnet user secrets for API project
Write-Host "`n❓ Would you like to set up .NET User Secrets for the API project?" -ForegroundColor Cyan
Write-Host "   This stores secrets securely in %APPDATA%\Microsoft\UserSecrets\" -ForegroundColor Gray
$setupSecrets = Read-Host "   Setup user secrets? (y/n)"

if ($setupSecrets -eq "y") {
    Write-Host "`n🔑 Initializing .NET User Secrets..." -ForegroundColor Cyan

    Push-Location "src/BloodThinnerTracker.Api"
    try {
        dotnet user-secrets init 2>&1 | Out-Null

        if ($AzureClientId) {
            dotnet user-secrets set "OAuth:Azure:ClientId" $AzureClientId 2>&1 | Out-Null
            Write-Host "✅ Stored Azure Client ID in user secrets" -ForegroundColor Green
        }

        if ($GoogleClientId) {
            dotnet user-secrets set "OAuth:Google:ClientId" $GoogleClientId 2>&1 | Out-Null
            Write-Host "✅ Stored Google Client ID in user secrets" -ForegroundColor Green
        }

        dotnet user-secrets set "Database:Password" $DbPassword 2>&1 | Out-Null
        Write-Host "✅ Stored database password in user secrets" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}

Write-Host @"

╔════════════════════════════════════════════════════════════════╗
║  ✅ Setup Complete!                                           ║
╚════════════════════════════════════════════════════════════════╝

📋 Next steps:

1. Start the Aspire AppHost:
   dotnet run --project src/BloodThinnerTracker.AppHost

2. In VS Code, press F5 and select:
   • "📱 Mobile + Backend (Real Services)" - Start full stack
   • "🚀 Full Stack (API + Web)" - Start API and Web only
   • "Launch MAUI (Windows)" - Start mobile app with mocks

3. Get OAuth token for API testing:
   Open http://localhost:5174/oauth-test.html in browser

📚 Documentation:
   • Setup: docs/DEVELOPMENT_SETUP.md
   • OAuth: docs/OAUTH_TESTING_GUIDE.md
   • AppHost: docs/ASPIRE_IMPLEMENTATION.md

"@ -ForegroundColor Green

Write-Host "ℹ️  Template & local files:" -ForegroundColor Cyan
Write-Host "   ✅ .vscode/launch.json.template    (committed to git)" -ForegroundColor Gray
Write-Host "   ✅ .vscode/launch.json              (local only, .gitignore'd)" -ForegroundColor Gray
Write-Host "" -ForegroundColor Gray
