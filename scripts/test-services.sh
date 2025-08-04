#!/usr/bin/env bash

# Service Integration Testing Script
# Tests all service endpoints with retries and comprehensive logging
# Usage: ./scripts/test-services.sh [--max-retries N] [--retry-delay N]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default configuration
MAX_RETRIES=3
RETRY_DELAY=5
COMPOSE_FILE="docker-compose.yml"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --max-retries)
            MAX_RETRIES="$2"
            shift 2
            ;;
        --retry-delay)
            RETRY_DELAY="$2"
            shift 2
            ;;
        --compose-file)
            COMPOSE_FILE="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--max-retries N] [--retry-delay N] [--compose-file FILE]"
            exit 1
            ;;
    esac
done

echo -e "${BLUE}Service Integration Testing Script${NC}"
echo -e "${BLUE}=================================${NC}"
echo -e "Max retries: $MAX_RETRIES"
echo -e "Retry delay: ${RETRY_DELAY}s"
echo -e "Compose file: $COMPOSE_FILE"
echo ""

# Test configuration: URL, expected HTTP code, service name, description
declare -a TESTS=(
    "http://localhost:8081/dashboard/,200,traefik,Traefik Dashboard"
    "http://localhost:8082,200,redpanda-console,Redpanda Console"
    "http://localhost:8080/api/stickers/v1/sticker-001,200,sticker-catalogue,Sticker Catalogue API"
    "http://localhost:8080/api/awards/v1/assignments/user-001,200,sticker-award,Sticker Award API"
    "http://localhost:8080/api/users/v1/health,200,user-management,User Management Health"
)

# Function to test a single endpoint with retries
test_endpoint() {
    local url="$1"
    local expected_code="$2"
    local service_name="$3"
    local description="$4"
    local attempt=1
    
    echo -e "${BLUE}Testing: $description${NC}"
    echo -e "URL: $url"
    echo -e "Service: $service_name"
    echo -e "Expected: HTTP $expected_code"
    
    while [ $attempt -le $MAX_RETRIES ]; do
        echo -e "${CYAN}  Attempt $attempt/$MAX_RETRIES...${NC}"
        
        # Make the request and capture both status code and output
        local http_code
        local response
        
        if response=$(curl -s -w "%{http_code}" -o /tmp/curl_response.txt "$url" 2>/dev/null); then
            http_code="${response: -3}"
            response_body=$(cat /tmp/curl_response.txt 2>/dev/null || echo "")
            
            if [ "$http_code" = "$expected_code" ]; then
                echo -e "${GREEN}  ✓ Success: HTTP $http_code${NC}"
                rm -f /tmp/curl_response.txt
                return 0
            else
                echo -e "${YELLOW}  ⚠ Unexpected status: HTTP $http_code (expected $expected_code)${NC}"
                if [ -n "$response_body" ]; then
                    echo -e "${YELLOW}  Response: ${response_body:0:200}${NC}"
                fi
            fi
        else
            echo -e "${YELLOW}  ⚠ Request failed (connection error)${NC}"
        fi
        
        if [ $attempt -lt $MAX_RETRIES ]; then
            echo -e "${YELLOW}  Retrying in ${RETRY_DELAY}s...${NC}"
            sleep $RETRY_DELAY
        fi
        
        attempt=$((attempt + 1))
    done
    
    # All retries failed
    echo -e "${RED}  ✗ Failed after $MAX_RETRIES attempts${NC}"
    rm -f /tmp/curl_response.txt
    
    # Dump diagnostic information
    dump_failure_diagnostics "$service_name" "$url"
    
    return 1
}

# Function to dump comprehensive failure diagnostics
dump_failure_diagnostics() {
    local service_name="$1"
    local failed_url="$2"
    
    echo -e "${RED}=== FAILURE DIAGNOSTICS for $service_name ===${NC}"
    echo -e "${RED}Failed URL: $failed_url${NC}"
    echo ""
    
    echo -e "${YELLOW}--- Docker Compose Services Status ---${NC}"
    docker compose -f "$COMPOSE_FILE" ps || echo "Failed to get service status"
    echo ""
    
    echo -e "${YELLOW}--- Health Status Details ---${NC}"
    docker compose -f "$COMPOSE_FILE" ps --format "table {{.Name}}\t{{.Status}}\t{{.Health}}" || echo "Failed to get health status"
    echo ""
    
    echo -e "${YELLOW}--- Service Container Logs ($service_name) ---${NC}"
    if docker compose -f "$COMPOSE_FILE" logs --tail=100 "$service_name" 2>/dev/null; then
        echo "Successfully retrieved logs for $service_name"
    else
        echo "Failed to retrieve logs for $service_name, trying docker logs..."
        # Try to find container by service label
        local container_id
        container_id=$(docker ps -q --filter "label=com.docker.compose.service=$service_name" | head -1)
        if [ -n "$container_id" ]; then
            echo "Found container: $container_id"
            docker logs --tail=100 "$container_id" 2>&1 || echo "Failed to get container logs"
        else
            echo "No container found for service: $service_name"
        fi
    fi
    echo ""
    
    echo -e "${YELLOW}--- Related Service Dependencies ---${NC}"
    # Show logs for related services based on the failed service
    case "$service_name" in
        "sticker-catalogue")
            echo "Checking sticker-catalogue-db logs:"
            docker compose -f "$COMPOSE_FILE" logs --tail=50 sticker-catalogue-db 2>/dev/null || echo "Failed to get sticker-catalogue-db logs"
            echo "Checking minio logs:"
            docker compose -f "$COMPOSE_FILE" logs --tail=50 minio 2>/dev/null || echo "Failed to get minio logs"
            ;;
        "sticker-award")
            echo "Checking sticker-award-db logs:"
            docker compose -f "$COMPOSE_FILE" logs --tail=50 sticker-award-db 2>/dev/null || echo "Failed to get sticker-award-db logs"
            ;;
        "user-management")
            echo "Checking user-management-db logs:"
            docker compose -f "$COMPOSE_FILE" logs --tail=50 user-management-db 2>/dev/null || echo "Failed to get user-management-db logs"
            ;;
        "traefik")
            echo "Checking if traefik can reach backend services:"
            docker compose -f "$COMPOSE_FILE" logs --tail=50 traefik 2>/dev/null || echo "Failed to get traefik logs"
            ;;
    esac
    echo ""
    
    echo -e "${YELLOW}--- Resource Usage ---${NC}"
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}" 2>/dev/null || echo "Failed to get resource usage"
    echo ""
    
    echo -e "${YELLOW}--- Network Connectivity Test ---${NC}"
    # Test if we can reach the service internally
    local port
    case "$failed_url" in
        *:8080*) port="8080" ;;
        *:8081*) port="8081" ;;
        *:8082*) port="8082" ;;
        *) port="unknown" ;;
    esac
    
    if [ "$port" != "unknown" ]; then
        echo "Testing port $port connectivity:"
        if command -v nc >/dev/null 2>&1; then
            if nc -z localhost "$port" 2>/dev/null; then
                echo "  ✓ Port $port is reachable"
            else
                echo "  ✗ Port $port is not reachable"
            fi
        else
            echo "  netcat not available, skipping port test"
        fi
    fi
    echo ""
}

# Function to run all tests
run_all_tests() {
    local failed_tests=0
    local total_tests=${#TESTS[@]}
    
    echo -e "${BLUE}Running $total_tests endpoint tests...${NC}"
    echo ""
    
    for test_config in "${TESTS[@]}"; do
        # Parse test configuration
        IFS=',' read -r url expected_code service_name description <<< "$test_config"
        
        if test_endpoint "$url" "$expected_code" "$service_name" "$description"; then
            echo -e "${GREEN}PASS: $description${NC}"
        else
            echo -e "${RED}FAIL: $description${NC}"
            failed_tests=$((failed_tests + 1))
        fi
        echo ""
    done
    
    # Final summary
    echo -e "${BLUE}=== TEST SUMMARY ===${NC}"
    echo -e "Total tests: $total_tests"
    echo -e "${GREEN}Passed: $((total_tests - failed_tests))${NC}"
    echo -e "${RED}Failed: $failed_tests${NC}"
    
    if [ $failed_tests -eq 0 ]; then
        echo -e "${GREEN}All tests passed! 🎉${NC}"
        return 0
    else
        echo -e "${RED}$failed_tests test(s) failed ❌${NC}"
        return 1
    fi
}

run_all_tests
exit_code=$?

echo ""
echo -e "${BLUE}Service testing completed.${NC}"
exit $exit_code
