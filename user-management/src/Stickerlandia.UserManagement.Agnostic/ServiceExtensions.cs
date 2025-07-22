// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Confluent.Kafka;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stickerlandia.UserManagement.Auth;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Agnostic;

public static class ServiceExtensions
{
    public static IServiceCollection AddAgnosticAdapters(this IServiceCollection services, IConfiguration configuration, bool enableDefaultUi = true)
    {
        services.AddKafkaMessaging(configuration);
        services.AddPostgresAuthServices(configuration, enableDefaultUi);

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
        IConfiguration configuration,
        bool enableDefaultUi = true)
    {
        services.AddDbContext<UserManagementDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("database"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("Stickerlandia.UserManagement.Agnostic"));
            options.UseOpenIddict();
        });

        var identityOptions = services.AddIdentity<PostgresUserAccount, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<UserManagementDbContext>()
            .AddDefaultTokenProviders();

        if (enableDefaultUi)
        {
            identityOptions.AddDefaultUI();
        }

        var disableSsl = false;

        if (configuration.GetValue<bool>("DISABLE_SSL")) disableSsl = true;

        services.AddCoreAuthentication(options =>
            options.UseEntityFrameworkCore()
                .UseDbContext<UserManagementDbContext>(), disableSsl);

        services.AddScoped<IAuthService, MicrosoftIdentityAuthService>();
        
        services.AddScoped<IOutbox, PostgresOutbox>();

        return services;
    }
}