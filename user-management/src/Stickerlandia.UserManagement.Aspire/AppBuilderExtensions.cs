// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Aspire.Hosting.Azure;
using Azure.Messaging.ServiceBus;
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
            .WaitFor(messagingResource);
        
        if (string.IsNullOrEmpty(cosmosDbConnectionString))
        {
            application.WithEnvironment("ConnectionStrings__cosmosdb", databaseResource);
            application.WaitFor(databaseResource);
        }
        else
        {
            application.WithEnvironment("ConnectionStrings__cosmosdb", cosmosDbConnectionString);
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
            .WaitFor(messagingResource)
            .WithExternalHttpEndpoints();

        if (string.IsNullOrEmpty(cosmosDbConnectionString))
        {
            functions.WithEnvironment("ConnectionStrings__cosmosdb", databaseResource);
            functions.WaitFor(databaseResource);
        }
        else
        {
            functions.WithEnvironment("ConnectionStrings__cosmosdb", cosmosDbConnectionString);
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