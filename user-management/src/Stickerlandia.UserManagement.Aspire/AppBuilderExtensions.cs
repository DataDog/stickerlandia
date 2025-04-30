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
        var webApp = builder.AddProject<Projects.Stickerlandia_UserManagement_AspNet>("api")
            .WithReference(databaseResource)
            .WithReference(messagingResource)
            .WithEnvironment("ConnectionStrings__messaging", messagingResource)
            .WithEnvironment("ConnectionStrings__cosmosdb", databaseResource)
            .WithEnvironment("Auth__Issuer", "https://stickerlandia.com")
            .WithEnvironment("Auth__Audience", "https://stickerlandia.com")
            .WithEnvironment("Auth__Key", "This is a super secret key that should not be used in production'")
            .WaitFor(databaseResource)
            .WaitFor(messagingResource);

        return builder;
    }

    public static IDistributedApplicationBuilder WithAzureFunctions(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> databaseResource,
        IResourceBuilder<IResourceWithConnectionString> messagingResource)
    {
        var functions = builder.AddAzureFunctionsProject<Projects.Stickerlandia_UserManagement_FunctionApp>("api")
            .WithEnvironment("ConnectionStrings__cosmosdb", databaseResource)
            .WithEnvironment("ConnectionStrings__messaging", messagingResource)
            .WithEnvironment("Auth__Issuer", "https://stickerlandia.com")
            .WithEnvironment("Auth__Audience", "https://stickerlandia.com")
            .WithEnvironment("Auth__Key", "This is a super secret key that should not be used in production'")
            .WithEndpoint(5139, 7071)
            .WaitFor(messagingResource)
            .WaitFor(databaseResource)
            .WithExternalHttpEndpoints();

        builder.AddProject<Projects.Stickerlandia_UserManagement_FunctionApp>("function-app")
            .WithEnvironment("ConnectionStrings__cosmosdb", databaseResource)
            .WithEnvironment("ConnectionStrings__messaging", messagingResource)
            .WithEnvironment("Auth__Issuer", "https://stickerlandia.com")
            .WithEnvironment("Auth__Audience", "https://stickerlandia.com")
            .WithEnvironment("Auth__Key", "This is a super secret key that should not be used in production'")
            .WithReference(functions)
            .WaitFor(functions);

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