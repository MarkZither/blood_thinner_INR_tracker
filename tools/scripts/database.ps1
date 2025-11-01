#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Database management script for Blood Thinner INR Tracker

.DESCRIPTION
    Provides commands to reset, migrate, seed, and backup the PostgreSQL database
    Used for development with .NET Aspire orchestration

.PARAMETER Command
    The database operation to perform:
    - reset: Drop and recreate database (removes all data)
    - migrate: Apply pending migrations
    - seed: Seed test data
    - backup: Create database backup
    - restore: Restore from backup

.PARAMETER Environment
    Target environment (Development, Staging, Production)
    Default: Development

.EXAMPLE
    .\database.ps1 -Command reset
    Drops and recreates the database

.EXAMPLE
    .\database.ps1 -Command migrate -Environment Staging
    Applies migrations to staging database
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("reset", "migrate", "seed", "backup", "restore")]
    [string]$Command,

    [Parameter(Mandatory = $false)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment = "Development"
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Script configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$ApiProject = Join-Path $RootDir "src/BloodThinnerTracker.Api"

# Database configuration
$DatabaseNames = @{
    "Development" = "bloodtracker_dev"
    "Staging"     = "bloodtracker_staging"
    "Production"  = "bloodtracker_prod"
}
$DatabaseName = $DatabaseNames[$Environment]

# Docker container name (Aspire-generated)
$ContainerName = "postgres-3fb14488"  # This may vary, we'll try to detect it

# Colors for output
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-ErrorMsg { Write-Host $args -ForegroundColor Red }

# Detect PostgreSQL container
function Get-PostgresContainer {
    Write-Info "Detecting PostgreSQL container..."
    $containers = docker ps -a --filter "name=postgres" --format "{{.Names}}" | Where-Object { $_ -match "postgres" }

    if ($containers.Count -eq 0) {
        Write-ErrorMsg "No PostgreSQL container found. Is Aspire AppHost running?"
        Write-Info "Start it with: dotnet run --project src/BloodThinnerTracker.AppHost"
        exit 1
    }

    if ($containers -is [array]) {
        $container = $containers[0]
        Write-Warning "Multiple containers found, using: $container"
    } else {
        $container = $containers
    }

    Write-Success "Found container: $container"
    return $container
}

# Check if database exists
function Test-DatabaseExists {
    param([string]$Container, [string]$DbName)

    $env:PGPASSWORD = "postgres"
    $result = docker exec -e PGPASSWORD=$env:PGPASSWORD $Container psql -U postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$DbName'" 2>$null
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    return $result -eq "1"
}

# Reset database (drop and recreate)
function Reset-Database {
    param([string]$Container, [string]$DbName)

    Write-Info "Resetting database: $DbName"

    # Check if database exists
    if (Test-DatabaseExists -Container $Container -DbName $DbName) {
        Write-Warning "Dropping existing database: $DbName"

        # Terminate active connections
        docker exec -e PGPASSWORD=postgres $Container psql -U postgres -c @"
SELECT pg_terminate_backend(pg_stat_activity.pid)
FROM pg_stat_activity
WHERE pg_stat_activity.datname = '$DbName'
  AND pid <> pg_backend_pid();
"@ | Out-Null

        # Drop database
        docker exec -e PGPASSWORD=postgres $Container psql -U postgres -c "DROP DATABASE IF EXISTS $DbName;" | Out-Null
        Write-Success "Database dropped"
    } else {
        Write-Info "Database does not exist yet"
    }

    # Create database
    Write-Info "Creating database: $DbName"
    docker exec -e PGPASSWORD=postgres $Container psql -U postgres -c "CREATE DATABASE $DbName;" | Out-Null
    Write-Success "Database created"

    # Apply migrations
    Write-Info "Applying migrations..."
    Invoke-Migrate -Container $Container -DbName $DbName
}

# Apply migrations
function Invoke-Migrate {
    param([string]$Container, [string]$DbName)

    Write-Info "Applying EF Core migrations to: $DbName"

    Push-Location $RootDir
    try {
        # Set connection string environment variable
        $connectionString = "Host=localhost;Port=5432;Database=$DbName;Username=postgres;Password=postgres"
        $env:ConnectionStrings__bloodtracker = $connectionString

        # Apply migrations
        dotnet ef database update --project $ApiProject --startup-project $ApiProject --no-build

        if ($LASTEXITCODE -eq 0) {
            Write-Success "Migrations applied successfully"
        } else {
            Write-ErrorMsg "Migration failed with exit code: $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    } finally {
        Pop-Location
        Remove-Item Env:\ConnectionStrings__bloodtracker -ErrorAction SilentlyContinue
    }
}

# Seed test data
function Invoke-Seed {
    param([string]$Container, [string]$DbName)

    Write-Info "Seeding test data to: $DbName"
    Write-Warning "Seed functionality not yet implemented"
    # TODO: Implement seeding logic
}

# Backup database
function Invoke-Backup {
    param([string]$Container, [string]$DbName)

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFile = Join-Path $RootDir "backups/$DbName`_$timestamp.sql"
    $backupDir = Split-Path $backupFile

    if (-not (Test-Path $backupDir)) {
        New-Item -ItemType Directory -Path $backupDir | Out-Null
    }

    Write-Info "Backing up database: $DbName"
    Write-Info "Backup file: $backupFile"

    docker exec -e PGPASSWORD=postgres $Container pg_dump -U postgres $DbName > $backupFile

    if ($LASTEXITCODE -eq 0) {
        $size = (Get-Item $backupFile).Length / 1KB
        Write-Success "Backup completed: $([math]::Round($size, 2)) KB"
    } else {
        Write-ErrorMsg "Backup failed"
        exit $LASTEXITCODE
    }
}

# Restore database from backup
function Invoke-Restore {
    param([string]$Container, [string]$DbName)

    Write-Info "Available backups:"
    $backupDir = Join-Path $RootDir "backups"

    if (-not (Test-Path $backupDir)) {
        Write-ErrorMsg "No backups found in: $backupDir"
        exit 1
    }

    $backups = Get-ChildItem $backupDir -Filter "$DbName`_*.sql" | Sort-Object LastWriteTime -Descending

    if ($backups.Count -eq 0) {
        Write-ErrorMsg "No backups found for database: $DbName"
        exit 1
    }

    for ($i = 0; $i -lt $backups.Count; $i++) {
        Write-Host "[$i] $($backups[$i].Name) - $($backups[$i].LastWriteTime)"
    }

    $selection = Read-Host "Enter backup number to restore (or 'q' to quit)"

    if ($selection -eq 'q') {
        Write-Info "Restore cancelled"
        exit 0
    }

    $backupFile = $backups[[int]$selection].FullName
    Write-Warning "This will drop the existing database and restore from: $($backups[[int]$selection].Name)"
    $confirm = Read-Host "Continue? (yes/no)"

    if ($confirm -ne "yes") {
        Write-Info "Restore cancelled"
        exit 0
    }

    # Drop and recreate database
    Reset-Database -Container $Container -DbName $DbName

    # Restore from backup
    Write-Info "Restoring from backup..."
    Get-Content $backupFile | docker exec -i -e PGPASSWORD=postgres $Container psql -U postgres $DbName

    if ($LASTEXITCODE -eq 0) {
        Write-Success "Database restored successfully"
    } else {
        Write-ErrorMsg "Restore failed"
        exit $LASTEXITCODE
    }
}

# Main script execution
Write-Info "=== Blood Thinner INR Tracker - Database Management ==="
Write-Info "Environment: $Environment"
Write-Info "Database: $DatabaseName"
Write-Info ""

# Detect container
$container = Get-PostgresContainer

# Execute command
switch ($Command) {
    "reset" {
        Write-Warning "This will delete all data in $DatabaseName!"
        $confirm = Read-Host "Continue? (yes/no)"
        if ($confirm -eq "yes") {
            Reset-Database -Container $container -DbName $DatabaseName
        } else {
            Write-Info "Reset cancelled"
        }
    }
    "migrate" {
        Invoke-Migrate -Container $container -DbName $DatabaseName
    }
    "seed" {
        Invoke-Seed -Container $container -DbName $DatabaseName
    }
    "backup" {
        Invoke-Backup -Container $container -DbName $DatabaseName
    }
    "restore" {
        Invoke-Restore -Container $container -DbName $DatabaseName
    }
}

Write-Success "`n=== Operation completed ==="
