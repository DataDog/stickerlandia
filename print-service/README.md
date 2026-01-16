# Stickerlandia Print Service

The **Stickerlandia Print Service** is a cloud-native system built with .NET 10 for managing sticker printing operations. It provides both a backend API for printer management and print job orchestration, and a client application that runs on machines with physical printers.

## What This Application Does

The Print Service consists of two main components:

1. **Backend API** - Server-side service for:
   - Registering printers for events
   - Submitting print jobs to specific printers
   - Tracking printer online/offline status via heartbeat
   - Managing print job lifecycle (Queued → Processing → Completed/Failed)

2. **Printer Client** - Blazor Server application that:
   - Polls the backend for pending print jobs
   - Stores job metadata locally
   - Acknowledges job completion back to the server

### Production-Ready Features

- Comprehensive health monitoring and observability
- Structured logging and error handling (RFC 7807 Problem Details)
- Static code analysis and quality enforcement
- Full test coverage with platform-specific integration tests
- Infrastructure as Code for AWS (CDK) and Azure (Terraform)

## Architecture

### Separation of Concerns

The project follows a ports and adapters (hexagonal) architecture, organized into:

**Core (Domain Layer)**
- `Stickerlandia.PrintService.Core` - Domain entities, commands, queries, and port interfaces

**Driven Adapters (Infrastructure)**
- `Stickerlandia.PrintService.AWS` - AWS implementations (DynamoDB, SNS, SQS)
- `Stickerlandia.PrintService.Agnostic` - Cloud-agnostic implementations (Kafka, Postgres)

**Driving Adapters (Entry Points)**
- `Stickerlandia.PrintService.Api` - ASP.NET minimal API
- `Stickerlandia.PrintService.Worker` - Background worker service
- `Stickerlandia.PrintService.Lambda` - AWS Lambda functions
- `Stickerlandia.PrintService.Client` - Blazor Server printer client

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/print/v1/event/{eventName}` | JWT | Register a new printer |
| GET | `/api/print/v1/event/{eventName}` | JWT | List printers for an event |
| GET | `/api/print/v1/event/{eventName}/printers/status` | JWT | Get printer statuses (online/offline) |
| POST | `/api/print/v1/event/{eventName}/printer/{printerName}/jobs` | JWT | Submit a print job |
| GET | `/api/print/v1/printer/jobs` | API Key | Poll for print jobs (printer client) |
| POST | `/api/print/v1/printer/jobs/{printJobId}/acknowledge` | API Key | Acknowledge job completion |

## Events

The service publishes domain events for print job lifecycle changes. See the full [Async API specification](./docs/async_api.yaml) for event schemas.

## Error Handling

The API returns standard HTTP status codes and follows the RFC 7807 Problem Details specification for error responses.

## Building and Running

### Prerequisites

- .NET 10.0
- .NET Aspire workload
- Docker (for local development)

### Development

One of the core principles is **platform adaptability** by design. The service can run on AWS or with cloud-agnostic infrastructure. Use [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) for local development with different hosting models.

Run with Agnostic services *(Kafka, Postgres)*:
```bash
cd src/Stickerlandia.PrintService.Aspire
dotnet run -lp agnostic
```

Run with AWS services *(SNS, SQS, Postgres)*:
```bash
cd src/Stickerlandia.PrintService.Aspire
dotnet run -lp aws_native
```

### Testing

Run unit tests:
```bash
dotnet test tests/Stickerlandia.PrintService.UnitTest
```

Run client tests:
```bash
dotnet test tests/Stickerlandia.PrintService.Client.Tests
```

The integration tests use [.NET Aspire testing support](https://learn.microsoft.com/en-us/dotnet/aspire/testing/write-your-first-test?pivots=xunit). Set the `DRIVING` and `DRIVEN` environment variables to select the adapter configuration.

Run Agnostic integration tests:
```bash
cd tests/Stickerlandia.PrintService.IntegrationTest
export DRIVING=AGNOSTIC && export DRIVEN=AGNOSTIC && dotnet test
```

Run AWS integration tests:
```bash
cd tests/Stickerlandia.PrintService.IntegrationTest
export DRIVING=AWS && export DRIVEN=AWS && dotnet test
```

### Infrastructure Deployment

**AWS CDK** (from `infra/aws/`):
```bash
npm run build    # Compile TypeScript
npm run test     # Run infrastructure tests
npm run cdk deploy
```

**Azure Terraform** (from `infra/azure/`):
```bash
terraform init
terraform plan
terraform apply
```

## Code Quality

This project enforces high code quality through static analysis tools. The [`Directory.Build.props`](./Directory.Build.props) file enables all .NET analyzers and treats warnings as errors across all projects.
