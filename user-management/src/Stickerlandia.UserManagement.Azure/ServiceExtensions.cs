// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Azure;

public static class ServiceExtensions
{
    public static FunctionsApplicationBuilder AddAzureAdapters(this FunctionsApplicationBuilder builder)
    {
        builder.AddAzureCosmosClient(
            "cosmosdb",
            settings => { settings.DisableTracing = false; },
            clientOptions =>
            {
                clientOptions.ApplicationName = "cosmos-aspire";
                clientOptions.SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.Default
                };
                clientOptions.CosmosClientTelemetryOptions = new CosmosClientTelemetryOptions
                {
                    DisableDistributedTracing = false
                };
                clientOptions.ConnectionMode = ConnectionMode.Gateway;
            });

        builder.Services.AddServices(builder.Configuration);

        return builder;
    }

    public static WebApplicationBuilder AddAzureAdapters(this WebApplicationBuilder builder)
    {
        builder.AddAzureCosmosClient(
            "cosmosdb",
            settings => { settings.DisableTracing = false; },
            clientOptions =>
            {
                clientOptions.ApplicationName = "cosmos-aspire";
                clientOptions.SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.Default
                };
                clientOptions.CosmosClientTelemetryOptions = new CosmosClientTelemetryOptions
                {
                    DisableDistributedTracing = false
                };
                clientOptions.ConnectionMode = ConnectionMode.Gateway;
            });

        builder.Services.AddServices(builder.Configuration);

        return builder;
    }

    private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IMessagingWorker, ServiceBusStickerClaimedWorker>();
        services.AddSingleton(new ServiceBusClient(configuration["ConnectionStrings:messaging"]));

        // RegisterUser the CosmosDB repository implementation
        services.AddSingleton<IUsers, CosmosDbUserRepository>();
        services.AddSingleton<IOutbox, CosmosDbUserRepository>();
        services.AddSingleton<IUserEventPublisher, ServiceBusEventPublisher>();

        return services;
    }
}