#
# Default target - show help
#
.DEFAULT_GOAL := help

help:
	@echo "Stickerlandia Development Environment"
	@echo "===================================="
	@echo ""
	@echo "Main targets:"
	@echo "  compose-up      - Start all services in production mode"
	@echo "  compose-dev-up  - Start all services in development mode (with hot reloading)"
	@echo "  compose-down    - Stop and remove all containers and storage volumes"
	@echo ""
	@echo "Other targets:"
	@echo "  time-startup    - Time the entire startup process"
	@echo "  clean           - Remove generated files"
	@echo "  test-services   - Test that all services are responding"
	@echo "  wait-for-services - Wait for all services to be healthy"

#
# Licensing
#

.PHONY: LICENSE-3rdparty.csv

LICENSE-3rdparty.csv:
	dd-license-attribution https://github.com/DataDog/stickerlandia > LICENSE-3rdparty.csv

clean: 
	rm -f LICENSE-3rdparty.csv

# Validate source files license headers
verify-license-headers:
	docker run -it --rm -v $$(pwd):/github/workspace apache/skywalking-eyes header check

# Update source files license headers
update-license-headers:
	docker run -it --rm -v $$(pwd):/github/workspace apache/skywalking-eyes header fix

#
# Environment setup
#
.env:
	@echo "Creating .env file..."
	@echo "Please provide values for the following environment variables:"
	@echo "(Press Enter to use default values shown in brackets)"
	@echo ""
	@read -p "Datadog API Key: " DD_API_KEY; \
	read -p "Datadog Site [datadoghq.eu]: " DD_SITE; \
	DD_SITE=$${DD_SITE:-datadoghq.eu}; \
	read -p "Datadog RUM Application ID [default-rum-app-id]: " DD_RUM_APPLICATION_ID; \
	DD_RUM_APPLICATION_ID=$${DD_RUM_APPLICATION_ID:-default-rum-app-id}; \
	read -p "Datadog RUM Client Token [default-rum-client-token]: " DD_RUM_CLIENT_TOKEN; \
	DD_RUM_CLIENT_TOKEN=$${DD_RUM_CLIENT_TOKEN:-default-rum-client-token}; \
	read -p "Commit SHA [latest]: " COMMIT_SHA; \
	COMMIT_SHA=$${COMMIT_SHA:-latest}; \
	if [ -n "$$CODESPACE_NAME" ]; then \
		DEPLOYMENT_HOST_URL="https://$${CODESPACE_NAME}-8080.app.github.dev"; \
	else \
		DEPLOYMENT_HOST_URL="http://localhost:8080"; \
	fi; \
	echo "# Datadog Configuration" > .env; \
	echo "# ===================" >> .env; \
	echo "" >> .env; \
	echo "# API Key for Datadog Agent (required for metrics, logs, traces)" >> .env; \
	echo "DD_API_KEY=$$DD_API_KEY" >> .env; \
	echo "" >> .env; \
	echo "# RUM (Real User Monitoring) Configuration for Frontend" >> .env; \
	echo "DD_RUM_APPLICATION_ID=$$DD_RUM_APPLICATION_ID" >> .env; \
	echo "DD_RUM_CLIENT_TOKEN=$$DD_RUM_CLIENT_TOKEN" >> .env; \
	echo "" >> .env; \
	echo "# Datadog site (datadoghq.com for US, datadoghq.eu for EU, etc.)" >> .env; \
	echo "DD_SITE=$$DD_SITE" >> .env; \
	echo "" >> .env; \
	echo "# Commit SHA for version tracking" >> .env; \
	echo "COMMIT_SHA=$$COMMIT_SHA" >> .env; \
	echo "" >> .env; \
	echo "# Deployment Configuration" >> .env; \
	echo "# ========================" >> .env; \
	echo "" >> .env; \
	echo "# Base URL for the deployment (auto-detected based on environment)" >> .env; \
	echo "DEPLOYMENT_HOST_URL=$$DEPLOYMENT_HOST_URL" >> .env; \
	echo ""; \
	echo ".env file created successfully!"

#
# Compose environments
#
compose-up: .env
	docker compose --profile monitoring build && docker compose --profile monitoring up -d
	docker logs user-management

compose-dev-up: .env
	docker compose --profile monitoring -f docker-compose.yml -f docker-compose.dev.yml build && docker compose --profile monitoring -f docker-compose.yml -f docker-compose.dev.yml up -d
	
compose-up-ci: .env
	docker compose build && docker compose up -d
	docker logs user-management

compose-down:
	docker compose --profile monitoring down -v

time-startup: .env
	@echo "Pre-building images ... "
	@docker compose --profile monitoring build --quiet
	@echo "Shutting down any existing stacks ..."
	@docker compose --profile monitoring down -v --remove-orphans > /dev/null 2>&1
	@echo "Starting timer..."
	time (docker compose --profile monitoring up -d > /dev/null 2>&1 && \
	      ./scripts/wait-for-services.sh)
	
compose-time-startup: .env
	./scripts/time-docker-startup.sh docker-compose.yml

compose-time-dev-startup: .env
	./scripts/time-docker-startup.sh docker-compose.yml docker-compose.dev.yml

compose-time-builds: .env
	./scripts/time-docker-builds.sh

wait-for-services: .env
	./scripts/wait-for-services.sh

test-services: .env
	./scripts/test-services.sh --max-retries 5 --retry-delay 10

wait-for-services-dev: .env
	./scripts/wait-for-services.sh docker-compose.yml docker-compose.dev.yml
