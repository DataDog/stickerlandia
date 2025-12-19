# Stickerlandia

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/DataDog/stickerlandia)

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

We use [mise-en-place](https://mise.jdx.dev/getting-started.html) for tool and task management.

```bash
mise trust && mise install         # Install tools
mise run env:setup                 # Configure .env, necessary for most of the rest of the tasks!
```

You can use mise to work with Docker locally:

```bash
mise run compose:deploy:local      # Start services (build from source)
mise run compose:deploy:release    # Start services (prebuilt GHCR images)
mise run compose:dev               # Start with hot reload
mise run compose:down              # Stop services
```

Or deploy to AWS in the cloud:

```bash
mise run aws:deploy:local          # Deploy to AWS (build containers locally)
mise run aws:deploy:release        # Deploy to AWS (prebuilt GHCR images)
mise run aws:info                  # Show AWS stack outputs
mise run aws:down                  # Destroy all AWS stacks
```

Run `mise tasks` to see top-level tasks, or `mise tasks --all` to see service-specific tasks:

```
//sticker-award:aws:deploy            Deploy to AWS
//sticker-award:build:docker-dev      Build dev container
//sticker-award:build:local           Build Go binary
//sticker-catalogue:aws:deploy        Deploy to AWS
//sticker-catalogue:build:local       Build Java package
//user-management:aws:deploy          Deploy to AWS
//user-management:build:local         Build .NET solution
...
```

Using mise's [monorepo support](https://mise.jdx.dev/tasks/toml-tasks.html#project-name), each service defines a consistent set of tasks (like `build:local`, `aws:deploy`) that roll up to the top-level orchestration tasks.

Access the app at `http://localhost:8080`.

### Default User

If you startup the entire application locally, database migrations are applied to the user service and two default users are created:

- User
    - Email address: user@stickerlandia.com
    - Password: Stickerlandia2025!
- Admin User
    - Email address: admin@stickerlandia.com
    - Password: Admin2025!

For detailed setup instructions, see the [environment setup guide](./docs/README.md).

> [!NOTE]
> For bigger, more serious microservice architectures, at some point its likely you'll have
> to give up the ability to `docker-compose` your whole stack. 

## Contributing

Contributions are welcome! Please see our [contributing guidelines](./CONTRIBUTING.md) for more information.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](./LICENSE) file for details.

