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

        builder.Services.AddSingleton<DatabaseBootstrapper>();
        builder.Services.AddHealthChecks()
            .Add(new("cosmos", sp => sp.GetRequiredService<DatabaseBootstrapper>(), null, null));
        builder.Services.AddHostedService(sp => sp.GetRequiredService<DatabaseBootstrapper>());

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

        builder.Services.AddSingleton<DatabaseBootstrapper>();
        builder.Services.AddHealthChecks()
            .Add(new("cosmos", sp => sp.GetRequiredService<DatabaseBootstrapper>(), null, null));
        builder.Services.AddHostedService(sp => sp.GetRequiredService<DatabaseBootstrapper>());

        // Register the CosmosDB repository implementation
        builder.Services.AddSingleton<IUserAccountRepository, CosmosDbUserRepository>();
        builder.Services.AddSingleton<IOutbox, CosmosDbUserRepository>();
        builder.Services.AddSingleton<IUserEventPublisher, ServiceBusEventPublisher>();

        return builder;
    }
}

// Background service used to scaffold the Cosmos DB/Container
public class DatabaseBootstrapper(CosmosClient cosmosClient, ILogger<DatabaseBootstrapper> logger)
    : BackgroundService, IHealthCheck
{
    private bool _dbCreated;
    private bool _dbCreationFailed;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var status = _dbCreated
            ? HealthCheckResult.Healthy()
            : _dbCreationFailed
                ? HealthCheckResult.Unhealthy("Database creation failed.")
                : HealthCheckResult.Degraded("Database creation is still in progress.");
        return Task.FromResult(status);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The Cosmos DB emulator can take a very long time to start (multiple minutes) so use a custom resilience strategy
        // to ensure it retries long enough.
        var retry = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                Delay = TimeSpan.FromSeconds(5),
                MaxRetryAttempts = 60,
                BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    logger.LogWarning("""
                                      Issue during database creation after {AttemptDuration} on attempt {AttemptNumber}. Will retry in {RetryDelay}.
                                      Exception:
                                          {ExceptionMessage}
                                          {InnerExceptionMessage}
                                      """,
                        args.Duration,
                        args.AttemptNumber,
                        args.RetryDelay,
                        args.Outcome.Exception?.Message ?? "[none]",
                        args.Outcome.Exception?.InnerException?.Message ?? "");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
        await retry.ExecuteAsync(async ct =>
        {
            await cosmosClient.CreateDatabaseIfNotExistsAsync("Stickerlandia", cancellationToken: ct);
            var database = cosmosClient.GetDatabase("Stickerlandia");
            await database.CreateContainerIfNotExistsAsync(new ContainerProperties("Users", "/emailAddress"),
                cancellationToken: ct);
            logger.LogInformation("Database successfully created!");
            _dbCreated = true;
        }, stoppingToken);

        _dbCreationFailed = !_dbCreated;
    }
}