// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
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

        // Register the CosmosDB repository implementation
        builder.Services.AddSingleton<IUserAccountRepository, CosmosDbUserRepository>();

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
        builder.Services.AddSingleton<IMessagingWorker, ServiceBusStickerClaimedWorker>();
        builder.Services.AddSingleton(new ServiceBusClient(builder.Configuration["messaging"]));

        // Register the CosmosDB repository implementation
        builder.Services.AddSingleton<IUserAccountRepository, CosmosDbUserRepository>();
        builder.Services.AddSingleton<IOutbox, CosmosDbUserRepository>();
        builder.Services.AddSingleton<IUserEventPublisher, ServiceBusEventPublisher>();

        return builder;
    }
}