# Sticker Award Service

The Sticker Award Service manages sticker assignments to users in the Stickerlandia platform. It provides:

- **Assignment API** (`/api/awards/v1/assignments`) - User sticker assignment management (CRUD operations)  
- **Event Integration** - Publishes sticker assignment events to Kafka for downstream services

## Architecture

### Technology Stack
- **Language**: Go 1.23+
- **HTTP Framework**: Gin (high performance REST API)
- **Database**: PostgreSQL with GORM ORM
- **Migrations**: golang-migrate with embedded SQL files
- **Configuration**: Viper (environment-based)
- **Logging**: Zap (structured JSON logging)
- **Messaging**: Kafka via segmentio/kafka-go
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
- Go 1.23+
- PostgreSQL 15+
- Apache Kafka (for event publishing)
- Docker & Docker Compose (for local development)

### Development

Run the full development stack:
```bash
docker-compose up --build
```

Run locally (requires separate PostgreSQL):
```bash
make run
# or
go run ./cmd/server
```

### Testing

Run all unit tests:
```bash
make test
```

Run all tests, including integration tests:
```bash
make test-integration
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

### Kafka Configuration
- `KAFKA_BROKERS` - Kafka broker addresses (comma-separated)
- `KAFKA_PRODUCER_TIMEOUT` - Producer timeout in milliseconds (default: 5000)
- `KAFKA_PRODUCER_RETRIES` - Number of retry attempts (default: 3)
- `KAFKA_PRODUCER_BATCH_SIZE` - Batch size in bytes (default: 16384)
- `KAFKA_REQUIRE_ACKS` - Acknowledgment level (default: 1)
- `KAFKA_ENABLE_IDEMPOTENT` - Enable idempotent producer (default: true)

### Logging
- `LOG_LEVEL` - Log level (debug, info, warn, error)
- `LOG_FORMAT` - Log format (json, console)

# Deployment

The sticker award service can be deployed to AWS, Azure & GCP. For deployment instructions see cloud provider specific instructions below:

## AWS

AWS deployment uses the AWS CDK. Inside the CDK code, there is the concept of an 'integrated' (dev, prod) and 'non-integrated' environment. For developing a development instance of the sticker award service you'll first need to copy some parameters inside AWS, and then deploy using the below commands.

### Parameters

The service expects SSM parameters named:

- /stickerlandia/<ENV>/sticker-award/database-host
- /stickerlandia/<ENV>/sticker-award/database-name
- /stickerlandia/<ENV>/sticker-award/database-user
- /stickerlandia/<ENV>/sticker-award/database-password
- /stickerlandia/<ENV>/sticker-award/kafka-broker
- /stickerlandia/<ENV>/sticker-award/kafka-username
- /stickerlandia/<ENV>/sticker-award/kafka-password

You will need to create those before running the deploy commands below.

### Deployment

```sh
export ENV= # The environment name to use, don't use 'dev' or 'prod'. Your initials is normally a good start.
export VERSION= # The commit hash you want to use, defaults to latest
export DD_API_KEY= # The Datadog API key for your org
export DD_SITE = # The Datadog site to use
cd infra/aws
cdk deploy
```