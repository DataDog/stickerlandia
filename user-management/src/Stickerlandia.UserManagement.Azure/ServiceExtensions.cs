/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.Azure;

public static class ServiceExtensions
{
    public static IServiceCollection AddAzureAdapters(this IServiceCollection services, IConfiguration configuration, bool enableDefaultUi = true)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        
        services.AddPostgresAuthServices(configuration, enableDefaultUi);

        services.AddSingleton<IMessagingWorker, ServiceBusStickerClaimedWorker>();
        
        services.AddSingleton<ServiceBusClient>(sp =>
            new ServiceBusClient(configuration["ConnectionStrings:messaging"]));

        services.AddSingleton<IUserEventPublisher, ServiceBusEventPublisher>();

        return services;
    }
}