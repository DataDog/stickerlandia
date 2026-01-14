/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Stickerlandia.PrintService.Aspire;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

DrivingAdapterSettings.OverrideTo(DrivingAdapter.AWS);
DrivenAdapterSettings.OverrideTo(DrivenAdapters.AWS);

InfrastructureResources? resources = null;

switch (DrivenAdapterSettings.DrivenAdapter)
{
    case DrivenAdapters.AZURE:
        throw new ArgumentException("Azure driven adapter is not yet implemented");
    case DrivenAdapters.AGNOSTIC:
        throw new ArgumentException("Agnostic driven adapter is not yet implemented");
    case DrivenAdapters.AWS:
        resources = builder.WithAwsServices();
        break;
    case DrivenAdapters.GCP:
        throw new NotImplementedException("GCP driven adapter is not yet implemented");
}

ArgumentNullException.ThrowIfNull(resources, nameof(resources));

switch (DrivingAdapterSettings.DrivingAdapter)
{
    case DrivingAdapter.AZURE:
        throw new ArgumentException("Azure driven adapter is not yet implemented");
    case DrivingAdapter.AGNOSTIC:
        throw new ArgumentException("Agnostic driven adapter is not yet implemented");
    case DrivingAdapter.GCP:
        throw new NotImplementedException("GCP driven adapter is not yet implemented");
    case DrivingAdapter.AWS:
        builder.WithAwsApi(resources.DatabaseResource);
        break;
}

builder.Eventing.Subscribe<ResourceReadyEvent>(resources.DatabaseResource.Resource, async (evnt, ct) =>
{
    Console.WriteLine($"Creating DynamoDB table for {evnt.Resource.Name}...");

    // Configure DynamoDB service client to connect to DynamoDB local.
    var serviceUrl = resources.DatabaseResource.Resource.GetEndpoint("http").Url;
    var credentials = new BasicAWSCredentials("dummyaccesskey", "dummysecretkey");
    var ddbClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig
        { ServiceURL = serviceUrl, DefaultAWSCredentials = credentials });

    // Create the Accounts table.
    await ddbClient.CreateTableAsync(new CreateTableRequest
    {
        TableName = DefaultValues.DYNAMO_DB_PRINTER_TABLE_NAME,
        AttributeDefinitions = new List<AttributeDefinition>
        {
            new() { AttributeName = "PK", AttributeType = "S" },
            new() { AttributeName = "SK", AttributeType = "S" },
            new() { AttributeName = "GSI1PK", AttributeType = "S" }
        },
        KeySchema = new List<KeySchemaElement>
        {
            new() { AttributeName = "PK", KeyType = "HASH" },
            new() { AttributeName = "SK", KeyType = "RANGE" }
        },
        BillingMode = BillingMode.PAY_PER_REQUEST,
        GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>(1)
        {
            new()
            {
                IndexName = "GSI1",
                KeySchema = new List<KeySchemaElement>
                {
                    new() { AttributeName = "GSI1PK", KeyType = "HASH" }
                },
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL
                }
            }
        }
    }, ct);
    
    var tableExists = await ddbClient.DescribeTableAsync(DefaultValues.DYNAMO_DB_PRINTER_TABLE_NAME, ct);

    Console.WriteLine($"Table created: {tableExists.HttpStatusCode}");
});


builder.Build().Run();