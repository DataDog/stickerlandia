// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Agnostic;

public static class ServiceExtensions
{
    public static FunctionsApplicationBuilder AddAgnosticAdapters(this FunctionsApplicationBuilder builder)
    {
        builder.Services.AddServices(builder.Configuration);

        return builder;
    }

    public static WebApplicationBuilder AddAgnosticAdapters(this WebApplicationBuilder builder)
    {
        builder.Services.AddServices(builder.Configuration);

        return builder;
    }

    private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            // User-specific properties that you must set
            BootstrapServers = configuration.GetConnectionString("messaging"),
            // Fixed properties
            SecurityProtocol = SecurityProtocol.Plaintext,
            Acks             = Acks.All
        };
        
        services.AddSingleton(config);
        services.AddSingleton<IMessagingWorker, KafakStickerClaimedWorker>();

        // Register PostgreSQL repository with proper EF Core configuration
        services.AddPostgresUserRepository(configuration);
        
        // Register event publisher as singleton
        services.AddSingleton<IUserEventPublisher, KafkaEventPublisher>();

        return services;
    }
}
