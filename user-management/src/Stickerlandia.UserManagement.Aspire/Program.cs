using Microsoft.Extensions.Configuration;
using Stickerlandia.UserManagement.Aspire;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

#pragma warning disable ASPIRECOSMOSDB001

var configuredDrivingAdapters = builder.Configuration["DRIVING"];
if (!string.IsNullOrEmpty(configuredDrivingAdapters))
{
    DrivingAdapterSettings.OverrideTo(Enum.Parse<DrivingAdapter>(configuredDrivingAdapters));
}

var configuredDrivenAdapters = builder.Configuration["DRIVEN"];
if (!string.IsNullOrEmpty(configuredDrivenAdapters))
{
    DrivenAdapterSettings.OverrideTo(Enum.Parse<DrivenAdapters>(configuredDrivenAdapters));
}

IResourceBuilder<IResourceWithConnectionString>? databaseResource = null;
IResourceBuilder<IResourceWithConnectionString>? messagingResource = null;

switch (DrivenAdapterSettings.DrivenAdapter)
{
    case DrivenAdapters.AZURE:
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
    case DrivenAdapters.AGNOSTIC:
        var kafka = builder.AddKafka("messaging")
            .WithKafkaUI()
            .WithTestCommands();
        var db = builder.AddPostgres("database").AddDatabase("users");
        
        messagingResource = kafka;
        databaseResource = db;
        
        break;
    case DrivenAdapters.AWS:
        break;
}

switch (DrivingAdapterSettings.DrivingAdapter)
{
    case DrivingAdapter.AZURE_FUNCTIONS:
        builder.WithAzureFunctions(databaseResource, messagingResource);
        break;
    case DrivingAdapter.AWS_LAMBDA:
        builder.WithAwsLambda(databaseResource, messagingResource);
        break;
    default:
        builder.WithContainerizedApp(databaseResource, messagingResource);
        break;
}

builder.Build().Run();