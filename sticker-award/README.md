# Sticker Award Service

The Sticker Award Service manages stickers and their assignment to users in the Stickerlandia platform. It provides APIs for sticker catalog management and user sticker assignments.

## Architecture

### Domain Structure
- **`award/`** - User sticker assignment domain
  - `StickerAwardResource.java` - HTTP API for user assignments
  - `StickerAwardRepository.java` - Data access and entity-DTO mapping
  - `dto/` - Request/Response DTOs (AssignStickerRequest, UserAssignmentDTO, etc.)
  - `entity/` - Database entities (StickerAssignment)
  - `messaging/` - Event publishing

- **`sticker/`** - Sticker catalog domain
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

## Features

- Sticker catalog management (CRUD operations)
- User sticker assignment management
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
