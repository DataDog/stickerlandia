# User Management Service

The User Management Service manages user accounts, credentials and details about a user. It provides APIs allowing users to register and login as well as retrieve and update their user account details. The API also generates JWT's for use by other services on a succesful login. Endpoints for interacting with a users account have appropriate access controls to ensure users can only access their own accounts.

## Features

- User registration and login
- JWT-based authentication and authorization
- Event-driven integration with other services

## Authentication

All API endpoints (except `/health`, `/login` and `/register`) require authentication via JWT token in the Authorization header. 
Access controls ensure users can only operate on their own accounts.

## Error Handling

The API returns standard HTTP status codes and follows the RFC 7807 Problem Details specification for error responses.

## API Documentation

Full API documentation is available in OpenAPI format:
- Synchronous API: [api.yaml](./docs/api.yaml)
- Asynchronous API: [async_api.json](./docs/async_api.yaml)

## Code Structure

The code is structured around the ports and adapters architecture style, allowing the same business logic to run on a variety of different hosting providers and connecting to a variety of external services (databases, message brokers etc).

The code is roughly broken down into three sections:

- application *(driving adapters)*
    - [Stickerlandia.UserManagement.AspNet](./src/Stickerlandia.UserManagement.AspNet/)
    - [Stickerlandia.UserManagement.FunctionApp](./src/Stickerlandia.UserManagement.AspNet/)
    - [Stickerlandia.UserManagement.Lambda](./src/Stickerlandia.UserManagement.Lambda/)
- core
    - [Stickerlandia.UserManagement.Core](./src/Stickerlandia.UserManagement.Core/)
- infrastructure *(driven adapters)*
    - [Stickerlandia.UserManagement.Agnostic](./src/Stickerlandia.UserManagement.Agnostic/)
    - [Stickerlandia.UserManagement.Azure](./src/Stickerlandia.UserManagement.Azure/)
    - [Stickerlandia.UserManagement.AWS](./src/Stickerlandia.UserManagement.AWS/)

## Local Development

The application uses [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) to simplify local development, allowing you to run the application and all it's dependencies locally using `dotnet run` or `dotnet test`.

The [Stickerlandia.UserManagement.Aspire](./src/Stickerlandia.UserManagement.Aspire/) project contains all of the .NET Aspire setup. The project also includes various different launch profiles so that you can launch the application locally using your preferred driving and driven adapters.

### Configuration Options

```
export DRIVING=ASPNET
export DRIVEN=AGNOSTIC
```

## Driving

The `DRIVING` option determine where this application is actually going to run. Whether that is leveraging serverless functions like AWS Lambda or simply running in a container using a web framework like ASP.NET. It can be configured to either

### AZURE_FUNCTIONS

The Azure native hosting option uses Azure Functions to host both the HTTP endpoints and any event handlers

### ASPNET

The ASPNET option hosts the API endpoints using the ASPNET web framework, and also uses `BackgroundServices` to run event handlers on a background thread inside the same running process.

### AWS_LAMBDA

TODO:

### KUBERNETES

TODO:

## Driven

The `DRIVEN` option determines the implementations for any driven adapters inside the application, things like the database and messaging middlewares.

### Azure 

When set to `AZURE`, uses CosmosDB and Azure Service Bus.

### Agnostic

When set to `AGNOSTIC`, uses Postgres and Kafka.

### AWS

TODO: