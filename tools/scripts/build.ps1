# Blood Thinner Tracker - Build Script (PowerShell)
# This script provides a unified build experience for all platforms on Windows

param(
    [Parameter(Position=0)]
    [ValidateSet("clean", "restore", "build", "build-release", "test", "package", "docker", "security", "ci", "all", "help")]
    [string]$Command = "help",
    
    [Parameter()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [Parameter()]
    [string]$TargetFramework = "net10.0"
)

# Configuration
$DotNetVersion = "10.0.x"
$SolutionFile = "BloodThinnerTracker.sln"
$BuildOutput = "./artifacts"

# Medical Disclaimer
Write-Host "⚠️  MEDICAL DISCLAIMER ⚠️" -ForegroundColor Red
Write-Host "This software is for informational purposes only and should not replace professional medical advice." -ForegroundColor Yellow
Write-Host "Always consult with your healthcare provider regarding your medication schedule." -ForegroundColor Yellow
Write-Host ""

# Helper functions
function Write-Status {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️ $Message" -ForegroundColor Yellow
}

# Check prerequisites
function Test-Prerequisites {
    Write-Status "Checking prerequisites..."
    
    # Check .NET CLI
    try {
        $dotnetVersion = & dotnet --version
        if ($dotnetVersion -notmatch "^10\.") {
            Write-Warning "Expected .NET 10.x, found $dotnetVersion"
            Write-Warning "This build may not work correctly with a different version."
        }
        Write-Success ".NET $dotnetVersion detected"
    }
    catch {
        Write-Error ".NET CLI not found. Please install .NET 10 SDK."
        exit 1
    }
    
    # Check Docker (optional)
    try {
        & docker --version | Out-Null
        Write-Success "Docker detected"
    }
    catch {
        Write-Warning "Docker not found - container builds will be skipped"
    }
}

# Clean build artifacts
function Invoke-Clean {
    Write-Status "Cleaning build artifacts..."
    
    # Remove build output
    if (Test-Path $BuildOutput) {
        Remove-Item -Recurse -Force $BuildOutput
    }
    
    # Clean .NET projects
    & dotnet clean $SolutionFile --verbosity quiet
    
    # Remove bin and obj directories
    Get-ChildItem -Path . -Recurse -Directory -Name "bin", "obj" | ForEach-Object {
        $path = Join-Path $PWD $_
        if (Test-Path $path) {
            Remove-Item -Recurse -Force $path
        }
    }
    
    Write-Success "Clean completed"
}

# Restore dependencies
function Invoke-Restore {
    Write-Status "Restoring dependencies..."
    & dotnet restore $SolutionFile --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Restore failed"
        exit 1
    }
    Write-Success "Dependencies restored"
}

# Build solution
function Invoke-Build {
    param(
        [string]$Config = "Debug",
        [string]$Framework = "net10.0"
    )
    
    Write-Status "Building solution ($Config)..."
    
    & dotnet build $SolutionFile `
        --configuration $Config `
        --no-restore `
        --verbosity quiet `
        --property:TargetFramework=$Framework
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed"
        exit 1
    }
    
    Write-Success "Build completed ($Config)"
}

# Run tests
function Invoke-Test {
    Write-Status "Running tests..."
    
    # Create test results directory
    New-Item -ItemType Directory -Force -Path "$BuildOutput/tests" | Out-Null
    
    # Run tests with coverage
    & dotnet test $SolutionFile `
        --configuration Release `
        --no-build `
        --verbosity normal `
        --logger "trx;LogFileName=test-results.trx" `
        --logger "html;LogFileName=test-results.html" `
        --results-directory "$BuildOutput/tests" `
        --collect:"XPlat Code Coverage" `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed"
        exit 1
    }
    
    Write-Success "Tests completed"
}

# Package applications
function Invoke-Package {
    param([string]$Config = "Release")
    
    Write-Status "Packaging applications..."
    
    # Create package directory
    New-Item -ItemType Directory -Force -Path "$BuildOutput/packages" | Out-Null
    
    # Package API
    Write-Status "Packaging API..."
    & dotnet publish "src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj" `
        --configuration $Config `
        --output "$BuildOutput/packages/api" `
        --self-contained false `
        --verbosity quiet
    
    # Package Web
    Write-Status "Packaging Web..."
    & dotnet publish "src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj" `
        --configuration $Config `
        --output "$BuildOutput/packages/web" `
        --self-contained false `
        --verbosity quiet
    
    # Package CLI
    Write-Status "Packaging CLI..."
    & dotnet publish "src/BloodThinnerTracker.Cli/BloodThinnerTracker.Cli.csproj" `
        --configuration $Config `
        --output "$BuildOutput/packages/cli" `
        --self-contained true `
        --verbosity quiet
    
    # Package MCP Server
    Write-Status "Packaging MCP Server..."
    & dotnet publish "src/BloodThinnerTracker.Mcp/BloodThinnerTracker.Mcp.csproj" `
        --configuration $Config `
        --output "$BuildOutput/packages/mcp" `
        --self-contained true `
        --verbosity quiet
    
    Write-Success "Packaging completed"
}

# Build Docker images
function Invoke-DockerBuild {
    try {
        & docker --version | Out-Null
    }
    catch {
        Write-Warning "Docker not found - skipping container builds"
        return
    }
    
    Write-Status "Building Docker images..."
    
    # Build API image
    & docker build -t bloodtracker-api:latest -f src/BloodThinnerTracker.Api/Dockerfile .
    
    # Build Web image
    & docker build -t bloodtracker-web:latest -f src/BloodThinnerTracker.Web/Dockerfile .
    
    Write-Success "Docker images built"
}

# Run security analysis
function Invoke-SecurityScan {
    Write-Status "Running security analysis..."
    
    # Create security output directory
    New-Item -ItemType Directory -Force -Path "$BuildOutput/security" | Out-Null
    
    # Run .NET security analyzers (already configured in Directory.Build.props)
    & dotnet build $SolutionFile `
        --configuration Release `
        --verbosity normal `
        --property:TreatWarningsAsErrors=true `
        --property:WarningsAsErrors="" `
        --property:WarningsNotAsErrors="CS1591" # XML documentation warnings
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Security analysis found issues"
        exit 1
    }
    
    Write-Success "Security analysis completed"
}

# Show help
function Show-Help {
    Write-Host "Blood Thinner Tracker Build Script (PowerShell)"
    Write-Host ""
    Write-Host "Usage: .\build.ps1 [COMMAND] [-Configuration <Debug|Release>]"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  clean          Clean build artifacts"
    Write-Host "  restore        Restore NuGet packages"
    Write-Host "  build          Build solution (Debug)"
    Write-Host "  build-release  Build solution (Release)"
    Write-Host "  test           Run tests with coverage"
    Write-Host "  package        Package applications"
    Write-Host "  docker         Build Docker images"
    Write-Host "  security       Run security analysis"
    Write-Host "  ci             Full CI pipeline (clean, restore, build, test, package)"
    Write-Host "  all            Full build (clean, restore, build, test, package, docker)"
    Write-Host "  help           Show this help"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\build.ps1 build"
    Write-Host "  .\build.ps1 ci"
    Write-Host "  .\build.ps1 all -Configuration Release"
}

# CI pipeline
function Invoke-CIPipeline {
    Write-Status "Running CI pipeline..."
    Invoke-Clean
    Invoke-Restore
    Invoke-Build -Config "Release"
    Invoke-Test
    Invoke-SecurityScan
    Invoke-Package -Config "Release"
    Write-Success "CI pipeline completed"
}

# Full build
function Invoke-FullBuild {
    Write-Status "Running full build..."
    Invoke-Clean
    Invoke-Restore
    Invoke-Build -Config "Release"
    Invoke-Test
    Invoke-SecurityScan
    Invoke-Package -Config "Release"
    Invoke-DockerBuild
    Write-Success "Full build completed"
}

# Main execution
try {
    switch ($Command) {
        "clean" {
            Test-Prerequisites
            Invoke-Clean
        }
        "restore" {
            Test-Prerequisites
            Invoke-Restore
        }
        "build" {
            Test-Prerequisites
            Invoke-Restore
            Invoke-Build -Config $Configuration
        }
        "build-release" {
            Test-Prerequisites
            Invoke-Restore
            Invoke-Build -Config "Release"
        }
        "test" {
            Test-Prerequisites
            Invoke-Restore
            Invoke-Build -Config "Release"
            Invoke-Test
        }
        "package" {
            Test-Prerequisites
            Invoke-Restore
            Invoke-Build -Config "Release"
            Invoke-Package -Config "Release"
        }
        "docker" {
            Test-Prerequisites
            Invoke-DockerBuild
        }
        "security" {
            Test-Prerequisites
            Invoke-Restore
            Invoke-SecurityScan
        }
        "ci" {
            Test-Prerequisites
            Invoke-CIPipeline
        }
        "all" {
            Test-Prerequisites
            Invoke-FullBuild
        }
        "help" {
            Show-Help
        }
        default {
            Write-Error "Unknown command: $Command"
            Show-Help
            exit 1
        }
    }
}
catch {
    Write-Error "Build script failed: $_"
    exit 1
}