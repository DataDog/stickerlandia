// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.Agnostic;

public static class ServiceExtensions
{
    public static IHostApplicationBuilder AddAgnosticAdapters(this IHostApplicationBuilder builder)
    {
        builder.Services.AddServices(builder.Configuration);

        return builder;
    }

    private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        var producerConfig = new ProducerConfig
        {
            // User-specific properties that you must set
            BootstrapServers = configuration.GetConnectionString("messaging"),
            // Fixed properties
            SecurityProtocol = SecurityProtocol.Plaintext,
            Acks             = Acks.All
        };
        
        var consumerConfig = new ConsumerConfig
        {
            // User-specific properties that you must set
            BootstrapServers = configuration.GetConnectionString("messaging"),
            // Fixed properties
            SecurityProtocol = SecurityProtocol.Plaintext,
            GroupId          = "stickerlandia-users",
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };
        
        services.AddSingleton(producerConfig);
        services.AddSingleton(consumerConfig);
        services.AddSingleton<IMessagingWorker, KafakStickerClaimedWorker>();

        // Register PostgreSQL repository with proper EF Core configuration
        services.AddPostgresUserRepository(configuration);
        
        // Register event publisher as singleton
        services.AddSingleton<IUserEventPublisher, KafkaEventPublisher>();

        return services;
    }
}
