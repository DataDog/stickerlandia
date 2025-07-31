// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA2000

using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.GCP;

public static class ServiceExtensions
{
    public static IServiceCollection AddGcpAdapters(this IServiceCollection services, IConfiguration configuration, bool enableDefaultUi = true)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        
        services.AddPostgresAuthServices(configuration, enableDefaultUi);
        
        var projectId = configuration["ConnectionStrings:messaging"];
        if (string.IsNullOrEmpty(projectId))
        {
            throw new InvalidOperationException("Google ProjectId is not configured.");
        }

        services.AddSingleton<IMessagingWorker, GooglePubSubMessagingWorker>();
        
        services.AddKeyedSingleton<PublisherClient>("users.userRegistered.v1", 
            PublisherClient.Create(new TopicName(projectId, "users.userRegistered.v1")));

        services.AddSingleton<IUserEventPublisher, GooglePubSubEventPublisher>();

        return services;
    }
}