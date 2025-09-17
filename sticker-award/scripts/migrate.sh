#!/bin/bash
# Copyright 2025 Datadog, Inc.
# SPDX-License-Identifier: Apache-2.0


# Database Migration Script for Sticker Award Service
# This script helps manage database migrations using golang-migrate

set -euo pipefail

# Configuration
DB_HOST=${DATABASE_HOST:-localhost}
DB_PORT=${DATABASE_PORT:-5432}
DB_USER=${DATABASE_USER:-sticker_user}
DB_PASSWORD=${DATABASE_PASSWORD:-sticker_password}
DB_NAME=${DATABASE_NAME:-sticker_awards}
DB_SSL_MODE=${DATABASE_SSL_MODE:-disable}

# Construct database URL
DATABASE_URL="postgres://${DB_USER}:${DB_PASSWORD}@${DB_HOST}:${DB_PORT}/${DB_NAME}?sslmode=${DB_SSL_MODE}"

# Migration directory
MIGRATIONS_DIR="./migrations"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if migrate command is available
check_migrate_installed() {
    if ! command -v migrate &> /dev/null; then
        log_error "golang-migrate is not installed"
        log_info "Install it using:"
        log_info "  macOS: brew install golang-migrate"
        log_info "  Linux: https://github.com/golang-migrate/migrate/releases"
        exit 1
    fi
}

# Show help
show_help() {
    echo "Database Migration Script for Sticker Award Service"
    echo ""
    echo "Usage: $0 [COMMAND] [OPTIONS]"
    echo ""
    echo "Commands:"
    echo "  up [N]       Apply all or N up migrations"
    echo "  down [N]     Apply all or N down migrations"
    echo "  version      Show current migration version"
    echo "  force VERSION Force migration to specific version"
    echo "  create NAME  Create new migration files"
    echo "  status       Show migration status"
    echo "  help         Show this help message"
    echo ""
    echo "Environment Variables:"
    echo "  DATABASE_HOST     Database host (default: localhost)"
    echo "  DATABASE_PORT     Database port (default: 5432)"
    echo "  DATABASE_USER     Database user (default: sticker_user)"
    echo "  DATABASE_PASSWORD Database password (default: sticker_password)"
    echo "  DATABASE_NAME     Database name (default: sticker_awards)"
    echo "  DATABASE_SSL_MODE SSL mode (default: disable)"
    echo ""
    echo "Examples:"
    echo "  $0 up                 # Apply all pending migrations"
    echo "  $0 up 1               # Apply next 1 migration"
    echo "  $0 down 1             # Rollback last 1 migration"
    echo "  $0 create add_indexes # Create new migration"
    echo "  $0 status             # Show current status"
}

# Apply up migrations
migrate_up() {
    local steps=${1:-""}
    log_info "Applying up migrations..."
    
    if [[ -n "$steps" ]]; then
        log_info "Applying $steps migration(s)"
        migrate -path "$MIGRATIONS_DIR" -database "$DATABASE_URL" up "$steps"
    else
        log_info "Applying all pending migrations"
        migrate -path "$MIGRATIONS_DIR" -database "$DATABASE_URL" up
    fi
    
    log_success "Migrations applied successfully"
}

# Apply down migrations
migrate_down() {
    local steps=${1:-""}
    log_warning "Rolling back migrations..."
    
    if [[ -n "$steps" ]]; then
        log_warning "Rolling back $steps migration(s)"
        migrate -path "$MIGRATIONS_DIR" -database "$DATABASE_URL" down "$steps"
    else
        log_warning "Rolling back ALL migrations (this will drop all tables!)"
        read -p "Are you sure? Type 'yes' to continue: " confirm
        if [[ "$confirm" == "yes" ]]; then
            migrate -path "$MIGRATIONS_DIR" -database "$DATABASE_URL" down
        else
            log_info "Operation cancelled"
            exit 0
        fi
    fi
    
    log_success "Rollback completed"
}

# Show current version
show_version() {
    log_info "Current migration version:"
    migrate -path "$MIGRATIONS_DIR" -database "$DATABASE_URL" version
}

# Force migration to specific version
force_version() {
    local version=${1:-""}
    if [[ -z "$version" ]]; then
        log_error "Version number required"
        exit 1
    fi
    
    log_warning "Forcing migration to version $version"
    migrate -path "$MIGRATIONS_DIR" -database "$DATABASE_URL" force "$version"
    log_success "Migration forced to version $version"
}

# Create new migration
create_migration() {
    local name=${1:-""}
    if [[ -z "$name" ]]; then
        log_error "Migration name required"
        exit 1
    fi
    
    log_info "Creating new migration: $name"
    migrate create -ext sql -dir "$MIGRATIONS_DIR" "$name"
    log_success "Migration files created"
}

# Show migration status
show_status() {
    log_info "Database connection: $DATABASE_URL"
    log_info "Migrations directory: $MIGRATIONS_DIR"
    log_info ""
    
    # Show current version
    show_version
    
    # List migration files
    log_info ""
    log_info "Available migrations:"
    ls -la "$MIGRATIONS_DIR"/*.sql 2>/dev/null || log_warning "No migration files found"
}

# Main script logic
main() {
    check_migrate_installed
    
    case "${1:-help}" in
        "up")
            migrate_up "${2:-}"
            ;;
        "down")
            migrate_down "${2:-}"
            ;;
        "version")
            show_version
            ;;
        "force")
            force_version "${2:-}"
            ;;
        "create")
            create_migration "${2:-}"
            ;;
        "status")
            show_status
            ;;
        "help"|*)
            show_help
            ;;
    esac
}

# Run main function with all arguments
main "$@"