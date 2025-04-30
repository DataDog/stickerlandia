using Stickerlandia.UserManagement.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

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
    .RunAsEmulator(c => c
        .WithLifetime(ContainerLifetime.Persistent).WithHostPort(60001));

serviceBus
    .AddServiceBusQueue("users-stickerClaimed-v1", "users.stickerClaimed.v1")
    .WithTestCommands();

serviceBus
    .AddServiceBusQueue("users-userRegistered-v1", "users.userRegistered.v1");

var runAs = Environment.GetEnvironmentVariable("RUN_AS") ?? "ASPNET";

switch (runAs)
{
    case "AZURE_FUNCTIONS":
        builder.WithAzureFunctions(cosmos, serviceBus);
        break;
    case "AWS_LAMBDA":
        builder.WithAwsLambda(cosmos, serviceBus);
        break;
    default:
        builder.WithContainerizedApp(cosmos, serviceBus);
        break;
}

builder.Build().Run();