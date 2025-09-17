.PHONY: run build docker-build test test-coverage fmt lint

# Default target
.DEFAULT_GOAL := help

# Binary name
BINARY_NAME := sticker-award

# Build version
VERSION := $(shell git describe --tags --always --dirty 2>/dev/null || echo "dev")
BUILD_TIME := $(shell date -u '+%Y-%m-%d_%H:%M:%S')
BUILD_COMMIT := $(shell git rev-parse --short HEAD 2>/dev/null || echo "unknown")

# Go build flags
LDFLAGS := -ldflags "-X main.Version=$(VERSION) -X main.BuildTime=$(BUILD_TIME) -X main.BuildCommit=$(BUILD_COMMIT)"

## run: Run the application locally
run:
	go run $(LDFLAGS) ./cmd/server

## build: Build the application binary
build:
	go build $(LDFLAGS) -o bin/$(BINARY_NAME) ./cmd/server

## docker-build: Build Docker image
docker-build:
	docker build -t $(BINARY_NAME):$(VERSION) .
	docker tag $(BINARY_NAME):$(VERSION) $(BINARY_NAME):latest

## test: Run unit tests only (excludes integration tests)
test:
	go test $$(go list ./... | grep -v /tests/)

## test-integration: Run integration tests only
test-integration:
	go test -tags=integration ./tests/...

## test-all: Run all tests (unit + integration)
test-all:
	go test -tags=integration ./...

## test-coverage: Run unit tests with coverage report
test-coverage:
	go test -coverprofile=coverage.out $$(go list ./... | grep -v /tests/)
	go tool cover -html=coverage.out -o coverage.html
	@echo "Unit test coverage report generated: coverage.html"

## test-coverage-integration: Run integration tests (no meaningful coverage)
test-coverage-integration:
	go test -tags=integration ./tests/... -v
	@echo "Integration tests completed (coverage not applicable for service-level tests)"

## test-coverage-all: Run all tests with coverage where possible
test-coverage-all:
	@echo "Running unit tests with coverage..."
	go test -coverprofile=coverage.out $$(go list ./... | grep -v /tests/)
	@echo "Running integration tests..."
	go test -tags=integration ./tests/... -v
	go tool cover -html=coverage.out -o coverage.html
	@echo "Coverage report (unit tests only): coverage.html"

## fmt: Format Go code
fmt:
	go fmt ./...
	goimports -w $$(find . -type f -name '*.go' -not -path "./vendor/*")

## lint: Run linter and check license headers
lint:
	golangci-lint run ./...
	addlicense -check -c "Datadog, Inc." -l apache -s=only -f ../LICENSE_HEADER.txt .

## clean: Clean build artifacts
clean:
	rm -rf bin/
	rm -f coverage.out coverage.html

## deps: Install dependencies
deps:
	go mod download
	go mod tidy

## dev-setup: Set up development environment
dev-setup: deps
	@command -v golangci-lint >/dev/null 2>&1 || { \
		echo "Installing golangci-lint..."; \
		go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest; \
	}
	@command -v goimports >/dev/null 2>&1 || { \
		echo "Installing goimports..."; \
		go install golang.org/x/tools/cmd/goimports@latest; \
	}
	@command -v addlicense >/dev/null 2>&1 || { \
		echo "Installing addlicense..."; \
		go install github.com/google/addlicense@latest; \
	}

## license-add: Add license headers to source files
license-add:
	addlicense -c "Datadog, Inc." -l apache -s=only -f ../LICENSE_HEADER.txt .

## docker-up: Start development environment with Docker Compose
docker-up:
	docker-compose up --build

## docker-down: Stop development environment
docker-down:
	docker-compose down

## help: Show this help message
help:
	@echo "Usage: make [target]"
	@echo ""
	@echo "Targets:"
	@sed -n 's/^##//p' $(MAKEFILE_LIST) | column -t -s ':' | sed -e 's/^/ /'