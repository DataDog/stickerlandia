using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.MigrationService;

var builder = Host.CreateApplicationBuilder(args);
//builder.AddServiceDefaults();
builder.Services.AddPostgresAuthServices(builder.Configuration, false);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();