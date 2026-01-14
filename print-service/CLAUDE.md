# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

This is a .NET 10 Print Management Service implementing ports and adapters architecture with platform adaptability by design. Currently, the service is built to run on AWS. But in the future 'agnostic' (Open Source services), Azure and GCP implementations will be provided.

### Core Components
- **Core** (`Stickerlandia.PrintService.Core`) - Domain services and business logic
- **Agnostic** (`Stickerlandia.PrintService.Agnostic`) - Cloud-agnostic implementations (Kafka, Postgres)
- **AWS** (`Stickerlandia.PrintService.AWS`) - AWS-specific implementations (SNS, SQS)
- **Azure** (`Stickerlandia.PrintService.Azure`) - Azure-specific implementations (Service Bus)

### Driving Adapters (Entry Points)
- **Api** - ASP.NET minimal API (`/api/print/v1`)
- **Worker** - Background worker service
- **Lambda** - AWS Lambda functions

## Development Commands

### Local Development with .NET Aspire
Run from `src/Stickerlandia.PrintService.Aspire/`:

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
cd tests/Stickerlandia.PrintService.UnitTest
dotnet test
```

**Integration Tests (requires environment variables):**
```bash
cd tests/Stickerlandia.PrintService.IntegrationTest

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