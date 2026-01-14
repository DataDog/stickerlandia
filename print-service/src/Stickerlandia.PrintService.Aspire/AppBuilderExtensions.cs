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

namespace Stickerlandia.PrintService.Aspire;

internal static class AppBuilderExtensions
{
    public static IDistributedApplicationBuilder WithAwsApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<DynamoDBLocalResource> dynamoDbLocalResource)
    {
        ArgumentNullException.ThrowIfNull(dynamoDbLocalResource, nameof(dynamoDbLocalResource));
        
        // Add the API project to the distributed application builder
        var application = builder.AddProject<Projects.Stickerlandia_PrintService_Api>("api")
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithEnvironment("DRIVEN", builder.Configuration["DRIVEN"])
            .WithEnvironment("Aws__PrinterTableName", DefaultValues.DYNAMO_DB_PRINTER_TABLE_NAME)
            .WithEnvironment("AWS_ACCESS_KEY_ID", "dummyaccesskey")
            .WithEnvironment("AWS_SECRET_ACCESS_KEY", "dummysecret")
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithHttpsEndpoint(51545)
            .WithReference(dynamoDbLocalResource)
            .WaitFor(dynamoDbLocalResource);

        return builder;
    }

    public static InfrastructureResources WithAwsServices(this IDistributedApplicationBuilder builder)
    {
        var dynamoDb = builder.AddAWSDynamoDBLocal("dynamodb");

        return new InfrastructureResources(dynamoDb);
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