# Stickerlandia Documentation

This directory contains comprehensive documentation about the Stickerlandia application architecture, including service interactions, event flows, and deployment options.

## Documentation Contents

- [Event Flows](event_flows.md) - Sequence diagrams showing the main event flows between services

## Service Communication

Services in Stickerlandia communicate in two primary ways:

### Synchronous Communication (REST APIs)

- Used for operations requiring immediate responses
- Appropriate for user-facing operations that need immediate feedback
- Examples: user authentication, retrieving user profiles, querying sticker collections
- Implemented as direct HTTP calls between services or from client applications
- Secured using JWT authentication
- API contracts defined in OpenAPI specification

### Asynchronous Communication (Events)

- Used for operations that can be processed in the background
- Appropriate for cross-service notifications and background processes
- Examples: user registration notifications, sticker assignments, certification completions
- Provides system resilience, independent scaling, and reduced service coupling
- Enables critical operations to complete even if dependent services are temporarily unavailable
- Event contracts defined in AsyncAPI specification

The choice between synchronous and asynchronous communication is based on:
- Need for immediate response
- Criticality of the operation
- Number of services that need to be notified
- Fault tolerance requirements

## Message Brokers

Asynchronous communication is implemented using message brokers with support for:
- Kafka - for Kubernetes and self-hosted deployments
- Azure Service Bus - for Azure cloud deployments

## Event Structure

Events follow the CloudEvents specification and include:
- Standard attributes (id, source, type, time)
- Event-specific data payload
- The event data is serialized as JSON
- Topics/channels are named using the pattern: `domain.eventName.version`

## Architecture Overview

Stickerlandia consists of three main services:

1. **User Management Service** (.NET)
   - Handles user registration and authentication
   - Manages user profiles and credentials
   - Issues and validates JWT tokens
   - Tracks user sticker statistics

2. **Sticker Award Service** (Go)
   - Handles sticker assignments and removals
   - Processes certification completions
   - Tracks which users own which stickers
   - Manages assignment-based business logic

3. **Sticker Catalogue Service** (Java/Quarkus)
   - Manages the master catalog of available stickers
   - Handles sticker metadata and images
   - Provides catalog browsing and search functionality
   - Maintains sticker definitions and properties

## Deployment Options

Stickerlandia supports multiple deployment options:

1. **Serverless Deployment**
   - User Management: Azure Functions / AWS Lambda
   - Sticker Award: Go with AWS Lambda or Azure Functions
   - Sticker Catalogue: Quarkus with AWS Lambda or Azure Functions
   - Messaging: Azure Service Bus / AWS SQS+SNS
   - Database: Azure Cosmos DB / AWS DynamoDB

2. **Container Orchestration**
   - Kubernetes deployment with Helm charts
   - Docker Compose for local development
   - PostgreSQL databases with proper persistence
   - Kafka for messaging

3. **Local Development**
   - Docker Compose setup for all services
   - Local PostgreSQL instances
   - In-memory or containerized Kafka

## Future Documentation

Additional documentation will include:

- Deployment diagrams
- Data model documentation
- Detailed API documentation
- Environment setup guides
- UI mockups and user flows 