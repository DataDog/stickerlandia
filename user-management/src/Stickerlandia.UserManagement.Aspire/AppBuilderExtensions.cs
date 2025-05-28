// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Aspire.Hosting.Azure;
using Azure.Messaging.ServiceBus;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace Stickerlandia.UserManagement.Aspire;

public static class AppBuilderExtensions
{
    private const string TEST_COMMAND_ACCOUNT_ID = "ii2ieniu23hrri23";
    private const string TEST_COMMAND_STICKER_ID = "dnqwiufb2f2";

    public static IDistributedApplicationBuilder WithContainerizedApi(
        this IDistributedApplicationBuilder builder,
        InfrastructureResources resources)
    {
        var application = builder.AddProject<Projects.Stickerlandia_UserManagement_Api>("api")
            .WithReference(resources.MessagingResource)
            .WithEnvironment("ConnectionStrings__messaging", resources.MessagingResource)
            .WithEnvironment("ConnectionStrings__database", resources.DatabaseResource)
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithEnvironment("DRIVEN", builder.Configuration["DRIVEN"])
            .WithHttpsEndpoint(51545)
            .WaitFor(resources.MessagingResource)
            .WaitFor(resources.DatabaseResource);

        return builder;
    }

    public static InfrastructureResources WithAgnosticServices(
        this IDistributedApplicationBuilder builder)
    {
        var kafka = builder.AddKafka("messaging")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithKafkaUI()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithKafkaTestCommands();
        builder.CreateKafkaTopicsOnReady(kafka);

        var agnosticDb = builder
            .AddPostgres("database")
            .WithLifetime(ContainerLifetime.Persistent)
            .AddDatabase("users");

        return new InfrastructureResources(agnosticDb, kafka);
    }
    
    public static IDistributedApplicationBuilder CreateKafkaTopicsOnReady(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<KafkaServerResource> kafka)
    {
        builder.Eventing.Subscribe<ResourceReadyEvent>(kafka.Resource, async (@event, ct) =>
        {
            var kafkaConnectionString = await kafka.Resource.ConnectionStringExpression.GetValueAsync(ct);

            var config = new AdminClientConfig
            {
                BootstrapServers = kafkaConnectionString
            };

            using var adminClient = new AdminClientBuilder(config).Build();
            try
            {
                await adminClient.CreateTopicsAsync(new TopicSpecification[]
                {
                    new() { Name = "users.stickerClaimed.v1", NumPartitions = 1, ReplicationFactor = 1 },
                    new() { Name = "users.stickerClaimed.v1.dlq", NumPartitions = 1, ReplicationFactor = 1 },
                    new() { Name = "users.userRegistered.v1", NumPartitions = 1, ReplicationFactor = 1 }
                });
            }
            catch (CreateTopicsException e)
            {
                Console.WriteLine($"An error occurred creating topic: {e.Message}");
                throw;
            }
        });

        return builder;
    }

    public static IResourceBuilder<KafkaServerResource> WithKafkaTestCommands(
        this IResourceBuilder<KafkaServerResource> builder)
    {
        builder.ApplicationBuilder.Services.AddSingleton<ProducerConfig>(provider =>
        {
            var connectionString = builder.Resource.ConnectionStringExpression
                .GetValueAsync(CancellationToken.None).GetAwaiter().GetResult();
            return new ProducerConfig
            {
                // User-specific properties that you must set
                BootstrapServers = connectionString,
                // Fixed properties
                SecurityProtocol = SecurityProtocol.Plaintext,
                Acks = Acks.All
            };
        });

        builder.WithCommand("SendStickerClaimedMessage", "Send Sticker Claimed Message", async (c) =>
        {
            var config = c.ServiceProvider.GetRequiredService<ProducerConfig>();
            using var producer = new ProducerBuilder<string, string>(config).Build();

            producer.Produce("users.stickerClaimed.v1", new Message<string, string>
                {
                    Key = "", Value = JsonSerializer.Serialize(new
                    {
                        accountId = TEST_COMMAND_ACCOUNT_ID,
                        stickerId = TEST_COMMAND_STICKER_ID
                    })
                },
                (deliveryReport) =>
                {
                    if (deliveryReport.Error.Code != ErrorCode.NoError)
                    {
                    }
                });

            producer.Flush(TimeSpan.FromSeconds(10));

            return new ExecuteCommandResult { Success = true };
        }, new CommandOptions());

        return builder;
    }

    public static InfrastructureResources WithAzureNativeServices(this IDistributedApplicationBuilder builder)
    {
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .RunAsEmulator(c =>
            {
                c.WithLifetime(ContainerLifetime.Persistent);
                c.WithBindMount("servicebus-data", "/var/opt/mssql/data");
                c.WithHostPort(60001);
            });

        serviceBus
            .AddServiceBusQueue("users-stickerClaimed-v1", "users.stickerClaimed.v1")
            .WithServiceBusTestCommands();

        var topic = serviceBus
            .AddServiceBusTopic("users-userRegistered-v1", "users.userRegistered.v1");
        topic.AddServiceBusSubscription("noop");

        var azurePostgresDb = builder
            .AddPostgres("database")
            .WithLifetime(ContainerLifetime.Persistent)
            .AddDatabase("users");

        return new InfrastructureResources(azurePostgresDb, serviceBus);
    }

    public static IResourceBuilder<AzureServiceBusQueueResource> WithServiceBusTestCommands(
        this IResourceBuilder<AzureServiceBusQueueResource> builder)
    {
        builder.ApplicationBuilder.Services.AddSingleton<ServiceBusClient>(provider =>
        {
            var connectionString = builder.Resource.Parent.ConnectionStringExpression
                .GetValueAsync(CancellationToken.None).GetAwaiter().GetResult();
            return new ServiceBusClient(connectionString);
        });

        builder.WithCommand("SendSbMessage", "Send Service Bus message", async (c) =>
        {
            var sbClient = c.ServiceProvider.GetRequiredService<ServiceBusClient>();
            await sbClient.CreateSender(builder.Resource.QueueName)
                .SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(new
                {
                    accountId = TEST_COMMAND_ACCOUNT_ID,
                    stickerId = TEST_COMMAND_STICKER_ID
                })));

            return new ExecuteCommandResult { Success = true };
        }, new CommandOptions());

        return builder;
    }

    public static IDistributedApplicationBuilder WithBackgroundWorker(
        this IDistributedApplicationBuilder builder,
        InfrastructureResources resources)
    {
        var application = builder.AddProject<Projects.Stickerlandia_UserManagement_Worker>("worker")
            .WithReference(resources.MessagingResource)
            .WithEnvironment("ConnectionStrings__messaging", resources.MessagingResource)
            .WithEnvironment("ConnectionStrings__database", resources.DatabaseResource)
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithEnvironment("DRIVEN", builder.Configuration["DRIVEN"])
            .WaitFor(resources.MessagingResource)
            .WaitFor(resources.DatabaseResource);

        return builder;
    }

    public static IDistributedApplicationBuilder WithAzureFunctions(
        this IDistributedApplicationBuilder builder,
        InfrastructureResources resources)
    {
        var functions = builder.AddAzureFunctionsProject<Projects.Stickerlandia_UserManagement_FunctionApp>("worker")
            .WithEnvironment("ConnectionStrings__messaging", resources.MessagingResource)
            .WithEnvironment("ConnectionStrings__database", resources.DatabaseResource)
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithEnvironment("DRIVEN", builder.Configuration["DRIVEN"])
            .WaitFor(resources.MessagingResource)
            .WaitFor(resources.DatabaseResource);

        return builder;
    }

    public static IDistributedApplicationBuilder WithAwsLambda(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> databaseResource,
        IResourceBuilder<IResourceWithConnectionString> messagingResource)
    {
        //TODO: Implement AWS Lambda support

        return builder;
    }
}