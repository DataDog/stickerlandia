// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Aspire.Hosting.Azure;
using Azure.Messaging.ServiceBus;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace Stickerlandia.UserManagement.Aspire;

public static class AppBuilderExtensions
{
    public static IResourceBuilder<AzureServiceBusQueueResource> WithTestCommands(
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
                .SendMessageAsync(new ServiceBusMessage("Hello, world!"));

            return new ExecuteCommandResult { Success = true };
        }, new CommandOptions());

        return builder;
    }
    
    public static IResourceBuilder<KafkaServerResource> WithTestCommands(
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
                Acks             = Acks.All
            };
        });

        builder.WithCommand("SendStickerClaimedMessage", "Send Sticker Claimed Message", async (c) =>
        {
            var config = c.ServiceProvider.GetRequiredService<ProducerConfig>();
            using var producer = new ProducerBuilder<string, string>(config).Build();
            
            producer.Produce("users.stickerClaimed.v1", new Message<string, string> { Key = "", Value = JsonSerializer.Serialize(new
                {
                    accountId = "i2ieniu23hrri23",
                    stickerId = "dnqwiufb2f2"
                }) },
                (deliveryReport) =>
                {
                    if (deliveryReport.Error.Code != ErrorCode.NoError) {
                    }
                });
                
            producer.Flush(TimeSpan.FromSeconds(10));

            return new ExecuteCommandResult { Success = true };
        }, new CommandOptions());

        return builder;
    }

    public static IDistributedApplicationBuilder WithContainerizedApp(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> databaseResource,
        IResourceBuilder<IResourceWithConnectionString> messagingResource)
    {
        var cosmosDbConnectionString = builder.Configuration["COSMOSDB_CONNECTION_STRING"];
        
        var application = builder.AddProject<Projects.Stickerlandia_UserManagement_AspNet>("api")
            .WithReference(messagingResource)
            .WithEnvironment("ConnectionStrings__messaging", messagingResource)
            .WithEnvironment("Auth__Issuer", "https://stickerlandia.com")
            .WithEnvironment("Auth__Audience", "https://stickerlandia.com")
            .WithEnvironment("Auth__Key", "This is a super secret key that should not be used in production'")
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithEnvironment("DRIVEN", builder.Configuration["DRIVEN"])
            .WaitFor(messagingResource);
        
        if (string.IsNullOrEmpty(cosmosDbConnectionString))
        {
            application.WithEnvironment("ConnectionStrings__database", databaseResource);
            application.WaitFor(databaseResource);
        }
        else
        {
            application.WithEnvironment("ConnectionStrings__database", cosmosDbConnectionString);
        }

        return builder;
    }

    public static IDistributedApplicationBuilder WithAzureFunctions(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> databaseResource,
        IResourceBuilder<IResourceWithConnectionString> messagingResource)
    {
        var cosmosDbConnectionString = builder.Configuration["COSMOSDB_CONNECTION_STRING"];

        var functions = builder.AddAzureFunctionsProject<Projects.Stickerlandia_UserManagement_FunctionApp>("api")
            .WithEnvironment("ConnectionStrings__messaging", messagingResource)
            .WithEnvironment("Auth__Issuer", "https://stickerlandia.com")
            .WithEnvironment("Auth__Audience", "https://stickerlandia.com")
            .WithEnvironment("Auth__Key", "This is a super secret key that should not be used in production'")
            .WithEnvironment("DRIVING", builder.Configuration["DRIVING"])
            .WithEnvironment("DRIVEN", builder.Configuration["DRIVEN"])
            .WaitFor(messagingResource)
            .WithExternalHttpEndpoints();

        if (string.IsNullOrEmpty(cosmosDbConnectionString))
        {
            functions.WithEnvironment("ConnectionStrings__database", databaseResource);
            functions.WaitFor(databaseResource);
        }
        else
        {
            functions.WithEnvironment("ConnectionStrings__database", cosmosDbConnectionString);
        }

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