# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

This is a .NET 10 Print Management Service implementing ports and adapters architecture with platform adaptability by design. The service manages printer registration, print job submission, and job polling for physical printers.

### Project Structure

**Core (Domain Layer)**
- `Stickerlandia.PrintService.Core` - Domain entities, commands, queries, and interfaces

**Driven Adapters (Infrastructure)**
- `Stickerlandia.PrintService.AWS` - AWS implementations (DynamoDB, SNS, SQS)
- `Stickerlandia.PrintService.Agnostic` - Cloud-agnostic implementations (Kafka, Postgres)

**Driving Adapters (Entry Points)**
- `Stickerlandia.PrintService.Api` - ASP.NET minimal API (`/api/print/v1`)
- `Stickerlandia.PrintService.Worker` - Background worker service for agnostic hosting
- `Stickerlandia.PrintService.Lambda` - AWS Lambda functions
- `Stickerlandia.PrintService.Client` - Blazor Server app for printer clients

**Support Projects**
- `Stickerlandia.PrintService.Aspire` - .NET Aspire orchestration
- `Stickerlandia.PrintService.MigrationService` - Database migrations
- `Stickerlandia.PrintService.ServiceDefaults` - Shared Aspire defaults

### Domain Model

The Core domain uses CQRS with these key components:
- `Printer` - Aggregate representing a registered printer
- `PrintJob` - Aggregate for print job lifecycle (Queued → Processing → Completed/Failed)
- Commands: `RegisterPrinterCommand`, `SubmitPrintJobCommand`, `AcknowledgePrintJobCommand`
- Queries: `GetPrintersForEventQuery`, `GetPrintJobsForPrinterQuery`, `GetPrinterStatusesQuery`

### Authentication

The API supports two authentication schemes:
- **JWT Bearer** - For admin/user operations (printer registration, job submission)
- **API Key** - For printer client operations (polling, acknowledgment) via `X-Printer-Key` header

## Development Commands

### Local Development with .NET Aspire

```bash
cd src/Stickerlandia.PrintService.Aspire

# Agnostic services (Kafka, Postgres)
dotnet run -lp agnostic

# AWS services (Lambda, SNS, SQS, Postgres)
dotnet run -lp aws_native
```

### Testing

```bash
# Unit tests
dotnet test tests/Stickerlandia.PrintService.UnitTest

# Run a single unit test
dotnet test tests/Stickerlandia.PrintService.UnitTest --filter "FullyQualifiedName~TestMethodName"

# Client tests
dotnet test tests/Stickerlandia.PrintService.Client.Tests

# Integration tests (require DRIVING and DRIVEN environment variables)
cd tests/Stickerlandia.PrintService.IntegrationTest
export DRIVING=AGNOSTIC && export DRIVEN=AGNOSTIC && dotnet test

# AWS integration tests
export DRIVING=AWS && export DRIVEN=AWS && dotnet test
```

### Build

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Stickerlandia.PrintService.Api
```

### Infrastructure

**AWS CDK (from `infra/aws/`):**
```bash
npm run build    # Compile TypeScript
npm run test     # Run infrastructure tests
npm run cdk      # CDK commands
```

**Azure Terraform (from `infra/azure/`):**
Standard Terraform commands.

## Code Quality

- **Warnings as Errors**: All projects via `Directory.Build.props`
- **Static Analysis**: `CodeAnalysis.props` enables .NET analyzers
- **Nullable Reference Types**: Enabled across all projects
- **Suppressed Warnings**: CA2007 (ConfigureAwait), CA1016, NU1901

## Key Patterns

- **Outbox Pattern**: Reliable event publishing via `IOutbox` and `OutboxProcessor`
- **Platform Adaptability**: `DRIVING` and `DRIVEN` environment variables control adapter selection
- **Heartbeat-based Status**: Printer online/offline status computed from `LastHeartbeat` timestamp
- **RFC 7807 Problem Details**: Standard error responses via `GlobalExceptionHandler`
- **Event Schemas**: Documented in `docs/async_api.yaml`