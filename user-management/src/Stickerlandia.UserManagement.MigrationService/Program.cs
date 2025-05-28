using Stickerlandia.UserManagement.MigrationService;
using Stickerlandia.UserManagement.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();