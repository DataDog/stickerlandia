# Sticker Award Service

The Sticker Award Service manages the assignment of stickers to users in the Stickerlandia platform. It provides APIs for creating, retrieving, and deleting sticker assignments, with appropriate access controls to ensure users can only manage their own stickers, or the stickers of users to whom they have been granted access.

## Features

- Sticker assignment management
- JWT-based authentication and authorization
- Event-driven integration with other services

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/award/v1/users/{userId}/stickers` | Get stickers assigned to a user |
| POST | `/api/award/v1/users/{userId}/stickers` | Assign a new sticker to a user |
| DELETE | `/api/award/v1/users/{userId}/stickers/{stickerId}` | Remove a sticker assignment |
| GET | `/health` | Service health check |

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
