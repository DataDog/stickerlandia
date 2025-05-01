using Microsoft.Extensions.Configuration;
using Stickerlandia.UserManagement.Aspire;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

#pragma warning disable ASPIRECOSMOSDB001
var cosmos = builder.AddAzureCosmosDB("cosmos-db")
    .RunAsPreviewEmulator(options =>
    {
        options.WithLifetime(ContainerLifetime.Persistent);
        options.WithDataExplorer();
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

var configuredRunAs = builder.Configuration["RUN_AS"];

if (!string.IsNullOrEmpty(configuredRunAs))
{
    RunSettings.OverrideTo(Enum.Parse<RunAs>(configuredRunAs));
}

switch (RunSettings.RunAs)
{
    case RunAs.AZURE_FUNCTIONS:
        builder.WithAzureFunctions(cosmos, serviceBus);
        break;
    case RunAs.AWS_LAMBDA:
        builder.WithAwsLambda(cosmos, serviceBus);
        break;
    default:
        builder.WithContainerizedApp(cosmos, serviceBus);
        break;
}

builder.Build().Run();