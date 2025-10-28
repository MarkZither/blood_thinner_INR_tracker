#!/bin/bash

# Blood Thinner Tracker - Deployment Script
# Automates deployment to various environments (dev, staging, production)

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
DOCKER_REGISTRY="bloodtrackerregistry.azurecr.io"
KUBECTL_CONTEXT=""
HELM_CHART_PATH="./helm/bloodtracker"
ENVIRONMENTS=("dev" "staging" "production")

# Medical Disclaimer
echo -e "${RED}⚠️  MEDICAL DEPLOYMENT DISCLAIMER ⚠️${NC}"
echo -e "${YELLOW}This deployment script handles medical application infrastructure.${NC}"
echo -e "${YELLOW}Ensure all compliance and security requirements are met before deploying.${NC}"
echo ""

# Helper functions
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

# Validate environment
validate_environment() {
    local env=$1
    
    if [[ ! " ${ENVIRONMENTS[@]} " =~ " ${env} " ]]; then
        print_error "Invalid environment: $env"
        print_error "Valid environments: ${ENVIRONMENTS[*]}"
        exit 1
    fi
    
    print_success "Environment validated: $env"
}

# Check prerequisites
check_prerequisites() {
    local env=$1
    
    print_status "Checking deployment prerequisites for $env environment..."
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        print_error "Docker not found. Please install Docker."
        exit 1
    fi
    
    # Check kubectl
    if ! command -v kubectl &> /dev/null; then
        print_error "kubectl not found. Please install kubectl."
        exit 1
    fi
    
    # Check Helm
    if ! command -v helm &> /dev/null; then
        print_error "Helm not found. Please install Helm."
        exit 1
    fi
    
    # Check Azure CLI (if using Azure)
    if ! command -v az &> /dev/null; then
        print_warning "Azure CLI not found. Some features may not work."
    fi
    
    # Set kubectl context based on environment
    case $env in
        "dev")
            KUBECTL_CONTEXT="bloodtracker-dev"
            ;;
        "staging")
            KUBECTL_CONTEXT="bloodtracker-staging"
            ;;
        "production")
            KUBECTL_CONTEXT="bloodtracker-prod"
            ;;
    esac
    
    print_success "Prerequisites checked"
}

# Build and push Docker images
build_and_push_images() {
    local version=$1
    
    print_status "Building and pushing Docker images (version: $version)..."
    
    # Build images
    docker build -t "${DOCKER_REGISTRY}/bloodtracker-api:${version}" -f src/BloodThinnerTracker.Api/Dockerfile .
    docker build -t "${DOCKER_REGISTRY}/bloodtracker-web:${version}" -f src/BloodThinnerTracker.Web/Dockerfile .
    
    # Tag as latest for non-production
    if [[ "$2" != "production" ]]; then
        docker tag "${DOCKER_REGISTRY}/bloodtracker-api:${version}" "${DOCKER_REGISTRY}/bloodtracker-api:latest"
        docker tag "${DOCKER_REGISTRY}/bloodtracker-web:${version}" "${DOCKER_REGISTRY}/bloodtracker-web:latest"
    fi
    
    # Push images
    docker push "${DOCKER_REGISTRY}/bloodtracker-api:${version}"
    docker push "${DOCKER_REGISTRY}/bloodtracker-web:${version}"
    
    if [[ "$2" != "production" ]]; then
        docker push "${DOCKER_REGISTRY}/bloodtracker-api:latest"
        docker push "${DOCKER_REGISTRY}/bloodtracker-web:latest"
    fi
    
    print_success "Docker images built and pushed"
}

# Deploy database migrations
deploy_database() {
    local env=$1
    local connection_string_secret="bloodtracker-${env}-db-connection"
    
    print_status "Deploying database migrations for $env..."
    
    # Get connection string from Kubernetes secret
    local connection_string=$(kubectl get secret "$connection_string_secret" -o jsonpath='{.data.connectionString}' | base64 -d)
    
    if [[ -z "$connection_string" ]]; then
        print_error "Could not retrieve database connection string"
        exit 1
    fi
    
    # Run migrations using a temporary job
    kubectl apply -f - <<EOF
apiVersion: batch/v1
kind: Job
metadata:
  name: bloodtracker-migration-$(date +%s)
  namespace: bloodtracker-${env}
spec:
  template:
    spec:
      containers:
      - name: migration
        image: ${DOCKER_REGISTRY}/bloodtracker-api:${version}
        command: ["dotnet", "ef", "database", "update"]
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: ${connection_string_secret}
              key: connectionString
        - name: ASPNETCORE_ENVIRONMENT
          value: "${env^}" # Capitalize first letter
      restartPolicy: Never
  backoffLimit: 3
EOF
    
    # Wait for migration to complete
    local job_name=$(kubectl get jobs -l app=bloodtracker-migration --sort-by=.metadata.creationTimestamp -o jsonpath='{.items[-1].metadata.name}')
    kubectl wait --for=condition=complete job/$job_name --timeout=300s
    
    print_success "Database migrations completed"
}

# Deploy to Kubernetes using Helm
deploy_helm() {
    local env=$1
    local version=$2
    
    print_status "Deploying to Kubernetes ($env) using Helm..."
    
    # Switch to correct kubectl context
    kubectl config use-context "$KUBECTL_CONTEXT"
    
    # Create namespace if it doesn't exist
    kubectl create namespace "bloodtracker-${env}" --dry-run=client -o yaml | kubectl apply -f -
    
    # Deploy using Helm
    helm upgrade --install "bloodtracker-${env}" "$HELM_CHART_PATH" \
        --namespace "bloodtracker-${env}" \
        --values "./helm/values-${env}.yaml" \
        --set image.tag="$version" \
        --set environment="$env" \
        --wait \
        --timeout=10m
    
    print_success "Helm deployment completed"
}

# Run health checks
run_health_checks() {
    local env=$1
    
    print_status "Running health checks for $env environment..."
    
    # Get service URLs
    local api_url
    local web_url
    
    case $env in
        "dev")
            api_url="https://api-dev.bloodtracker.com"
            web_url="https://dev.bloodtracker.com"
            ;;
        "staging")
            api_url="https://api-staging.bloodtracker.com"
            web_url="https://staging.bloodtracker.com"
            ;;
        "production")
            api_url="https://api.bloodtracker.com"
            web_url="https://bloodtracker.com"
            ;;
    esac
    
    # Check API health
    print_status "Checking API health..."
    local api_health=$(curl -s -o /dev/null -w "%{http_code}" "${api_url}/health" || echo "000")
    
    if [[ "$api_health" == "200" ]]; then
        print_success "API health check passed"
    else
        print_error "API health check failed (HTTP $api_health)"
        return 1
    fi
    
    # Check Web health
    print_status "Checking Web health..."
    local web_health=$(curl -s -o /dev/null -w "%{http_code}" "${web_url}" || echo "000")
    
    if [[ "$web_health" == "200" ]]; then
        print_success "Web health check passed"
    else
        print_error "Web health check failed (HTTP $web_health)"
        return 1
    fi
    
    print_success "All health checks passed"
}

# Send deployment notification
send_notification() {
    local env=$1
    local version=$2
    local status=$3
    
    if [[ -n "$SLACK_WEBHOOK_URL" ]]; then
        local color="good"
        local emoji="✅"
        
        if [[ "$status" != "success" ]]; then
            color="danger"
            emoji="❌"
        fi
        
        curl -X POST -H 'Content-type: application/json' \
            --data "{
                \"text\":\"$emoji Blood Thinner Tracker deployment to $env\",
                \"attachments\":[{
                    \"color\":\"$color\",
                    \"fields\":[{
                        \"title\":\"Environment\",
                        \"value\":\"$env\",
                        \"short\":true
                    },{
                        \"title\":\"Version\",
                        \"value\":\"$version\",
                        \"short\":true
                    },{
                        \"title\":\"Status\",
                        \"value\":\"$status\",
                        \"short\":true
                    }]
                }]
            }" \
            "$SLACK_WEBHOOK_URL"
    fi
}

# Rollback deployment
rollback_deployment() {
    local env=$1
    local revision=${2:-1}
    
    print_status "Rolling back $env deployment..."
    
    kubectl config use-context "$KUBECTL_CONTEXT"
    
    # Rollback using Helm
    helm rollback "bloodtracker-${env}" "$revision" --namespace "bloodtracker-${env}"
    
    print_success "Rollback completed"
}

# Show deployment status
show_status() {
    local env=$1
    
    print_status "Deployment status for $env:"
    
    kubectl config use-context "$KUBECTL_CONTEXT"
    
    # Show Helm releases
    helm list --namespace "bloodtracker-${env}"
    
    # Show pod status
    kubectl get pods --namespace "bloodtracker-${env}"
    
    # Show service status
    kubectl get services --namespace "bloodtracker-${env}"
}

# Show help
show_help() {
    echo "Blood Thinner Tracker Deployment Script"
    echo ""
    echo "Usage: $0 [COMMAND] [ENVIRONMENT] [VERSION]"
    echo ""
    echo "Commands:"
    echo "  deploy      Deploy to environment"
    echo "  rollback    Rollback deployment"
    echo "  status      Show deployment status"
    echo "  health      Run health checks"
    echo "  help        Show this help"
    echo ""
    echo "Environments: ${ENVIRONMENTS[*]}"
    echo ""
    echo "Examples:"
    echo "  $0 deploy dev v1.2.3"
    echo "  $0 rollback production 2"
    echo "  $0 status staging"
    echo "  $0 health production"
    echo ""
    echo "Environment Variables:"
    echo "  SLACK_WEBHOOK_URL    Slack webhook for notifications"
    echo "  DOCKER_REGISTRY      Docker registry URL (default: $DOCKER_REGISTRY)"
}

# Main execution
main() {
    local command=${1:-help}
    local environment=${2:-}
    local version=${3:-}
    
    case $command in
        deploy)
            if [[ -z "$environment" || -z "$version" ]]; then
                print_error "Usage: $0 deploy <environment> <version>"
                exit 1
            fi
            
            validate_environment "$environment"
            check_prerequisites "$environment"
            
            print_status "Starting deployment to $environment (version: $version)..."
            
            # Build and push images
            build_and_push_images "$version" "$environment"
            
            # Deploy database
            deploy_database "$environment"
            
            # Deploy to Kubernetes
            deploy_helm "$environment" "$version"
            
            # Wait a bit for services to start
            sleep 30
            
            # Run health checks
            if run_health_checks "$environment"; then
                send_notification "$environment" "$version" "success"
                print_success "Deployment to $environment completed successfully"
            else
                send_notification "$environment" "$version" "failed"
                print_error "Deployment to $environment failed health checks"
                exit 1
            fi
            ;;
            
        rollback)
            if [[ -z "$environment" ]]; then
                print_error "Usage: $0 rollback <environment> [revision]"
                exit 1
            fi
            
            validate_environment "$environment"
            check_prerequisites "$environment"
            rollback_deployment "$environment" "$version"
            ;;
            
        status)
            if [[ -z "$environment" ]]; then
                print_error "Usage: $0 status <environment>"
                exit 1
            fi
            
            validate_environment "$environment"
            check_prerequisites "$environment"
            show_status "$environment"
            ;;
            
        health)
            if [[ -z "$environment" ]]; then
                print_error "Usage: $0 health <environment>"
                exit 1
            fi
            
            validate_environment "$environment"
            run_health_checks "$environment"
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