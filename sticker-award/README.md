# Sticker Award Service

The Sticker Award Service manages stickers and their assignment to users in the Stickerlandia platform. It provides two distinct API domains:

- **Assignment API** (`/api/awards/v1/assignments`) - Manages user sticker assignments
- **Catalog API** (`/api/stickers/v1`) - Manages the sticker catalog (metadata, images, CRUD)

## Architecture

### Domain Structure
- **`award/`** - User sticker assignment domain (`/api/awards/v1`)
  - `StickerAwardResource.java` - HTTP API for user assignments
  - `StickerAwardRepository.java` - Data access and entity-DTO mapping
  - `dto/` - Request/Response DTOs (AssignStickerRequest, UserAssignmentDTO, etc.)
  - `entity/` - Database entities (StickerAssignment)
  - `messaging/` - Event publishing

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

### Assignment API (`/api/awards/v1/assignments`)
- `GET /api/awards/v1/assignments/{userId}` - Get user's sticker assignments
- `POST /api/awards/v1/assignments/{userId}` - Assign a sticker to a user
- `DELETE /api/awards/v1/assignments/{userId}/{stickerId}` - Remove sticker assignment

### Catalog API (`/api/stickers/v1`)
- `GET /api/stickers/v1` - List all stickers (paginated)
- `POST /api/stickers/v1` - Create new sticker
- `GET /api/stickers/v1/{stickerId}` - Get sticker metadata
- `PUT /api/stickers/v1/{stickerId}` - Update sticker metadata
- `DELETE /api/stickers/v1/{stickerId}` - Delete sticker
- `GET /api/stickers/v1/{stickerId}/image` - Get sticker image
- `PUT /api/stickers/v1/{stickerId}/image` - Upload/update sticker image

## Features

- Sticker catalog management (CRUD operations)
- User sticker assignment management
- Sticker image storage and retrieval
- JWT-based authentication and authorization
- Event-driven integration with other services

## Event Subscriptions

The service listens to the following events:
- `CertificationCompleted` - Assigns stickers automatically when users complete certifications

## Events Published

The service publishes the following events:
- `StickerAssignedToUser` - When a sticker is assigned to a user
- `StickerRemovedFromUser` - When a sticker is removed from a user

## Authentication

All API endpoints (except `/health`) require authentication via JWT token in the Authorization header. 
Access controls ensure users can only operate on their own sticker assignments unless they have admin privileges.

## Error Handling

The API returns standard HTTP status codes and follows the RFC 7807 Problem Details specification for error responses.

## API Documentation

Full API documentation is available in OpenAPI format:
- Synchronous API: [api.yaml](./docs/api.yaml)
- Asynchronous API: [async_api.json](./docs/async_api.json)

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

**Checkstyle Configuration:**
- Configuration file: `checkstyle.xml`
- Suppressions file: `checkstyle-suppressions.xml`
- Based on Google Java Style Guide (modified for 4-space indentation)
- Enforces 4-space indentation, 100-character line limit
- Checks import ordering, Javadoc completeness, and naming conventions
