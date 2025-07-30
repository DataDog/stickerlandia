#!/usr/bin/env bash

# Docker Build Timing Script
# Usage: ./scripts/time-docker-builds.sh [docker-compose-file]
# Default: docker-compose.yml

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
COMPOSE_FILE=${1:-"docker-compose.yml"}
RESULTS_FILE="build-timing-results.txt"
TEMP_LOG="build-temp.log"

echo -e "${BLUE}Docker Build Timing Script${NC}"
echo -e "${BLUE}===========================${NC}"
echo ""

# Check if docker-compose file exists
if [ ! -f "$COMPOSE_FILE" ]; then
    echo -e "${RED}Error: Docker Compose file '$COMPOSE_FILE' not found${NC}"
    exit 1
fi

# Clear previous results
> "$RESULTS_FILE"
> "$TEMP_LOG"

# echo -e "${YELLOW}Step 1: Cleaning Docker system...${NC}"
# echo "Removing all containers, images, and build cache..."

# # Stop all containers
# docker stop $(docker ps -aq) 2>/dev/null || true

# # Remove all containers
# docker rm $(docker ps -aq) 2>/dev/null || true

# # Remove all images
# docker rmi $(docker images -q) 2>/dev/null || true

# # Remove all volumes
# docker volume rm $(docker volume ls -q) 2>/dev/null || true

# # Remove all networks (except default ones)
# docker network rm $(docker network ls -q --filter type=custom) 2>/dev/null || true

# # Clean build cache
# docker builder prune -a -f

# # Clean system
# docker system prune -a -f --volumes

# echo -e "${GREEN}✓ Docker system cleaned${NC}"
# echo ""

echo -e "${YELLOW}Step 2: Extracting services from $COMPOSE_FILE...${NC}"

# Extract service names that have a 'build' section
SERVICES=$(docker compose -f "$COMPOSE_FILE" config --services | while read service; do
    if docker compose -f "$COMPOSE_FILE" config | grep -A 10 "^  $service:" | grep -q "build:"; then
        echo "$service"
    fi
done)

if [ -z "$SERVICES" ]; then
    echo -e "${RED}Error: No services with build configuration found in $COMPOSE_FILE${NC}"
    exit 1
fi

echo "Found services with build configurations:"
for service in $SERVICES; do
    echo "  - $service"
done
echo ""

echo -e "${YELLOW}Step 3: Building services and measuring time...${NC}"
echo ""

# Arrays to store results
declare -A build_times
declare -A build_status

# Build each service individually and measure time
for service in $SERVICES; do
    echo -e "${BLUE}Building service: $service${NC}"
    echo "----------------------------------------"
    
    # Record start time
    start_time=$(date +%s.%N)
    
    # Build the service and capture output
    if docker compose -f "$COMPOSE_FILE" build --no-cache "$service" > "$TEMP_LOG" 2>&1; then
        # Record end time
        end_time=$(date +%s.%N)
        
        # Calculate duration
        duration=$(echo "$end_time - $start_time" | bc -l)
        build_times["$service"]=$(printf "%.2f" $duration)
        build_status["$service"]="SUCCESS"
        
        echo -e "${GREEN}✓ $service built successfully in ${build_times[$service]}s${NC}"
    else
        # Record end time even for failures
        end_time=$(date +%s.%N)
        duration=$(echo "$end_time - $start_time" | bc -l)
        build_times["$service"]=$(printf "%.2f" $duration)
        build_status["$service"]="FAILED"
        
        echo -e "${RED}✗ $service build failed after ${build_times[$service]}s${NC}"
        echo -e "${RED}Error output:${NC}"
        tail -10 "$TEMP_LOG"
    fi
    
    echo ""
done

echo -e "${YELLOW}Step 4: Generating results...${NC}"
echo ""

# Write results to file
echo "Docker Build Timing Results" > "$RESULTS_FILE"
echo "==========================" >> "$RESULTS_FILE"
echo "Compose file: $COMPOSE_FILE" >> "$RESULTS_FILE"
echo "Timestamp: $(date)" >> "$RESULTS_FILE"
echo "" >> "$RESULTS_FILE"

# Calculate total time
total_time=0
success_count=0
failure_count=0

for service in $SERVICES; do
    if [ "${build_status[$service]}" = "SUCCESS" ]; then
        success_count=$((success_count + 1))
    else
        failure_count=$((failure_count + 1))
    fi
    total_time=$(echo "$total_time + ${build_times[$service]}" | bc -l)
done

# Display results table
echo -e "${BLUE}Build Results Summary${NC}"
echo -e "${BLUE}=====================${NC}"
printf "%-30s %-10s %-10s\n" "Service" "Status" "Time (s)"
printf "%-30s %-10s %-10s\n" "-------" "------" "--------"

{
    echo "Build Results Summary"
    echo "====================="
    printf "%-30s %-10s %-10s\n" "Service" "Status" "Time (s)"
    printf "%-30s %-10s %-10s\n" "-------" "------" "--------"
} >> "$RESULTS_FILE"

for service in $SERVICES; do
    status="${build_status[$service]}"
    time="${build_times[$service]}"
    
    if [ "$status" = "SUCCESS" ]; then
        printf "${GREEN}%-30s %-10s %-10s${NC}\n" "$service" "$status" "$time"
    else
        printf "${RED}%-30s %-10s %-10s${NC}\n" "$service" "$status" "$time"
    fi
    
    printf "%-30s %-10s %-10s\n" "$service" "$status" "$time" >> "$RESULTS_FILE"
done

echo ""
{
    echo ""
    echo "Summary Statistics"
    echo "=================="
} >> "$RESULTS_FILE"

printf "${BLUE}%-20s: %d${NC}\n" "Total services" $(echo "$SERVICES" | wc -w)
printf "${GREEN}%-20s: %d${NC}\n" "Successful builds" $success_count
printf "${RED}%-20s: %d${NC}\n" "Failed builds" $failure_count
printf "${YELLOW}%-20s: %.2f seconds${NC}\n" "Total build time" $(printf "%.2f" $total_time)

{
    printf "%-20s: %d\n" "Total services" $(echo "$SERVICES" | wc -w)
    printf "%-20s: %d\n" "Successful builds" $success_count
    printf "%-20s: %d\n" "Failed builds" $failure_count
    printf "%-20s: %.2f seconds\n" "Total build time" $(printf "%.2f" $total_time)
} >> "$RESULTS_FILE"

echo ""
echo -e "${BLUE}Results saved to: $RESULTS_FILE${NC}"

# Cleanup temp files
rm -f "$TEMP_LOG"

# Exit with error code if any builds failed
if [ $failure_count -gt 0 ]; then
    echo -e "${RED}Warning: $failure_count build(s) failed${NC}"
    exit 1
else
    echo -e "${GREEN}All builds completed successfully!${NC}"
    exit 0
fi
