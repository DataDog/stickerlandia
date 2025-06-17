# User Management Service

The User Management Service manages users. It provides one primary API:

- **User Management API** (`/api/users/v1`) - Manages users and provides OAuth 2.0 endpoints for AuthN/Z

## Architecture

### Domain Structure
- **`users/`** - User sticker assignment domain (`/api/users/v1`)
  - `StickerAwardResource.java` - HTTP API for user assignments
  - `StickerAwardRepository.java` - Data access and entity-DTO mapping
  - `dto/` - Request/Response DTOs (AssignStickerRequest, UserAssignmentDTO, etc.)
  - `entity/` - Database entities (StickerAssignment)
  - `messaging/` - Event publishing

### Separation of Concerns
- **Resource** - HTTP layer, handles requests/responses, only works with DTOs
- **Repository** - Data layer, maps between entities and DTOs, contains business logic
- **Entity** - Database layer, JPA entities for persistence
- **DTO** - API layer, request/response objects for HTTP APIs

## API Endpoints

### User Management API (`/api/users/v1`)

You can find th full [Open API specification in the docs folder](./docs/api.yaml).

## Features

- Register users
- OAuth2.0 with password and client credential grant types
- Get and update user accounts

## Events

You can find the full [Async API specification for events published and received in the docs folder](./docs/async_api.yaml)

## Authentication

All API endpoints (except `/health` and `/register`) require authentication via JWT token in the Authorization header. 
Access controls ensure users can only operate on their own accounts unless they have admin privileges.

## Error Handling

The API returns standard HTTP status codes and follows the RFC 7807 Problem Details specification for error responses.

## Building and Running

### Prerequisites
- .NET 8.0
- .NET Aspire

### Development

Run in development mode:
```bash
cd src/Stickerlandia.UserManagement.Aspire
dotnet run
```

### Testing

Run tests:
```bash
cd tests/Stickerlandia.UserManagement.UnitTest
dotnet test
```

Run integration tests:
```bash
cd tests/Stickerlandia.UserManagement.IntegrationTest
dotnet test
```

## Code Quality

This project enforces high code quality through multiple static analysis tools:

### Built in Static Analyis Tools

.NET has built-in Roslyn analyzers that inspect your C# code for code style and quality issues. To enforce these styles, a [`Directory.build.props`](./Directory.build.props) file is included in the repository root that turns on all static analysis tools.
