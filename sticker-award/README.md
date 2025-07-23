# Sticker Award Service

The Sticker Award Service manages sticker assignments to users in the Stickerlandia platform. It provides:

- **Assignment API** (`/api/awards/v1/assignments`) - User sticker assignment management (CRUD operations)  
- **Event Integration** - Listens for certification completions and publishes assignment events

## Architecture

### Technology Stack
- **Language**: Go 1.21+
- **HTTP Framework**: Gin (high performance REST API)
- **Database**: PostgreSQL with GORM ORM
- **Migrations**: golang-migrate with embedded SQL files
- **Configuration**: Viper (environment-based)
- **Logging**: Zap (structured JSON logging)
- **Messaging**: Kafka via IBM/sarama
- **Validation**: go-playground/validator

### Domain Structure
- **`internal/api/`** - HTTP handlers, middleware, DTOs, and routing
- **`internal/application/service/`** - Business logic and use cases
- **`internal/domain/`** - Core entities, repository interfaces, business rules
- **`internal/infrastructure/`** - Database access, external APIs, messaging
- **`internal/config/`** - Configuration management
- **`pkg/`** - Shared utilities (logger, errors, validator)

### Clean Architecture Layers
- **API Layer** - HTTP handlers, middleware, request/response DTOs
- **Application Layer** - Business workflows, external service coordination
- **Domain Layer** - Core business entities, repository interfaces, validation rules
- **Infrastructure Layer** - Database repositories, external clients, messaging

## API Endpoints

### Assignment API (`/api/awards/v1/assignments`)
- `GET /api/awards/v1/assignments/{userId}` - Get user's sticker assignments
- `POST /api/awards/v1/assignments/{userId}` - Assign a sticker to a user
- `DELETE /api/awards/v1/assignments/{userId}/{stickerId}` - Remove sticker assignment

### System Endpoints
- `GET /health` - Health check with database connectivity

## API Documentation

Full API documentation is available in OpenAPI format:
- Synchronous API: [api.yaml](./docs/api.yaml)
- Asynchronous API: [async_api.json](./docs/async_api.json)

## Building and Running

### Prerequisites
- Go 1.21+
- PostgreSQL 15+
- Docker & Docker Compose (for local development)

### Development

Run the full development stack:
```bash
make dev-run
# or
docker-compose up --build
```

Run locally (requires separate PostgreSQL):
```bash
make run
# or
go run ./cmd/server
```

### Testing

Run all tests:
```bash
make test
```

Run tests with coverage:
```bash
make test-coverage
```

### Building

Build the application:
```bash
make build
```

Build Docker image:
```bash
make docker-build
```

## Code Quality

### Formatting and Linting
```bash
# Format code
make fmt

# Run linter
make lint
```

### Database Migrations

Migrations run automatically on service startup. For manual migration management:
```bash
# Start only database
make db-up

# Reset database (WARNING: destroys data)
make db-reset
```

## Development Workflow

### Setup Development Environment
```bash
make dev-setup
```

### Common Commands
```bash
# Start development stack
make dev-run

# View logs
make dev-logs

# Stop services
make dev-stop

# Clean up resources
make docker-clean
```

## Configuration

The service is configured via environment variables:

### Server Configuration
- `SERVER_PORT` - HTTP server port (default: 8080)

### Database Configuration
- `DATABASE_HOST` - Database host (default: localhost)
- `DATABASE_PORT` - Database port (default: 5432)
- `DATABASE_USER` - Database user (default: sticker_user)
- `DATABASE_PASSWORD` - Database password
- `DATABASE_NAME` - Database name (default: sticker_awards)
- `DATABASE_SSL_MODE` - SSL mode (default: disable)

### External Services
- `STICKER_CATALOGUE_BASE_URL` - Catalogue service URL
- `KAFKA_BROKERS` - Kafka broker addresses

### Logging
- `LOG_LEVEL` - Log level (debug, info, warn, error)
- `LOG_FORMAT` - Log format (json, console)
