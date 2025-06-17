using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.MigrationService;
using Stickerlandia.UserManagement.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
//builder.AddServiceDefaults();
builder.Services.AddPostgresAuthServices(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();