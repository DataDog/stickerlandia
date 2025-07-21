using Stickerlandia.UserManagement.ServiceDefaults;
using Stickerlandia.UserManagement.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults(enableDefaultUi: false);

builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddHostedService<StickerClaimedWorker>();

var host = builder.Build();
host.Run();