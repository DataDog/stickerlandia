#!/usr/bin/env bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
# This product includes software developed at Datadog (https://www.datadoghq.com/).
# Copyright 2025-Present Datadog, Inc.


# Wait for Services Script
# Monitors services with health checks and displays real-time status table
# Usage: ./scripts/wait-for-services.sh [--timeout N] [docker-compose-file]
# Default: docker-compose.yml, timeout 90s

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default configuration
TIMEOUT=90
COMPOSE_FILE="docker-compose.yml"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        *)
            COMPOSE_FILE="$1"
            shift
            ;;
    esac
done

echo -e "${BLUE}Waiting for Services to be Ready${NC}"
echo -e "${BLUE}================================${NC}"
echo -e "Compose file: $COMPOSE_FILE"
echo -e "Timeout: ${TIMEOUT}s"
echo ""

# Check if docker-compose file exists
if [ ! -f "$COMPOSE_FILE" ]; then
    echo -e "${RED}Error: Docker Compose file '$COMPOSE_FILE' not found${NC}"
    exit 1
fi

# Extract services that have healthcheck configurations
SERVICES=$(docker compose -f "$COMPOSE_FILE" config | awk '
/^  [a-zA-Z0-9_-]+:$/ { 
    service = substr($1, 1, length($1)-1)
}
/^    healthcheck:$/ {
    if (service != "") {
        print service
        service = ""
    }
}
')

if [ -z "$SERVICES" ]; then
    echo -e "${RED}Error: No services with healthcheck configuration found in $COMPOSE_FILE${NC}"
    exit 1
fi

# Convert services to array
SERVICE_ARRAY=($SERVICES)
SERVICE_COUNT=${#SERVICE_ARRAY[@]}

echo -e "${YELLOW}Monitoring $SERVICE_COUNT services with health checks:${NC}"
for service in "${SERVICE_ARRAY[@]}"; do
    echo -e "  - $service"
done
echo ""

# Function to get service health status
get_service_status() {
    local service=$1
    local container_name
    
    # Try to get container name
    container_name=$(docker compose -f "$COMPOSE_FILE" ps -q "$service" 2>/dev/null)
    
    # If container_name is empty, try alternative approach
    if [ -z "$container_name" ]; then
        container_name=$(docker ps -q --filter "label=com.docker.compose.service=$service" 2>/dev/null | head -1)
    fi
    
    # If still no container, service is not running
    if [ -z "$container_name" ]; then
        echo "NOT_RUNNING"
        return
    fi
    
    # Get health status
    local health_status
    health_status=$(docker inspect --format='{{.State.Health.Status}}' "$container_name" 2>/dev/null || echo "none")
    
    case "$health_status" in
        "healthy")
            echo "HEALTHY"
            ;;
        "unhealthy")
            echo "UNHEALTHY"
            ;;
        "starting")
            echo "STARTING"
            ;;
        "none")
            # Check if container is running but has no health check
            local container_state
            container_state=$(docker inspect --format='{{.State.Status}}' "$container_name" 2>/dev/null || echo "unknown")
            case "$container_state" in
                "running")
                    echo "NO_HEALTHCHECK"
                    ;;
                *)
                    echo "NOT_RUNNING"
                    ;;
            esac
            ;;
        *)
            echo "UNKNOWN"
            ;;
    esac
}

# Function to get color for status
get_status_color() {
    local status=$1
    case "$status" in
        "HEALTHY")
            echo "$GREEN"
            ;;
        "UNHEALTHY")
            echo "$RED"
            ;;
        "STARTING")
            echo "$YELLOW"
            ;;
        "NOT_RUNNING")
            echo "$RED"
            ;;
        "NO_HEALTHCHECK")
            echo "$CYAN"
            ;;
        *)
            echo "$NC"
            ;;
    esac
}

# Function to check if all services are ready
all_services_ready() {
    for service in "${SERVICE_ARRAY[@]}"; do
        local status
        status=$(get_service_status "$service")
        if [[ "$status" != "HEALTHY" ]]; then
            return 1
        fi
    done
    return 0
}

# Function to print status table
print_status_table() {
    local elapsed=$1
    
    # Clear screen and move cursor to top
    printf "\033[2J\033[H"
    
    echo -e "${BLUE}Waiting for Services to be Ready${NC}"
    echo -e "${BLUE}================================${NC}"
    echo -e "Compose file: $COMPOSE_FILE"
    echo -e "Timeout: ${TIMEOUT}s | Elapsed: ${elapsed}s"
    echo ""
    
    # Print table header
    printf "%-30s %-15s\n" "Service" "Status"
    printf "%-30s %-15s\n" "-------" "------"
    
    # Print each service status
    for service in "${SERVICE_ARRAY[@]}"; do
        local status
        status=$(get_service_status "$service")
        local color
        color=$(get_status_color "$status")
        printf "${color}%-30s %-15s${NC}\n" "$service" "$status"
    done
    
    echo ""
    
    # Print summary
    local healthy_count=0
    local total_count=${#SERVICE_ARRAY[@]}
    
    for service in "${SERVICE_ARRAY[@]}"; do
        local status
        status=$(get_service_status "$service")
        if [[ "$status" == "HEALTHY" ]]; then
            healthy_count=$((healthy_count + 1))
        fi
    done
    
    echo -e "Progress: ${GREEN}${healthy_count}${NC}/${total_count} services healthy"
    
    if [[ $healthy_count -eq $total_count ]]; then
        echo -e "${GREEN}✓ All services are healthy!${NC}"
    else
        echo -e "${YELLOW}⏳ Waiting for services to become healthy...${NC}"
    fi
}

# Main monitoring loop
start_time=$(date +%s)
elapsed=0

# Initial table print
print_status_table $elapsed

while [[ $elapsed -lt $TIMEOUT ]]; do
    # Check if all services are ready
    if all_services_ready; then
        echo ""
        echo -e "${GREEN}🎉 All services are healthy after ${elapsed}s!${NC}"
        exit 0
    fi
    
    # Wait 1 second
    sleep 1
    
    # Update elapsed time
    current_time=$(date +%s)
    elapsed=$((current_time - start_time))
    
    # Print updated table
    print_status_table $elapsed
done

# Timeout reached
echo ""
echo -e "${RED}❌ Timeout reached (${TIMEOUT}s) - not all services became healthy${NC}"

# Print final status summary
echo ""
echo -e "${YELLOW}Final Status Summary:${NC}"
printf "%-30s %-15s\n" "Service" "Status"
printf "%-30s %-15s\n" "-------" "------"

for service in "${SERVICE_ARRAY[@]}"; do
    local status
    status=$(get_service_status "$service")
    local color
    color=$(get_status_color "$status")
    printf "${color}%-30s %-15s${NC}\n" "$service" "$status"
done

exit 1