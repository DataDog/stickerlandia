// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Globalization;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stickerlandia.UserManagement.Auth;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Agnostic;

public static class ServiceExtensions
{
    public static IServiceCollection AddAgnosticAdapters(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKafkaMessaging(configuration);
        services.AddPostgresAuthServices(configuration);

        return services;
    }

    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var producerConfig = new ProducerConfig
        {
            // User-specific properties that you must set
            BootstrapServers = configuration.GetConnectionString("messaging"),
            // Fixed properties
            SecurityProtocol = SecurityProtocol.Plaintext,
            Acks = Acks.All
        };

        var consumerConfig = new ConsumerConfig
        {
            // User-specific properties that you must set
            BootstrapServers = configuration.GetConnectionString("messaging"),
            // Fixed properties
            SecurityProtocol = SecurityProtocol.Plaintext,
            GroupId = "stickerlandia-users",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        services.AddSingleton(producerConfig);
        services.AddSingleton(consumerConfig);

        // Register event publisher as singleton
        services.AddSingleton<IUserEventPublisher, KafkaEventPublisher>();
        services.AddSingleton<IMessagingWorker, KafakStickerClaimedWorker>();

        return services;
    }

    public static IServiceCollection AddPostgresAuthServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<UserManagementDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("database"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("Stickerlandia.UserManagement.Agnostic"));
            options.UseOpenIddict();
        });

        services.AddIdentityCore<PostgresUserAccount>()
            .AddEntityFrameworkStores<UserManagementDbContext>()
            .AddDefaultTokenProviders();

        var enableSsl = true;

        if (configuration.GetValue<bool>("DISABLE_SSL")) enableSsl = false;

        services.AddCoreAuthentication(options =>
            options.UseEntityFrameworkCore()
                .UseDbContext<UserManagementDbContext>(), enableSsl);

        services.AddScoped<IAuthService, MicrosoftIdentityAuthService>();

        services.AddScoped<IUsers, PostgresUserRepository>();
        services.AddScoped<IOutbox, PostgresUserRepository>();

        return services;
    }
}