# Stickerlandia Print Service

The **Stickerlandia Print Service** is a comprehensive, cloud-native system built with .NET 10. It serves as both the backend, server side and client side management for printing stickers.

## What This Application Does

The Print Service runs in two parts

// TODO

### 📊 **Production-Ready Features**
- Comprehensive health monitoring and observability
- Structured logging and error handling (RFC 7807 Problem Details)
- Static code analysis and quality enforcement
- Full test coverage with platform-specific integration tests
- Infrastructure as Code for AWS (CDK) and Azure (Terraform)

The service provides one primary API:

- **Print Service API** (`/api/print/v1`) - RESTful API for print operations

## Architecture

### Separation of Concerns

The project follows a ports and adapters architecture style, split down into `Driving` and `Driven` adapters, as well as a `Core` library.

- **Stickerlandia.PrintService.AWS** - Driven adapters for AWS native services

- **Stickerlandia.PrintService.Api** - Driving adapters for a containerized ASP.NET minimal API
- **Stickerlandia.PrintService.Worker** - A seperate background worker service for agnostic background workers
- **Stickerlandia.PrintService.Lambda** - Driving adapters for AWS Lambda background workers

- **Stickerlandia.PrintService.Core** - Core library for domain services


### AWS Architecture

TODO: 

## API Endpoints

TODO: 

## Key Features

## Events

You can find the full [Async API specification for events published and received in the docs folder](./docs/async_api.yaml)

## Error Handling

The API returns standard HTTP status codes and follows the RFC 7807 Problem Details specification for error responses.

## Building and Running

### Prerequisites
- .NET 10.0
- .NET Aspire

### Development

One of the core principles of Stickerlandia is **platform adaptability** by design. That means the user service can run on Azure, AWS or any cloud agnostic container orchestrator. When developing locally, you can use [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) to run and debug the application locally whichever stack you want to deploy against. The .NET Aspire project has different launch profiles for each of the different hosting models.

Run with Agnostic services *(Kafka, Postgres)*:
```bash
cd src/Stickerlandia.PrintService.Aspire
dotnet run -lp agnostic
```

Run with Azure services *(Azure Functions, Azure Service Bus, Postgres)*:
```bash
cd src/Stickerlandia.PrintService.Aspire
dotnet run -lp azure_native
```

Run with AWS services *(AWS Lambda, Amazon SNS, Aamzon SQS, Postgres)*:
```bash
cd src/Stickerlandia.PrintService.Aspire
dotnet run -lp aws_native
```

### Testing

Run unit tests:
```bash
cd tests/Stickerlandia.PrintService.UnitTest
dotnet test
```

The integration tests use [.NET Aspire testing support](https://learn.microsoft.com/en-us/dotnet/aspire/testing/write-your-first-test?pivots=xunit). This enables you to run full integration tests for each of the individual hosting models. To run the tests, you need to set the `DRIVING` and `DRIVEN` environment variables.

Run Agnostic integration tests:
```bash
cd tests/Stickerlandia.PrintService.IntegrationTest
export DRIVING=AGNOSTIC
export DRIVEN=AGNOSTIC
dotnet test
```

Run Azure integration tests:
```bash
cd tests/Stickerlandia.PrintService.IntegrationTest
export DRIVING=AZURE
export DRIVEN=AZURE
dotnet test
```

Run AWS integration tests:
```bash
cd tests/Stickerlandia.PrintService.IntegrationTest
export DRIVING=AWS
export DRIVEN=AWS
dotnet test
```

## Code Quality

This project enforces high code quality through the use of static analysis tools:

.NET has built-in Roslyn analyzers that inspect your C# code for code style and quality issues. To enforce these styles, a [`Directory.build.props`](./Directory.build.props) file is included in the repository root that turns on all static analysis tools.
