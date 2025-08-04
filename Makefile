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
compose-up:
	docker compose build && docker compose up -d

compose-dev-up:
	docker compose -f docker-compose.yml -f docker-compose.dev.yml build && docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d
	
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
