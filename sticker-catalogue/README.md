# Sticker Catalogue Service

The Sticker Catalogue Service manages stickers in the Stickerlandia platform. It stores both relational data
about the stickers, and the images of the stickers themselves.

- **Catalog API** (`/api/stickers/v1`) - Manages the sticker catalog (metadata, images, CRUD)

## Architecture

### Domain Structure

- **`sticker/`** - Sticker catalog domain (`/api/stickers/v1`)
  - `StickerResource.java` - HTTP API for sticker catalog
  - `StickerRepository.java` - Data access and entity-DTO mapping
  - `dto/` - Request/Response DTOs (CreateStickerRequest, StickerDTO, etc.)
  - `entity/` - Database entities (Sticker)

- **`common/`** - Shared utilities
  - `dto/` - Common DTOs (PagedResponse)
  - `events/` - Domain events

### Separation of Concerns
- **Resource** - HTTP layer, handles requests/responses, only works with DTOs
- **Repository** - Data layer, maps between entities and DTOs, contains business logic
- **Entity** - Database layer, JPA entities for persistence
- **DTO** - API layer, request/response objects for HTTP APIs

## API Endpoints

### Catalog API (`/api/stickers/v1`)
- `GET /api/stickers/v1` - List all stickers (paginated)
- `POST /api/stickers/v1` - Create new sticker
- `GET /api/stickers/v1/{stickerId}` - Get sticker metadata
- `PUT /api/stickers/v1/{stickerId}` - Update sticker metadata
- `DELETE /api/stickers/v1/{stickerId}` - Delete sticker
- `GET /api/stickers/v1/{stickerId}/image` - Get sticker image
- `PUT /api/stickers/v1/{stickerId}/image` - Upload/update sticker image

## Authentication

All API endpoints (except `/health`) require authentication via JWT token in the Authorization header. 

## Error Handling

The API returns standard HTTP status codes and follows the RFC 7807 Problem Details specification for error responses.

## API Documentation

Full API documentation is available in OpenAPI format:
- Synchronous API: [api.yaml](./docs/api.yaml)
- Asynchronous API: [async_api.json](./docs/async_api.json)

## Environment Configuration

This service uses Quarkus profiles to manage configuration across different environments. Profiles are selected via the `QUARKUS_PROFILE` environment variable.

### Available Profiles

| Profile | Purpose | Messaging | Activation |
|---------|---------|-----------|------------|
| `dev` | Local development with DevServices | Kafka  | `./mvnw quarkus:dev` (automatic) |
| `prod` | Base production (not used directly) | None | - |
| `prod-kafka` | Production with Kafka messaging | Kafka | `QUARKUS_PROFILE=prod-kafka` |
| `prod-aws` | Production with AWS EventBridge | EventBridge | `QUARKUS_PROFILE=prod-aws` |

### Common Environment Variables

These variables must be provided in all production profiles (`prod-kafka`, `prod-aws`):

| Variable | Purpose                                       | Example |
|----------|-----------------------------------------------|---------|
| `QUARKUS_DATASOURCE_JDBC_URL` | PostgreSQL connection URL                     | `jdbc:postgresql://db:5432/sticker_catalogue` |
| `QUARKUS_DATASOURCE_USERNAME` | Database username                             | `sticker_user` |
| `QUARKUS_DATASOURCE_PASSWORD` | Database password                             | `secret` |
| `QUARKUS_S3_ENDPOINT_OVERRIDE` | (optional) S3 endpoint (for MinIO/LocalStack) | `http://minio:9000` |
| `QUARKUS_S3_AWS_REGION` | (optional) AWS region for S3                  | `us-east-1` |
| `STICKER_IMAGES_BUCKET` | S3 bucket for sticker images                  | `sticker-images` |

### Profile: `prod-kafka`

Use this profile when running with Kafka/Redpanda for messaging.

**Additional Variables:**

| Variable | Purpose | Example |
|----------|---------|---------|
| `KAFKA_BOOTSTRAP_SERVERS` | Kafka broker addresses | `redpanda:9092` |
| `MP_MESSAGING_CONNECTOR_SMALLRYE_KAFKA_BOOTSTRAP_SERVERS` | SmallRye Kafka bootstrap servers | `redpanda:9092` |

**S3 Credentials**

In a real AWS environment these are automatically provided by the metadata service; they only need to be set explicitly
when running elsewhere - e.g. in docker-compose.

| Variable | Purpose |
|----------|---------|
| `QUARKUS_S3_AWS_CREDENTIALS_TYPE` | Set to `static` for explicit credentials |
| `QUARKUS_S3_AWS_CREDENTIALS_STATIC_PROVIDER_ACCESS_KEY_ID` | S3 access key |
| `QUARKUS_S3_AWS_CREDENTIALS_STATIC_PROVIDER_SECRET_ACCESS_KEY` | S3 secret key |
| `QUARKUS_S3_PATH_STYLE_ACCESS` | Set to `true` for MinIO compatibility |

### Profile: `prod-aws`

Use this profile when deploying to AWS with EventBridge for messaging.

**Additional Variables:**

| Variable | Purpose | Example |
|----------|---------|---------|
| `EVENT_BUS_NAME` | EventBridge bus name | `stickerlandia-events` |
| `AWS_REGION` | AWS region | `eu-central-1` |

## Building and Running

### Prerequisites
- Java 21+
- Maven 3.8+

### Development

Run in development mode:
```bash
./mvnw compile quarkus:dev
```

### Testing

Run tests:
```bash
./mvnw test
```

Run integration tests:
```bash
./mvnw verify
```

## Code Quality

This project enforces high code quality through multiple static analysis tools:

### Error Prone

This project uses Error Prone to catch common Java programming mistakes at compile time.

**Error Prone Integration:**
- Runs automatically during compilation (`./mvnw compile`)
- Catches bugs like incorrect Date usage, unused variables, and charset issues
- Configured in the Maven compiler plugin
- Uses Error Prone version 2.38.0

**Common Error Prone checks include:**
- `JavaUtilDate` - Flags usage of legacy `java.util.Date` API
- `UnusedVariable` - Detects unused fields and variables
- `DefaultCharset` - Warns about implicit charset usage in string operations

### Checkstyle

This project uses Checkstyle to enforce coding standards based on the Google Java Style Guide.

**Run Checkstyle validation:**
```bash
# Check code style (runs automatically during build)
./mvnw validate

# Run only Checkstyle check
./mvnw checkstyle:check

# Generate Checkstyle report (creates HTML report at target/reports/checkstyle.html)
./mvnw checkstyle:checkstyle
```

### Spotless (Code Formatting)

This project uses Spotless with Google Java Format to automatically fix code style issues. 

**Format your code:**
```bash
# Check if code formatting is correct
./mvnw spotless:check

# Automatically fix code formatting issues
./mvnw spotless:apply

# Format and then validate with Checkstyle
./mvnw spotless:apply validate
```
