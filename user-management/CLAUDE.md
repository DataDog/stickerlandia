# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

This is a .NET 8 User Management Service implementing ports and adapters architecture with platform adaptability by design. The service can run on Azure, AWS, or any cloud-agnostic container orchestrator.

### Core Components
- **Core** (`Stickerlandia.UserManagement.Core`) - Domain services and business logic
- **Auth** (`Stickerlandia.UserManagement.Auth`) - Authentication using OpenIddict
- **Agnostic** (`Stickerlandia.UserManagement.Agnostic`) - Cloud-agnostic implementations (Kafka, Postgres)
- **AWS** (`Stickerlandia.UserManagement.AWS`) - AWS-specific implementations (SNS, SQS)
- **Azure** (`Stickerlandia.UserManagement.Azure`) - Azure-specific implementations (Service Bus)

### Driving Adapters (Entry Points)
- **Api** - ASP.NET minimal API (`/api/users/v1`)
- **Worker** - Background worker service
- **FunctionApp** - Azure Functions
- **Lambda** - AWS Lambda functions

## Development Commands

### Local Development with .NET Aspire
Run from `src/Stickerlandia.UserManagement.Aspire/`:

```bash
# Agnostic services (Kafka, Postgres)
dotnet run -lp agnostic

# Azure services (Azure Functions, Service Bus, Postgres)
dotnet run -lp azure_native

# AWS services (Lambda, SNS, SQS, Postgres)
dotnet run -lp aws_native
```

### Testing

**Unit Tests:**
```bash
cd tests/Stickerlandia.UserManagement.UnitTest
dotnet test
```

**Integration Tests (requires environment variables):**
```bash
cd tests/Stickerlandia.UserManagement.IntegrationTest

# Agnostic integration tests
export DRIVING=AGNOSTIC && export DRIVEN=AGNOSTIC && dotnet test

# Azure integration tests
export DRIVING=AZURE && export DRIVEN=AZURE && dotnet test

# AWS integration tests
export DRIVING=AWS && export DRIVEN=AWS && dotnet test
```

### Infrastructure Deployment

**AWS CDK (from `infra/aws/`):**
```bash
npm run build    # Compile TypeScript
npm run test     # Run infrastructure tests
npm run cdk      # CDK commands
```

**Azure Terraform (from `infra/azure/`):**
Standard Terraform commands for Azure resources.

## Code Quality Standards

- **Static Analysis**: Enforced via `Directory.build.props` and `CodeAnalysis.props`
- **Warnings as Errors**: All projects treat warnings as errors
- **Nullable Reference Types**: Enabled across all projects
- **Implicit Usings**: Enabled for cleaner code

## Event Architecture

The service publishes and consumes events via:
- **Outbox Pattern**: Implemented for reliable event publishing
- **Platform-specific Messaging**: SNS/SQS (AWS), Service Bus (Azure), Kafka (Agnostic)
- **Event Schemas**: Documented in `docs/async_api.yaml`

## Key Patterns

- **CQRS**: Commands and queries are separated in the Core domain
- **Dependency Injection**: All adapters registered via service extensions
- **Configuration**: Platform-specific configuration in each adapter
- **Background Processing**: Handled by workers, functions, or lambdas depending on platform