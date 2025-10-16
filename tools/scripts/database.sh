#!/bin/bash

# Blood Thinner Tracker - Database Management Script
# Handles migrations, backups, and database operations for medical data

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
API_PROJECT="src/BloodThinnerTracker.Api"
BACKUP_DIR="./backups/database"
MIGRATION_DIR="$API_PROJECT/Migrations"

# Medical Disclaimer
echo -e "${RED}⚠️  MEDICAL DATABASE DISCLAIMER ⚠️${NC}"
echo -e "${YELLOW}This script manages medical database operations and must be used with extreme care.${NC}"
echo -e "${YELLOW}Always backup medical data before performing migrations or maintenance operations.${NC}"
echo -e "${YELLOW}Ensure compliance with healthcare data protection regulations (HIPAA, GDPR, etc.).${NC}"
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

# Check prerequisites
check_prerequisites() {
    print_status "Checking database management prerequisites..."
    
    # Check .NET CLI
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET CLI not found. Please install .NET 10 SDK."
        exit 1
    fi
    
    # Check EF Core tools
    if ! dotnet tool list -g | grep -q "dotnet-ef"; then
        print_warning "Entity Framework Core tools not installed globally. Installing..."
        dotnet tool install --global dotnet-ef --version 10.0.0-*
    fi
    
    # Check if API project exists
    if [[ ! -f "$API_PROJECT/BloodThinnerTracker.Api.csproj" ]]; then
        print_error "API project not found at $API_PROJECT"
        exit 1
    fi
    
    print_success "Prerequisites checked"
}

# Create backup directory
ensure_backup_directory() {
    mkdir -p "$BACKUP_DIR"
    print_success "Backup directory ensured: $BACKUP_DIR"
}

# Backup database (SQLite)
backup_sqlite() {
    local environment=${1:-Development}
    local backup_name="backup_$(date +%Y%m%d_%H%M%S).db"
    local backup_path="$BACKUP_DIR/$backup_name"
    
    print_status "Creating SQLite backup for $environment environment..."
    
    # Get database path from appsettings
    local db_path
    case $environment in
        "Development")
            db_path="./bloodtracker_dev.db"
            ;;
        "Staging")
            db_path="./bloodtracker_staging.db"
            ;;
        "Production")
            db_path="./bloodtracker_prod.db"
            ;;
    esac
    
    if [[ -f "$db_path" ]]; then
        cp "$db_path" "$backup_path"
        
        # Compress the backup
        gzip "$backup_path"
        
        print_success "SQLite backup created: ${backup_path}.gz"
        
        # Keep only last 30 backups
        find "$BACKUP_DIR" -name "backup_*.db.gz" -type f -mtime +30 -delete
        print_success "Old backups cleaned up (kept last 30 days)"
    else
        print_warning "Database file not found: $db_path"
    fi
}

# Backup database (PostgreSQL)
backup_postgresql() {
    local environment=${1:-Development}
    local backup_name="backup_postgresql_$(date +%Y%m%d_%H%M%S).sql"
    local backup_path="$BACKUP_DIR/$backup_name"
    
    print_status "Creating PostgreSQL backup for $environment environment..."
    
    # Get connection string from environment or configuration
    local connection_string=""
    case $environment in
        "Development")
            connection_string=${DEV_DB_CONNECTION:-""}
            ;;
        "Staging")
            connection_string=${STAGING_DB_CONNECTION:-""}
            ;;
        "Production")
            connection_string=${PROD_DB_CONNECTION:-""}
            ;;
    esac
    
    if [[ -n "$connection_string" ]]; then
        # Extract connection details (simplified)
        pg_dump "$connection_string" > "$backup_path"
        
        # Compress the backup
        gzip "$backup_path"
        
        print_success "PostgreSQL backup created: ${backup_path}.gz"
        
        # Keep only last 30 backups
        find "$BACKUP_DIR" -name "backup_postgresql_*.sql.gz" -type f -mtime +30 -delete
        print_success "Old backups cleaned up (kept last 30 days)"
    else
        print_warning "PostgreSQL connection string not found for $environment"
    fi
}

# List migrations
list_migrations() {
    print_status "Listing Entity Framework migrations..."
    
    cd "$API_PROJECT"
    dotnet ef migrations list
    cd - > /dev/null
}

# Create new migration
create_migration() {
    local migration_name=$1
    
    if [[ -z "$migration_name" ]]; then
        print_error "Migration name is required"
        exit 1
    fi
    
    print_status "Creating new migration: $migration_name"
    
    # Backup database before migration
    backup_sqlite "Development"
    
    cd "$API_PROJECT"
    
    # Create migration
    dotnet ef migrations add "$migration_name" --verbose
    
    print_success "Migration created: $migration_name"
    
    # Show migration files
    print_status "Migration files created:"
    ls -la "$MIGRATION_DIR"/*"$migration_name"* 2>/dev/null || true
    
    cd - > /dev/null
    
    print_warning "Review the generated migration files before applying!"
    print_warning "Medical data migrations require extra validation."
}

# Apply migrations
apply_migrations() {
    local environment=${1:-Development}
    
    print_status "Applying migrations to $environment environment..."
    
    # Create backup before applying migrations
    backup_sqlite "$environment"
    
    cd "$API_PROJECT"
    
    # Set environment
    export ASPNETCORE_ENVIRONMENT="$environment"
    
    # Apply migrations
    dotnet ef database update --verbose
    
    print_success "Migrations applied to $environment database"
    
    cd - > /dev/null
}

# Rollback migration
rollback_migration() {
    local target_migration=$1
    local environment=${2:-Development}
    
    if [[ -z "$target_migration" ]]; then
        print_error "Target migration is required"
        exit 1
    fi
    
    print_warning "Rolling back to migration: $target_migration in $environment"
    print_warning "This operation may result in data loss!"
    
    read -p "Are you sure you want to proceed? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_status "Rollback cancelled"
        exit 0
    fi
    
    # Create backup before rollback
    backup_sqlite "$environment"
    
    cd "$API_PROJECT"
    
    # Set environment
    export ASPNETCORE_ENVIRONMENT="$environment"
    
    # Rollback to target migration
    dotnet ef database update "$target_migration" --verbose
    
    print_success "Database rolled back to: $target_migration"
    
    cd - > /dev/null
}

# Remove migration
remove_migration() {
    print_status "Removing last migration..."
    
    cd "$API_PROJECT"
    
    # Show current migrations
    print_status "Current migrations:"
    dotnet ef migrations list
    
    print_warning "This will remove the last migration. Ensure it hasn't been applied to other environments!"
    
    read -p "Are you sure you want to remove the last migration? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_status "Migration removal cancelled"
        exit 0
    fi
    
    # Remove migration
    dotnet ef migrations remove --verbose
    
    print_success "Last migration removed"
    
    cd - > /dev/null
}

# Validate database schema
validate_schema() {
    local environment=${1:-Development}
    
    print_status "Validating database schema for $environment..."
    
    cd "$API_PROJECT"
    
    # Set environment
    export ASPNETCORE_ENVIRONMENT="$environment"
    
    # Check if database matches migrations
    if dotnet ef migrations has-pending-migrations; then
        print_warning "Database has pending migrations"
        dotnet ef migrations list
        return 1
    else
        print_success "Database schema is up to date"
        return 0
    fi
    
    cd - > /dev/null
}

# Generate database script
generate_script() {
    local from_migration=${1:-}
    local to_migration=${2:-}
    local output_file="$BACKUP_DIR/migration_script_$(date +%Y%m%d_%H%M%S).sql"
    
    print_status "Generating database migration script..."
    
    cd "$API_PROJECT"
    
    if [[ -n "$from_migration" && -n "$to_migration" ]]; then
        dotnet ef migrations script "$from_migration" "$to_migration" --output "$output_file"
        print_success "Migration script generated: $output_file (from $from_migration to $to_migration)"
    elif [[ -n "$from_migration" ]]; then
        dotnet ef migrations script "$from_migration" --output "$output_file"
        print_success "Migration script generated: $output_file (from $from_migration)"
    else
        dotnet ef migrations script --output "$output_file"
        print_success "Full migration script generated: $output_file"
    fi
    
    cd - > /dev/null
}

# Show database info
show_database_info() {
    local environment=${1:-Development}
    
    print_status "Database information for $environment environment:"
    
    cd "$API_PROJECT"
    
    # Set environment
    export ASPNETCORE_ENVIRONMENT="$environment"
    
    # Show connection string (masked)
    echo "Connection String: [MASKED FOR SECURITY]"
    
    # Show migrations
    echo ""
    echo "Applied Migrations:"
    dotnet ef migrations list 2>/dev/null || echo "Could not retrieve migration list"
    
    # Check pending migrations
    echo ""
    if dotnet ef migrations has-pending-migrations 2>/dev/null; then
        echo "Status: ⚠️  Pending migrations exist"
    else
        echo "Status: ✅ Database is up to date"
    fi
    
    cd - > /dev/null
}

# Cleanup old backups
cleanup_backups() {
    local days=${1:-30}
    
    print_status "Cleaning up backups older than $days days..."
    
    local deleted_count=$(find "$BACKUP_DIR" -name "backup_*.gz" -type f -mtime +$days -delete -print | wc -l)
    
    print_success "Cleaned up $deleted_count old backup files"
}

# Show help
show_help() {
    echo "Blood Thinner Tracker - Database Management Script"
    echo ""
    echo "⚠️  MEDICAL DATABASE OPERATIONS - USE WITH EXTREME CARE"
    echo ""
    echo "Usage: $0 [COMMAND] [OPTIONS]"
    echo ""
    echo "Migration Commands:"
    echo "  list                     List all migrations"
    echo "  create <name>            Create new migration"
    echo "  apply <env>              Apply pending migrations (dev/staging/production)"
    echo "  rollback <migration> <env>  Rollback to specific migration"
    echo "  remove                   Remove last migration"
    echo "  validate <env>           Check if database matches migrations"
    echo ""
    echo "Backup Commands:"
    echo "  backup-sqlite <env>      Backup SQLite database"
    echo "  backup-postgresql <env>  Backup PostgreSQL database"
    echo "  cleanup-backups <days>   Clean up old backups (default: 30 days)"
    echo ""
    echo "Utility Commands:"
    echo "  script [from] [to]       Generate migration script"
    echo "  info <env>              Show database information"
    echo "  help                    Show this help"
    echo ""
    echo "Environments: Development (default), Staging, Production"
    echo ""
    echo "Examples:"
    echo "  $0 create \"Add medication dosage tracking\""
    echo "  $0 apply Development"
    echo "  $0 backup-sqlite Production"
    echo "  $0 rollback InitialCreate Development"
    echo "  $0 script InitialCreate AddMedications"
    echo ""
    echo "Security Notes:"
    echo "- Always backup before migrations"
    echo "- Test migrations in Development first"
    echo "- Review generated migration code"
    echo "- Use encrypted backups for production"
    echo "- Follow medical data retention policies"
}

# Main execution
main() {
    local command=${1:-help}
    local arg1=${2:-}
    local arg2=${3:-}
    
    case $command in
        list)
            check_prerequisites
            list_migrations
            ;;
        create)
            if [[ -z "$arg1" ]]; then
                print_error "Migration name is required"
                exit 1
            fi
            check_prerequisites
            ensure_backup_directory
            create_migration "$arg1"
            ;;
        apply)
            local env=${arg1:-Development}
            check_prerequisites
            ensure_backup_directory
            apply_migrations "$env"
            ;;
        rollback)
            if [[ -z "$arg1" ]]; then
                print_error "Target migration is required"
                exit 1
            fi
            local env=${arg2:-Development}
            check_prerequisites
            ensure_backup_directory
            rollback_migration "$arg1" "$env"
            ;;
        remove)
            check_prerequisites
            remove_migration
            ;;
        validate)
            local env=${arg1:-Development}
            check_prerequisites
            validate_schema "$env"
            ;;
        backup-sqlite)
            local env=${arg1:-Development}
            check_prerequisites
            ensure_backup_directory
            backup_sqlite "$env"
            ;;
        backup-postgresql)
            local env=${arg1:-Development}
            check_prerequisites
            ensure_backup_directory
            backup_postgresql "$env"
            ;;
        script)
            check_prerequisites
            ensure_backup_directory
            generate_script "$arg1" "$arg2"
            ;;
        info)
            local env=${arg1:-Development}
            check_prerequisites
            show_database_info "$env"
            ;;
        cleanup-backups)
            local days=${arg1:-30}
            ensure_backup_directory
            cleanup_backups "$days"
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