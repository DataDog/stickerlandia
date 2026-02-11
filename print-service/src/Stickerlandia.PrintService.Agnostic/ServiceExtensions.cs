/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.PrintService.Agnostic.Data;
using Stickerlandia.PrintService.Agnostic.Repositories;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.Agnostic;

public static class ServiceExtensions
{
    public static IServiceCollection AddAgnosticAdapters(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddKafkaMessaging(configuration);

        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<PrintServiceDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("database"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("Stickerlandia.PrintService.Agnostic"));
            options.UseOpenIddict();
        });

        // Register repositories as Scoped (to match DbContext lifetime)
        services.AddScoped<IPrinterRepository, PostgresPrinterRepository>();
        services.AddScoped<IPrintJobRepository, PostgresPrintJobRepository>();
        services.AddScoped<IPrinterKeyValidator, PostgresPrinterKeyValidator>();
        services.AddScoped<IOutbox, PostgresOutbox>();

        return services;
    }

    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var kafkaUsername = configuration?["KAFKA_USERNAME"];
        var kafkaPassword = configuration?["KAFKA_PASSWORD"];
        var securityProtocol = string.IsNullOrEmpty(kafkaUsername) ? SecurityProtocol.Plaintext : SecurityProtocol.SaslSsl;

        // Retry Kafka connection with exponential backoff to handle startup timing
        const int maxRetries = 5;
        var retryDelay = TimeSpan.FromSeconds(2);
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var adminClient = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = configuration!.GetConnectionString("messaging"),
                    SecurityProtocol = securityProtocol,
                    SaslUsername = kafkaUsername ?? null,
                    SaslPassword = kafkaPassword ?? null,
                    SaslMechanism = SaslMechanism.Plain,
                }).Build();

                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));

                if (metadata.Brokers.Count > 0)
                {
                    break; // Successfully connected
                }

                lastException = new InvalidOperationException("No Kafka brokers available with the provided configuration.");
            }
            catch (KafkaException ex)
            {
                lastException = ex;
            }

            if (attempt < maxRetries)
            {
                Thread.Sleep(retryDelay);
                retryDelay = TimeSpan.FromTicks(retryDelay.Ticks * 2); // Exponential backoff
            }
        }

        if (lastException != null)
        {
            throw new InvalidOperationException($"Failed to connect to Kafka after {maxRetries} attempts.", lastException);
        }

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration!.GetConnectionString("messaging"),
            SecurityProtocol = securityProtocol,
            SaslUsername = kafkaUsername ?? null,
            SaslPassword = kafkaPassword ?? null,
            SaslMechanism = SaslMechanism.Plain,
            Acks = Acks.All
        };

        var consumerConfig = new ConsumerConfig
        {
            // User-specific properties that you must set
            BootstrapServers = configuration!.GetConnectionString("messaging"),
            // Fixed properties
            SecurityProtocol = securityProtocol,
            SaslUsername = kafkaUsername ?? null,
            SaslPassword = kafkaPassword ?? null,
            SaslMechanism = SaslMechanism.Plain,
            Acks = Acks.All,
            GroupId = "stickerlandia-users",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        services.AddSingleton(producerConfig);
        services.AddSingleton(consumerConfig);

        // Register event publisher as singleton
        services.AddSingleton<IPrintServiceEventPublisher, KafkaEventPublisher>();

        return services;
    }
}
