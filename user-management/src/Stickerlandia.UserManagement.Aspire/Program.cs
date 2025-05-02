using Microsoft.Extensions.Configuration;
using Stickerlandia.UserManagement.Aspire;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

#pragma warning disable ASPIRECOSMOSDB001

var configuredRunAs = builder.Configuration["RUN_AS"];
var configuredHostOn = builder.Configuration["HOST_ON"];

if (!string.IsNullOrEmpty(configuredRunAs))
{
    RunSettings.OverrideTo(Enum.Parse<RunAs>(configuredRunAs));
}

if (!string.IsNullOrEmpty(configuredHostOn))
{
    HostOnSettings.OverrideTo(Enum.Parse<HostOn>(configuredHostOn));
}

IResourceBuilder<IResourceWithConnectionString>? databaseResource = null;
IResourceBuilder<IResourceWithConnectionString>? messagingResource = null;

switch (HostOnSettings.HostOn)
{
    case HostOn.AZURE:
        var cosmos = builder
            .AddAzureCosmosDB("database")
            .RunAsPreviewEmulator(options =>
            {
                options.WithLifetime(ContainerLifetime.Persistent);
            });

        var database = cosmos.AddCosmosDatabase("Stickerlandia");
        var container = database.AddContainer("Users", "/emailAddress");

        var serviceBus = builder.AddAzureServiceBus("messaging")
            .RunAsEmulator(c =>
            {
                c.WithLifetime(ContainerLifetime.Persistent);
                c.WithHostPort(60001);
            });

        serviceBus
            .AddServiceBusQueue("users-stickerClaimed-v1", "users.stickerClaimed.v1")
            .WithTestCommands();

        var topic = serviceBus
            .AddServiceBusTopic("users-userRegistered-v1", "users.userRegistered.v1");
        topic.AddServiceBusSubscription("noop");

        databaseResource = cosmos;
        messagingResource = serviceBus;
        break;
    case HostOn.AGNOSTIC:
        var kafka = builder.AddKafka("messaging")
            .WithKafkaUI();
        var db = builder.AddPostgres("database").AddDatabase("users");
        
        messagingResource = kafka;
        databaseResource = db;
        
        break;
    case HostOn.AWS:
        break;
}

switch (RunSettings.RunAs)
{
    case RunAs.AZURE_FUNCTIONS:
        builder.WithAzureFunctions(databaseResource, messagingResource);
        break;
    case RunAs.AWS_LAMBDA:
        builder.WithAwsLambda(databaseResource, messagingResource);
        break;
    default:
        builder.WithContainerizedApp(databaseResource, messagingResource);
        break;
}

builder.Build().Run();