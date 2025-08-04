#!/usr/bin/env bash

# Docker Startup Timing Script
# Usage: ./scripts/time-docker-startup.sh [--no-shutdown] [docker-compose-file]
# Default: docker-compose.yml
# Options:
#   --no-shutdown    Leave containers running after timing test

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Parse arguments
NO_SHUTDOWN=false
COMPOSE_FILE="docker-compose.yml"

while [[ $# -gt 0 ]]; do
    case $1 in
        --no-shutdown)
            NO_SHUTDOWN=true
            shift
            ;;
        *)
            COMPOSE_FILE="$1"
            shift
            ;;
    esac
done
TEMP_LOG="startup-temp.log"
MAX_WAIT_TIME=300  # 5 minutes max wait per service

echo -e "${BLUE}Docker Startup Timing Script${NC}"
echo -e "${BLUE}=============================${NC}"
echo ""

# Check if docker-compose file exists
if [ ! -f "$COMPOSE_FILE" ]; then
    echo -e "${RED}Error: Docker Compose file '$COMPOSE_FILE' not found${NC}"
    exit 1
fi

# Clear previous results
> "$TEMP_LOG"

echo -e "${YELLOW}Step 1: Stopping existing containers...${NC}"
docker compose -f "$COMPOSE_FILE" down > /dev/null 2>&1 || true
echo -e "${GREEN}✓ Containers stopped${NC}"
echo ""

echo -e "${YELLOW}Step 2: Extracting services with healthchecks...${NC}"

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
    echo "Debug: Let me show you what docker-compose config shows:"
    docker compose -f "$COMPOSE_FILE" config | grep -E "^  [a-zA-Z0-9_-]+:$|^    healthcheck:$" | head -20
    exit 1
fi

echo "Found services with healthcheck configurations:"
for service in $SERVICES; do
    echo "  - $service"
done
echo ""

# Arrays to store results
declare -A startup_times
declare -A startup_status

# Function to check if service is healthy
check_service_health() {
    local service=$1
    local container_name=$(docker compose -f "$COMPOSE_FILE" ps -q "$service" 2>/dev/null)
    
    if [ -z "$container_name" ]; then
        return 1  # Container not found
    fi
    
    local health_status=$(docker inspect --format='{{.State.Health.Status}}' "$container_name" 2>/dev/null || echo "none")
    
    case "$health_status" in
        "healthy")
            return 0  # Healthy
            ;;
        "unhealthy")
            return 2  # Unhealthy
            ;;
        "starting"|"none")
            return 1  # Still starting or no healthcheck
            ;;
        *)
            return 1  # Unknown status
            ;;
    esac
}

# Function to get dependency services
get_dependencies() {
    local service=$1
    docker compose -f "$COMPOSE_FILE" config | awk -v svc="$service" '
    /^  [a-zA-Z0-9_-]+:$/ { 
        current_service = substr($1, 1, length($1)-1)
    }
    current_service == svc && /^    depends_on:$/ {
        in_depends = 1
        next
    }
    in_depends && /^      [a-zA-Z0-9_-]+:$/ {
        dep = substr($2, 1, length($2)-1)
        print dep
    }
    in_depends && /^      - [a-zA-Z0-9_-]+$/ {
        dep = substr($2, 3)
        print dep
    }
    in_depends && /^    [a-zA-Z]/ && $0 !~ /^      / {
        in_depends = 0
    }
    '
}

echo -e "${YELLOW}Step 3: Starting dependencies and timing service startup...${NC}"
echo ""

# Start all dependencies first (but don't start services we're testing)
echo -e "${CYAN}Starting all dependencies...${NC}"
ALL_DEPS=""
for service in $SERVICES; do
    deps=$(get_dependencies "$service")
    for dep in $deps; do
        # Only add if it's not already in the list and not a service we're testing
        if [[ ! " $ALL_DEPS " =~ " $dep " ]] && [[ ! " $SERVICES " =~ " $dep " ]]; then
            ALL_DEPS="$ALL_DEPS $dep"
        fi
    done
done

if [ -n "$ALL_DEPS" ]; then
    echo "Dependencies to start: $ALL_DEPS"
    docker compose -f "$COMPOSE_FILE" up -d $ALL_DEPS > "$TEMP_LOG" 2>&1
    
    # Wait for dependencies to be healthy
    echo "Waiting for dependencies to become healthy..."
    for dep in $ALL_DEPS; do
        echo "  $dep (no healthcheck - waiting 10s for startup)"
        sleep 5
    done
    echo ""
else
    echo "No external dependencies found - services will be tested independently"
    echo ""
fi

# Now test each service individually
for service in $SERVICES; do
    echo -e "${BLUE}Testing service: $service${NC}"
    echo "----------------------------------------"
    
    # Stop the service if it's running
    docker compose -f "$COMPOSE_FILE" stop "$service" > /dev/null 2>&1 || true
    docker compose -f "$COMPOSE_FILE" rm -f "$service" > /dev/null 2>&1 || true
    
    # Record start time
    start_time=$(date +%s.%N)
    
    # Start the service
    echo "Starting $service..."
    if docker compose -f "$COMPOSE_FILE" up -d "$service" > "$TEMP_LOG" 2>&1; then
        
        # Give container a moment to appear in ps output
        sleep 2
        
        # Wait for service to become healthy
        echo "Waiting for $service to become healthy... "
        wait_time=0
        is_healthy=false
        
        while [ $wait_time -lt $MAX_WAIT_TIME ]; do
            container_name=$(docker compose -f "$COMPOSE_FILE" ps -q "$service" 2>/dev/null)
            
            # If container_name is empty, try alternative approaches
            if [ -z "$container_name" ]; then
                # Try getting container by service name pattern
                container_name=$(docker ps -q --filter "label=com.docker.compose.service=$service" 2>/dev/null | head -1)
            fi
            
            health_status=$(docker inspect --format='{{.State.Health.Status}}' "$container_name" 2>/dev/null || echo "none")
            
            echo "  [$wait_time s] Health status: $health_status (container: $container_name)"
            
            # Simplified health check logic
            case "$health_status" in
                "healthy")
                    health_exit_code=0
                    ;;
                "unhealthy")
                    health_exit_code=2
                    ;;
                *)
                    health_exit_code=1
                    ;;
            esac
            
            case $health_exit_code in
                0)  # Healthy
                    end_time=$(date +%s.%N)
                    duration=$(echo "$end_time - $start_time" | bc -l)
                    startup_times["$service"]=$(printf "%.2f" $duration)
                    startup_status["$service"]="HEALTHY"
                    is_healthy=true
                    echo -e "${GREEN}✓ $service became healthy in ${startup_times[$service]}s${NC}"
                    break
                    ;;
                2)  # Unhealthy
                    end_time=$(date +%s.%N)
                    duration=$(echo "$end_time - $start_time" | bc -l)
                    startup_times["$service"]=$(printf "%.2f" $duration)
                    startup_status["$service"]="UNHEALTHY"
                    echo -e "${RED}✗ $service became unhealthy after ${startup_times[$service]}s${NC}"
                    echo -e "${RED}Container logs:${NC}"
                    docker logs "$container_name" --tail 50 2>&1 || echo "Failed to get logs"
                    break
                    ;;
                *)  # Still starting
                    sleep 2
                    wait_time=$((wait_time + 5))
                    ;;
            esac
        done
        
        if [ "$is_healthy" = false ] && [ "${startup_status[$service]}" != "UNHEALTHY" ]; then
            end_time=$(date +%s.%N)
            duration=$(echo "$end_time - $start_time" | bc -l)
            startup_times["$service"]=$(printf "%.2f" $duration)
            startup_status["$service"]="TIMEOUT"
            echo -e "${RED}timeout after ${startup_times[$service]}s${NC}"
            if [ -n "$container_name" ]; then
                echo -e "${RED}Container logs:${NC}"
                docker logs "$container_name" --tail 50 2>&1 || echo "Failed to get logs"
            fi
        fi
        
    else
        end_time=$(date +%s.%N)
        duration=$(echo "$end_time - $start_time" | bc -l)
        startup_times["$service"]=$(printf "%.2f" $duration)
        startup_status["$service"]="FAILED"
        echo -e "${RED}failed to start after ${startup_times[$service]}s${NC}"
        echo -e "${RED}Error output:${NC}"
        tail -5 "$TEMP_LOG"
    fi
    
    echo ""
done

echo -e "${YELLOW}Step 4: Generating results...${NC}"
echo ""

# Calculate statistics
total_time=0
healthy_count=0
unhealthy_count=0
timeout_count=0
failed_count=0

for service in $SERVICES; do
    case "${startup_status[$service]}" in
        "HEALTHY") healthy_count=$((healthy_count + 1)) ;;
        "UNHEALTHY") unhealthy_count=$((unhealthy_count + 1)) ;;
        "TIMEOUT") timeout_count=$((timeout_count + 1)) ;;
        "FAILED") failed_count=$((failed_count + 1)) ;;
    esac
    total_time=$(echo "$total_time + ${startup_times[$service]}" | bc -l)
done

# Display results table
echo -e "${BLUE}Startup Results Summary${NC}"
echo -e "${BLUE}=======================${NC}"
printf "%-30s %-12s %-10s\n" "Service" "Status" "Time (s)"
printf "%-30s %-12s %-10s\n" "-------" "------" "--------"

for service in $SERVICES; do
    status="${startup_status[$service]}"
    time="${startup_times[$service]}"
    
    case "$status" in
        "HEALTHY")
            printf "${GREEN}%-30s %-12s %-10s${NC}\n" "$service" "$status" "$time"
            ;;
        "UNHEALTHY")
            printf "${RED}%-30s %-12s %-10s${NC}\n" "$service" "$status" "$time"
            ;;
        "TIMEOUT")
            printf "${YELLOW}%-30s %-12s %-10s${NC}\n" "$service" "$status" "$time"
            ;;
        "FAILED")
            printf "${RED}%-30s %-12s %-10s${NC}\n" "$service" "$status" "$time"
            ;;
    esac
   
done

printf "${BLUE}%-20s: %d${NC}\n" "Total services" $(echo "$SERVICES" | wc -w)
printf "${GREEN}%-20s: %d${NC}\n" "Healthy" $healthy_count
printf "${RED}%-20s: %d${NC}\n" "Unhealthy" $unhealthy_count
printf "${YELLOW}%-20s: %d${NC}\n" "Timeouts" $timeout_count
printf "${RED}%-20s: %d${NC}\n" "Failed to start" $failed_count
printf "${CYAN}%-20s: %.2f seconds${NC}\n" "Total startup time" $(printf "%.2f" $total_time)

echo ""

# Cleanup
echo ""
if [ "$NO_SHUTDOWN" = true ]; then
    echo -e "${YELLOW}Leaving containers running (--no-shutdown specified)${NC}"
else
    echo -e "${YELLOW}Cleaning up...${NC}"
    docker compose -f "$COMPOSE_FILE" down > /dev/null 2>&1
fi
rm -f "$TEMP_LOG"

# Exit with appropriate code
if [ $failed_count -gt 0 ] || [ $unhealthy_count -gt 0 ]; then
    echo -e "${RED}Warning: Some services failed to start or became unhealthy${NC}"
    exit 1
elif [ $timeout_count -gt 0 ]; then
    echo -e "${YELLOW}Warning: Some services timed out during startup${NC}"
    exit 2
else
    echo -e "${GREEN}All services started successfully and became healthy!${NC}"
    exit 0
fi
