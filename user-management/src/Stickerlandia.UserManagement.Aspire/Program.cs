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
        .WithLifetime(ContainerLifetime.Persistent));

serviceBus
    .AddServiceBusQueue("users-stickerClaimed-v1", "users.stickerClaimed.v1")
    .WithTestCommands();

serviceBus
    .AddServiceBusQueue("users-userRegistered-v1", "users.userRegistered.v1");

var functions = builder.AddAzureFunctionsProject<Projects.Stickerlandia_UserManagement_FunctionApp>("functions")
    .WithEnvironment("ConnectionStrings__cosmosdb", cosmos)
    .WithReference(serviceBus)
    .WaitFor(serviceBus)
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Stickerlandia_UserManagement_FunctionApp>("user-management-api")
    .WithReference(functions)
    .WithReference(cosmos)
    .WithEnvironment("ConnectionStrings__cosmosdb", cosmos)
    .WithEnvironment("Auth__Issuer", "https://stickerlandia.com")
    .WithEnvironment("Auth__Audience", "https://stickerlandia.com")
    .WithEnvironment("Auth__Key", "This is a super secret key that should not be used in production'")
    .WaitFor(functions);

var webApp = builder.AddProject<Projects.Stickerlandia_UserManagement_AspNet>("aspnetapp")
    .WithReference(cosmos)
    .WithEnvironment("messaging", serviceBus)
    .WithEnvironment("ConnectionStrings__cosmosdb", cosmos)
    .WithEnvironment("Auth__Issuer", "https://stickerlandia.com")
    .WithEnvironment("Auth__Audience", "https://stickerlandia.com")
    .WithEnvironment("Auth__Key", "This is a super secret key that should not be used in production'")
    .WaitFor(cosmos);

builder.Build().Run();