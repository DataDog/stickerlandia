# Stickerlandia

![Stickerlandia Logo](https://img.shields.io/badge/Stickerlandia-Collect_and_Trade_Stickers-blue)
[![Documentation](https://img.shields.io/badge/Documentation-Available-green)](./docs/README.md)

## Overview

Stickerlandia lets you collect Datadog stickers by completing Datadog certifications, trading with others, and through various other exciting mechanisms. Users can bring their Stickerlandia account to Datadog events and receive high-quality physical stickers for their gadgets.

This application serves a dual purpose:

- A fun community engagement platform
- A reference application demonstrating modern architectural patterns

As a reference application, Stickerlandia showcases **platform adaptability** by design. We've gone to significant lengths to ensure that all components can be retargeted idiomatically to different platforms - from AWS serverless to Azure Functions to Kubernetes to simple Docker deployments. This adaptability reflects the increasingly common need for applications to run across diverse environments while leveraging each platform's native strengths.

## Key Features

- **Collect stickers** through certifications and achievements
- **Track your collection** of digital stickers
- **Trade stickers** with other community members
- **Redeem physical stickers** at Datadog events
- **Multi-platform deployment** capabilities for cloud and on-premises environments
- **Consistent architecture** across all deployment targets

## Documentation

Comprehensive documentation is available in the [docs](./docs/README.md).

For deployment options and platform-specific configurations, see the [deployment guide](./docs/deploy.md).

## High-Level Architecture

Stickerlandia follows a microservice architecture that can run in various configurations across different types of modern infrastructure:
- Cloud-native serverless
- Cloud-native container orchestration
- Self-hosted container orchestration

Each deployment model uses appropriate components optimized for the specific platform - from database selection to queue technology and load balancing. This platform-specific optimization allows Stickerlandia to maintain consistent functionality while leveraging the unique capabilities of each environment.

## Technical Stack

- **User Management**: .NET-based service for identity and authentication
- **Sticker Award**: Go-based service for sticker assignment management
- **Sticker Catalogue**: Java/Quarkus-based service for sticker catalog management
- **Message Broker**: Kafka or Azure Service Bus for event distribution
- **Databases**: PostgreSQL for structured data
- **Authentication**: JWT-based authentication

## Services

| Service | Description | Documentation |
|---------|-------------|---------------|
| [User Management](./user-management/) | Manages user accounts, authentication, and profile information. Handles user registration, login, and JWT token issuance. | [API Docs](./user-management/docs/api.yaml) |
| [Sticker Award](./sticker-award/) | Manages the assignment of stickers to users. Tracks which users have which stickers and handles assignment/removal based on criteria like certification completion. | [API Docs](./sticker-award/docs/api.yaml) |
| [Sticker Catalogue](./sticker-catalogue/) | Manages the master catalog of available stickers. Handles sticker metadata, images, and provides catalog browsing functionality. | [API Docs](./sticker-catalogue/docs/api.yaml) |

## Observability

Stickerlandia is fully instrumented with Datadog's observability and analysis tooling, showcasing best practices for modern application monitoring and performance optimization.

## Getting Started

To run Stickerlandia locally, follow these steps:

1. Clone this repository
2. Set up dependencies (see service-specific READMEs)
3. Start the services using Docker Compose or your preferred method
4. Access the application at `http://localhost:8080`

For detailed setup instructions, see the [environment setup guide](./docs/README.md).

> [!NOTE]
> For bigger, more serious microservice architectures, at some point its likely you'll have
> to give up the ability to `docker-compose` your whole stack. 

## Contributing

Contributions are welcome! Please see our [contributing guidelines](./CONTRIBUTING.md) for more information.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](./LICENSE) file for details.

