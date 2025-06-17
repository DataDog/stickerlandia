# User Management Service

The User Management Service manages users. It provides one primary API:

- **User Management API** (`/api/users/v1`) - Manages users and provides OAuth 2.0 endpoints for AuthN/Z

## Architecture

### Domain Structure
- **`users/`** - User management domain (`/api/users/v1`)

### Separation of Concerns

The project follows a ports and adapters architecture style, split down into `Driving` and `Driven` adapters, as well as a `Core` library.

- **Stickerlandia.UserManagement.Agnostic** - Driven adapters for Agnostic services
- **Stickerlandia.UserManagement.AWS** - Driven adapters for AWS native services
- **Stickerlandia.UserManagement.Azure** - Driven adapters for Azure native services

- **Stickerlandia.UserManagement.Api** - Driving adapters for a containerized ASP.NET minimal API
- **Stickerlandia.UserManagement.Worker** - A seperate background worker service for agnostic background workers
- **Stickerlandia.UserManagement.FunctionApp** - Driving adapters for a Azure function app background workers
- **Stickerlandia.UserManagement.Lambda** - Driving adapters for AWS Lambda background workers

- **Stickerlandia.UserManagement.Core** - Core library for domain services
- **Stickerlandia.UserManagement.Auth** - Core library for auth concerns using [OpenIddict](https://documentation.openiddict.com/)


## API Endpoints

### User Management API (`/api/users/v1`)

You can find the full [Open API specification in the docs folder](./docs/api.yaml).

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

One of the core principles of Stickerlandia is **platform adaptability** by design. That means the user service can run on Azure, AWS or any cloud agnostic container orchestrator. When developing locally, you can use [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) to run and debug the application locally whichever stack you want to deploy against. The .NET Aspire project has different launch profiles for each of the different hosting models.

Run with Agnostic services *(Kafka, Postgres)*:
```bash
cd src/Stickerlandia.UserManagement.Aspire
dotnet run -lp agnostic
```

Run with Azure services *(Azure Functions, Azure Service Bus, Postgres)*:
```bash
cd src/Stickerlandia.UserManagement.Aspire
dotnet run -lp azure_native
```

Run with AWS services *(AWS Lambda, Amazon SNS, Aamzon SQS, Postgres)*:
```bash
cd src/Stickerlandia.UserManagement.Aspire
dotnet run -lp aws_native
```

### Testing

Run unit tests:
```bash
cd tests/Stickerlandia.UserManagement.UnitTest
dotnet test
```

The integration tests use [.NET Aspire testing support](https://learn.microsoft.com/en-us/dotnet/aspire/testing/write-your-first-test?pivots=xunit). This enables you to run full integration tests for each of the individual hosting models. To run the tests, you need to set the `DRIVING` and `DRIVEN` environment variables.

Run Agnostic integration tests:
```bash
cd tests/Stickerlandia.UserManagement.IntegrationTest
export DRIVING=AGNOSTIC
export DRIVEN=AGNOSTIC
dotnet test
```

Run Azure integration tests:
```bash
cd tests/Stickerlandia.UserManagement.IntegrationTest
export DRIVING=AZURE
export DRIVEN=AZURE
dotnet test
```

Run AWS integration tests:
```bash
cd tests/Stickerlandia.UserManagement.IntegrationTest
export DRIVING=AWS
export DRIVEN=AWS
dotnet test
```

## Code Quality

This project enforces high code quality through the use of static analysis tools:

### Built in Static Analyis Tools

.NET has built-in Roslyn analyzers that inspect your C# code for code style and quality issues. To enforce these styles, a [`Directory.build.props`](./Directory.build.props) file is included in the repository root that turns on all static analysis tools.
