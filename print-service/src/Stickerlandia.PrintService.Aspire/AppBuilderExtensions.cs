/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA2012 // Accessing ValueTasks directly is ok in the Aspire project

using System.Text.Json;
using Aspire.Hosting.AWS.DynamoDB;
using Aspire.Hosting.AWS.Lambda;
using Aspire.Hosting.Azure;
using Azure.Messaging.ServiceBus;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.PrintService.Aspire.WireMock;

namespace Stickerlandia.PrintService.Aspire;

internal static class AppBuilderExtensions
{
    public static IDistributedApplicationBuilder WithAwsApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<DynamoDBLocalResource> dynamoDbLocalResource,
        IResourceBuilder<WireMockOidcResource>? oidcServer = null)
    {
        ArgumentNullException.ThrowIfNull(dynamoDbLocalResource, nameof(dynamoDbLocalResource));

        // Add the API project to the distributed application builder
        var application = builder.AddProject<Projects.Stickerlandia_PrintService_Api>("api")
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithEnvironment("DRIVEN", builder.Configuration["DRIVEN"])
            .WithEnvironment("Aws__PrinterTableName", DefaultValues.DYNAMO_DB_PRINTER_TABLE_NAME)
            .WithEnvironment("Aws__PrintJobTableName", DefaultValues.DYNAMO_DB_PRINT_JOB_TABLE_NAME)
            .WithEnvironment("AWS_ACCESS_KEY_ID", "dummyaccesskey")
            .WithEnvironment("AWS_SECRET_ACCESS_KEY", "dummysecret")
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithHttpsEndpoint(51545)
            .WithReference(dynamoDbLocalResource)
            .WaitFor(dynamoDbLocalResource);

        // Add the API project to the distributed application builder
        var printClient = builder.AddProject<Projects.Stickerlandia_PrintService_Client>("client")
            .WithHttpsEndpoint(51546);

        // Configure OIDC authentication
        // Check if authentication is pre-configured (e.g., by tests)
        var preConfiguredAuthority = builder.Configuration["Authentication:Authority"];
        if (!string.IsNullOrEmpty(preConfiguredAuthority))
        {
            // Use pre-configured authentication (from tests)
            application
                .WithEnvironment("Authentication__Mode", builder.Configuration["Authentication:Mode"] ?? "OidcDiscovery")
                .WithEnvironment("Authentication__Authority", preConfiguredAuthority)
                .WithEnvironment("Authentication__Audience", builder.Configuration["Authentication:Audience"] ?? "print-service")
                .WithEnvironment("Authentication__RequireHttpsMetadata", builder.Configuration["Authentication:RequireHttpsMetadata"] ?? "false");
        }
        else if (oidcServer != null)
        {
            // Use WireMock server for local development
            application.WithOidcAuthentication(oidcServer);
        }

        return builder;
    }

    public static InfrastructureResources WithAwsServices(this IDistributedApplicationBuilder builder)
    {
        var dynamoDb = builder.AddAWSDynamoDBLocal("dynamodb");

        return new InfrastructureResources(DynamoDbResource: dynamoDb);
    }

    public static InfrastructureResources WithAgnosticServices(this IDistributedApplicationBuilder builder)
    {
        var postgres = builder.AddPostgres("postgres")
            .WithPgAdmin()
            .AddDatabase("printservice");

        var kafka = builder.AddKafka("messaging")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithKafkaUI()
            .WithLifetime(ContainerLifetime.Persistent);

        var migrationService = builder
            .AddProject<Projects.Stickerlandia_PrintService_MigrationService>("migration-service")
            .WithEnvironment("ConnectionStrings__database", postgres)
            .WithEnvironment("ConnectionStrings__messaging", kafka)
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithEnvironment("DRIVEN", builder.Configuration["DRIVEN"])
            .WithHttpsEndpoint(51545)
            .WaitFor(postgres);

        return new InfrastructureResources(PostgresResource: postgres, KafkaResource: kafka, MigrationResource: migrationService);
    }

    public static IDistributedApplicationBuilder WithAgnosticApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> postgresResource,
        IResourceBuilder<KafkaServerResource> kafkaResource,
        IResourceBuilder<ProjectResource> migrationService,
        IResourceBuilder<WireMockOidcResource>? oidcServer = null)
    {
        ArgumentNullException.ThrowIfNull(postgresResource, nameof(postgresResource));

        // Add the API project to the distributed application builder
        var application = builder.AddProject<Projects.Stickerlandia_PrintService_Api>("api")
            .WithEnvironment("ConnectionStrings__database", postgresResource)
            .WithEnvironment("ConnectionStrings__messaging", kafkaResource)
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithEnvironment("DRIVEN", builder.Configuration["DRIVEN"])
            .WithHttpsEndpoint(51545)
            .WithReference(postgresResource)
            .WithReference(kafkaResource)
            .WaitForCompletion(migrationService)
            .WaitFor(postgresResource)
            .WaitFor(kafkaResource);

        // Add the print client
        var printClient = builder.AddProject<Projects.Stickerlandia_PrintService_Client>("client")
            .WithHttpsEndpoint(51546);

        // Configure OIDC authentication
        var preConfiguredAuthority = builder.Configuration["Authentication:Authority"];
        if (!string.IsNullOrEmpty(preConfiguredAuthority))
        {
            application
                .WithEnvironment("Authentication__Mode", builder.Configuration["Authentication:Mode"] ?? "OidcDiscovery")
                .WithEnvironment("Authentication__Authority", preConfiguredAuthority)
                .WithEnvironment("Authentication__Audience", builder.Configuration["Authentication:Audience"] ?? "print-service")
                .WithEnvironment("Authentication__RequireHttpsMetadata", builder.Configuration["Authentication:RequireHttpsMetadata"] ?? "false");
        }
        else if (oidcServer != null)
        {
            application.WithOidcAuthentication(oidcServer);
        }

        return builder;
    }

    public static IDistributedApplicationBuilder WithBackgroundWorker(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<DynamoDBLocalResource> dynamoDbLocalResource)
    {
        ArgumentNullException.ThrowIfNull(dynamoDbLocalResource, nameof(dynamoDbLocalResource));

        var application = builder.AddProject<Projects.Stickerlandia_PrintService_Worker>("worker")
            .WithReference(dynamoDbLocalResource)
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithEnvironment("DRIVEN", builder.Configuration["DRIVEN"])
            .WithEnvironment("AWS_ACCESS_KEY_ID", "dummyaccesskey")
            .WithEnvironment("AWS_SECRET_ACCESS_KEY", "dummysecret")
            .WithEnvironment("Aws__PrinterTableName", DefaultValues.DYNAMO_DB_PRINTER_TABLE_NAME)
            .WithEnvironment("Aws__PrintJobTableName", DefaultValues.DYNAMO_DB_PRINT_JOB_TABLE_NAME)
            .WaitFor(dynamoDbLocalResource);

        return builder;
    }

#pragma warning disable CA2252

    public static IDistributedApplicationBuilder WithAwsLambda(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<DynamoDBLocalResource> dynamoDbLocalResource)
    {
        ArgumentNullException.ThrowIfNull(dynamoDbLocalResource, nameof(dynamoDbLocalResource));
        
        var apiLambdaFunction = builder.AddAWSLambdaFunction<Projects.Stickerlandia_PrintService_Api>("UsersApi",
                "Stickerlandia.PrintService.Api")
            .WithReference(dynamoDbLocalResource)
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithEnvironment("DRIVEN", builder.Configuration["DRIVEN"]);

        builder.AddAWSAPIGatewayEmulator("api", APIGatewayType.Rest)
            .WithReference(apiLambdaFunction, Method.Any, "{proxy+}")
            .WithHttpsEndpoint(51660);
        ;

        return builder;
    }
}