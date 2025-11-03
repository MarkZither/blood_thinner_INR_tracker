#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Resets the local PostgreSQL database by stopping containers and removing volumes.

.DESCRIPTION
    This script safely stops all running PostgreSQL containers used by the Blood Thinner Tracker
    application and removes their associated data volumes. This is useful for:
    - Starting fresh with a clean database
    - Resolving migration issues
    - Clearing test data
    - Fixing corrupted database state

.PARAMETER Force
    Skip confirmation prompt and proceed with deletion immediately.

.EXAMPLE
    .\reset-database.ps1
    # Interactive mode - prompts for confirmation

.EXAMPLE
    .\reset-database.ps1 -Force
    # Non-interactive mode - deletes immediately

.NOTES
    - This script requires Docker Desktop to be running
    - All data in the local PostgreSQL container will be permanently deleted
    - Production databases are NOT affected (this is local development only)
    - After running this script, press F5 in Visual Studio to recreate the database
    - Password configuration: Set POSTGRES_PASSWORD environment variable or use appsettings.json
    - The new container will use the configured password from AppHost

Author: Blood Thinner Tracker Development Team
Version: 1.1.0 - Added environment variable password support
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

# Set strict mode for better error handling
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Color output functions
function Write-Success {
    param([string]$Message)
    Write-Host "✓ " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ " -ForegroundColor Cyan -NoNewline
    Write-Host $Message
}

function Write-Warning-Custom {
    param([string]$Message)
    Write-Host "⚠ " -ForegroundColor Yellow -NoNewline
    Write-Host $Message
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "✗ " -ForegroundColor Red -NoNewline
    Write-Host $Message
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Blood Thinner Tracker - Database Reset Utility" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
Write-Info "Checking Docker status..."
try {
    $null = docker info 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Docker is not running!"
        Write-Host ""
        Write-Host "Please start Docker Desktop and try again." -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }
    Write-Success "Docker is running"
} catch {
    Write-Error-Custom "Failed to connect to Docker: $_"
    exit 1
}

# Find PostgreSQL containers related to Blood Thinner Tracker
Write-Info "Searching for PostgreSQL containers..."
$containers = docker ps -a --filter "ancestor=postgres" --format "{{.ID}}|{{.Names}}|{{.Status}}" |
    Where-Object { $_ -match "postgres|bloodtracker|aspire" }

if ($containers.Count -eq 0) {
    Write-Warning-Custom "No PostgreSQL containers found."
    Write-Host "This could mean:"
    Write-Host "  - The database has never been started (press F5 to create it)"
    Write-Host "  - Containers were already removed"
    Write-Host "  - Using a different database provider"
    Write-Host ""
    exit 0
}

Write-Success "Found $($containers.Count) PostgreSQL container(s)"
Write-Host ""

# Display containers
Write-Host "Containers to be stopped:" -ForegroundColor White
foreach ($container in $containers) {
    $parts = $container -split '\|'
    $id = $parts[0].Substring(0, [Math]::Min(12, $parts[0].Length))
    $name = $parts[1]
    $status = $parts[2]
    Write-Host "  • $name ($id) - $status" -ForegroundColor Gray
}
Write-Host ""

# Find data volumes
Write-Info "Searching for data volumes..."
$volumes = docker volume ls --filter "name=postgres" --format "{{.Name}}" |
    Where-Object { $_ -match "postgres|bloodtracker|aspire" }

if ($volumes.Count -gt 0) {
    Write-Success "Found $($volumes.Count) data volume(s)"
    Write-Host ""
    Write-Host "Volumes to be removed:" -ForegroundColor White
    foreach ($volume in $volumes) {
        Write-Host "  • $volume" -ForegroundColor Gray
    }
} else {
    Write-Info "No persistent data volumes found"
}
Write-Host ""

# Confirm deletion
if (-not $Force) {
    Write-Host "⚠️  WARNING: This will permanently delete ALL local database data!" -ForegroundColor Yellow
    Write-Host ""
    $confirmation = Read-Host "Are you sure you want to continue? (yes/no)"

    if ($confirmation -ne "yes") {
        Write-Info "Operation cancelled by user"
        exit 0
    }
    Write-Host ""
}

# Stop containers
Write-Info "Stopping PostgreSQL containers..."
$stopCount = 0
foreach ($container in $containers) {
    $id = ($container -split '\|')[0]
    try {
        docker stop $id 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $stopCount++
        }
    } catch {
        Write-Warning-Custom "Failed to stop container $id : $_"
    }
}
Write-Success "Stopped $stopCount container(s)"

# Remove containers
Write-Info "Removing PostgreSQL containers..."
$removeCount = 0
foreach ($container in $containers) {
    $id = ($container -split '\|')[0]
    try {
        docker rm $id 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $removeCount++
        }
    } catch {
        Write-Warning-Custom "Failed to remove container $id : $_"
    }
}
Write-Success "Removed $removeCount container(s)"

# Remove volumes
if ($volumes.Count -gt 0) {
    Write-Info "Removing data volumes..."
    $volumeRemoveCount = 0
    foreach ($volume in $volumes) {
        try {
            docker volume rm $volume 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                $volumeRemoveCount++
            }
        } catch {
            Write-Warning-Custom "Failed to remove volume $volume : $_"
        }
    }
    Write-Success "Removed $volumeRemoveCount volume(s)"
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Database Reset Complete!" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Press F5 in Visual Studio to start the application"
Write-Host "  2. Aspire will automatically create a fresh PostgreSQL container"
Write-Host "  3. Entity Framework migrations will run automatically"
Write-Host "  4. Your database will be ready with clean schema"
Write-Host ""
Write-Success "Database reset completed successfully"
Write-Host ""
