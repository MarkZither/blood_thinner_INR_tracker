#!/bin/bash

# Blood Thinner Tracker - Build Script
# This script provides a unified build experience for all platforms

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
DOTNET_VERSION="10.0.x"
SOLUTION_FILE="BloodThinnerTracker.sln"
BUILD_OUTPUT="./artifacts"

# Medical Disclaimer
echo -e "${RED}⚠️  MEDICAL DISCLAIMER ⚠️${NC}"
echo -e "${YELLOW}This software is for informational purposes only and should not replace professional medical advice.${NC}"
echo -e "${YELLOW}Always consult with your healthcare provider regarding your medication schedule.${NC}"
echo ""

# Helper function to print status
print_status() {
    echo -e "${BLUE}==>${NC} ${1}"
}

print_success() {
    echo -e "${GREEN}✅${NC} ${1}"
}

print_error() {
    echo -e "${RED}❌${NC} ${1}"
}

print_warning() {
    echo -e "${YELLOW}⚠️${NC} ${1}"
}

# Check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    # Check .NET
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET CLI not found. Please install .NET 10 SDK."
        exit 1
    fi
    
    # Check .NET version
    INSTALLED_VERSION=$(dotnet --version)
    if [[ ! $INSTALLED_VERSION == 10.* ]]; then
        print_warning "Expected .NET 10.x, found $INSTALLED_VERSION"
        print_warning "This build may not work correctly with a different version."
    fi
    
    print_success ".NET $INSTALLED_VERSION detected"
    
    # Check Docker (optional)
    if command -v docker &> /dev/null; then
        print_success "Docker detected"
    else
        print_warning "Docker not found - container builds will be skipped"
    fi
}

# Clean build artifacts
clean() {
    print_status "Cleaning build artifacts..."
    
    # Remove build output
    rm -rf "$BUILD_OUTPUT"
    
    # Clean .NET projects
    dotnet clean "$SOLUTION_FILE" --verbosity quiet
    
    # Remove bin and obj directories
    find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
    find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
    
    print_success "Clean completed"
}

# Restore dependencies
restore() {
    print_status "Restoring dependencies..."
    dotnet restore "$SOLUTION_FILE" --verbosity quiet
    print_success "Dependencies restored"
}

# Build solution
build() {
    local configuration=${1:-Debug}
    local target_framework=${2:-net10.0}
    
    print_status "Building solution (${configuration})..."
    
    dotnet build "$SOLUTION_FILE" \
        --configuration "$configuration" \
        --no-restore \
        --verbosity quiet \
        --property:TargetFramework="$target_framework"
    
    print_success "Build completed (${configuration})"
}

# Run tests
test() {
    print_status "Running tests..."
    
    # Create test results directory
    mkdir -p "$BUILD_OUTPUT/tests"
    
    # Run tests with coverage
    dotnet test "$SOLUTION_FILE" \
        --configuration Release \
        --no-build \
        --verbosity normal \
        --logger "trx;LogFileName=test-results.trx" \
        --logger "html;LogFileName=test-results.html" \
        --results-directory "$BUILD_OUTPUT/tests" \
        --collect:"XPlat Code Coverage" \
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
    
    print_success "Tests completed"
}

# Package applications
package() {
    local configuration=${1:-Release}
    
    print_status "Packaging applications..."
    
    # Create package directory
    mkdir -p "$BUILD_OUTPUT/packages"
    
    # Package API
    print_status "Packaging API..."
    dotnet publish "src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj" \
        --configuration "$configuration" \
        --output "$BUILD_OUTPUT/packages/api" \
        --self-contained false \
        --verbosity quiet
    
    # Package Web
    print_status "Packaging Web..."
    dotnet publish "src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj" \
        --configuration "$configuration" \
        --output "$BUILD_OUTPUT/packages/web" \
        --self-contained false \
        --verbosity quiet
    
    # Package CLI
    print_status "Packaging CLI..."
    dotnet publish "src/BloodThinnerTracker.Cli/BloodThinnerTracker.Cli.csproj" \
        --configuration "$configuration" \
        --output "$BUILD_OUTPUT/packages/cli" \
        --self-contained true \
        --verbosity quiet
    
    # Package MCP Server
    print_status "Packaging MCP Server..."
    dotnet publish "src/BloodThinnerTracker.Mcp/BloodThinnerTracker.Mcp.csproj" \
        --configuration "$configuration" \
        --output "$BUILD_OUTPUT/packages/mcp" \
        --self-contained true \
        --verbosity quiet
    
    print_success "Packaging completed"
}

# Build Docker images
docker_build() {
    if ! command -v docker &> /dev/null; then
        print_warning "Docker not found - skipping container builds"
        return
    fi
    
    print_status "Building Docker images..."
    
    # Build API image
    docker build -t bloodtracker-api:latest -f src/BloodThinnerTracker.Api/Dockerfile .
    
    # Build Web image
    docker build -t bloodtracker-web:latest -f src/BloodThinnerTracker.Web/Dockerfile .
    
    print_success "Docker images built"
}

# Run security analysis
security_scan() {
    print_status "Running security analysis..."
    
    # Create security output directory
    mkdir -p "$BUILD_OUTPUT/security"
    
    # Run .NET security analyzers (already configured in Directory.Build.props)
    dotnet build "$SOLUTION_FILE" \
        --configuration Release \
        --verbosity normal \
        --property:TreatWarningsAsErrors=true \
        --property:WarningsAsErrors="" \
        --property:WarningsNotAsErrors="CS1591" # XML documentation warnings
    
    print_success "Security analysis completed"
}

# Show help
show_help() {
    echo "Blood Thinner Tracker Build Script"
    echo ""
    echo "Usage: $0 [COMMAND] [OPTIONS]"
    echo ""
    echo "Commands:"
    echo "  clean          Clean build artifacts"
    echo "  restore        Restore NuGet packages"
    echo "  build          Build solution (Debug)"
    echo "  build-release  Build solution (Release)"
    echo "  test           Run tests with coverage"
    echo "  package        Package applications"
    echo "  docker         Build Docker images"
    echo "  security       Run security analysis"
    echo "  ci             Full CI pipeline (clean, restore, build, test, package)"
    echo "  all            Full build (clean, restore, build, test, package, docker)"
    echo "  help           Show this help"
    echo ""
    echo "Examples:"
    echo "  $0 build"
    echo "  $0 ci"
    echo "  $0 all"
}

# CI pipeline
ci_pipeline() {
    print_status "Running CI pipeline..."
    clean
    restore
    build Release
    test
    security_scan
    package Release
    print_success "CI pipeline completed"
}

# Full build
full_build() {
    print_status "Running full build..."
    clean
    restore
    build Release
    test
    security_scan
    package Release
    docker_build
    print_success "Full build completed"
}

# Main execution
main() {
    local command=${1:-help}
    
    case $command in
        clean)
            check_prerequisites
            clean
            ;;
        restore)
            check_prerequisites
            restore
            ;;
        build)
            check_prerequisites
            restore
            build Debug
            ;;
        build-release)
            check_prerequisites
            restore
            build Release
            ;;
        test)
            check_prerequisites
            restore
            build Release
            test
            ;;
        package)
            check_prerequisites
            restore
            build Release
            package Release
            ;;
        docker)
            check_prerequisites
            docker_build
            ;;
        security)
            check_prerequisites
            restore
            security_scan
            ;;
        ci)
            check_prerequisites
            ci_pipeline
            ;;
        all)
            check_prerequisites
            full_build
            ;;
        help|--help|-h)
            show_help
            ;;
        *)
            print_error "Unknown command: $command"
            show_help
            exit 1
            ;;
    esac
}

# Run main function
main "$@"