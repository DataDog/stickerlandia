/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.AWS;

public static class ServiceExtensions
{
    public static IServiceCollection AddAwsAdapters(this IServiceCollection services, IConfiguration configuration,
        bool enableDefaultUi = true)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AwsConfiguration>(
            configuration.GetSection("Aws"));

        //services.AddSingleton<IMessagingWorker, SqsStickerClaimedWorker>();

        services.AddSingleton<IAmazonDynamoDB>(sp =>
        {
            var endpointUrl = Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL_DYNAMODB");
            if (!string.IsNullOrEmpty(endpointUrl))
            {
                var credentials = new BasicAWSCredentials("dummyaccesskey", "dummysecretkey");
                var config = new AmazonDynamoDBConfig { ServiceURL = endpointUrl };
                return new AmazonDynamoDBClient(credentials, config);
            }

            return new AmazonDynamoDBClient();
        });
        services.AddSingleton(sp => new AmazonSQSClient());
        services.AddSingleton(sp => new AmazonEventBridgeClient());
        services.AddSingleton(sp => new AmazonSimpleNotificationServiceClient());
        services.AddSingleton<IPrinterRepository, DynamoDbPrinterRepository>();
        services.AddSingleton<IPrintJobRepository, DynamoDbPrintJobRepository>();
        services.AddSingleton<IPrinterKeyValidator, DynamoDbPrinterKeyValidator>();
        services.AddSingleton<IOutbox, AwsOutboxImplementation>();

        services.AddSingleton<IPrintServiceEventPublisher, EventBridgeEventPublisher>();

        return services;
    }
}