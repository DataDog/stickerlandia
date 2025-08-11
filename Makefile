#
# Licensing
#

.PHONY: LICENSE-3rdparty.csv

LICENSE-3rdparty.csv:
	dd-license-attribution https://github.com/DataDog/stickerlandia > LICENSE-3rdparty.csv

clean: 
	rm -f LICENSE-3rdparty.csv

#
# Compose environments
#

# Build and deploy entire stack on localhost:8080. Hot-reloading not supported.
compose-up:
	docker compose build && docker compose up -d
	docker logs user-management

# Build and deploy entire stack on localhost:8080. Hot reloading works for _most_ of 
# services - edit a file in the repository and it will be redeployed. At the time
# of writing, this isn't supported for web-frontend or web-backend.
compose-dev-up:
	docker compose -f docker-compose.yml -f docker-compose.dev.yml build && docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Shuts down the stack and removes storage volumes, cleaning all state from the app
compose-down:
	docker-compose down -v
	
compose-time-startup:
	./scripts/time-docker-startup.sh docker-compose.yml

compose-time-dev-startup:
	./scripts/time-docker-startup.sh docker-compose.yml docker-compose.dev.yml

compose-time-builds:
	./scripts/time-docker-builds.sh

wait-for-services:
	./scripts/wait-for-services.sh

test-services:
	./scripts/test-services.sh --max-retries 5 --retry-delay 10

wait-for-services-dev:
	./scripts/wait-for-services.sh docker-compose.yml docker-compose.dev.yml
