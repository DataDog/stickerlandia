// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.AWS;

public static class ServiceExtensions
{
    public static IServiceCollection AddAwsAdapters(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        
        services.AddPostgresAuthServices(configuration);

        services.Configure<AwsConfiguration>(
            configuration.GetSection("Aws"));

        services.AddSingleton<IMessagingWorker, SqsStickerClaimedWorker>();
        services.AddSingleton(sp => new AmazonSQSClient());
        services.AddSingleton(sp => new AmazonSimpleNotificationServiceClient());

        services.AddSingleton<IUserEventPublisher, SnsEventPublisher>();

        return services;
    }
}