using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Json;
using Stickerlandia.UserManagement.Azure;
using Stickerlandia.UserManagement.FunctionApp.Configurations;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.FunctionApp.Middlewares;

var builder = FunctionsApplication.CreateBuilder(args);
builder.UseDefaultWorkerMiddleware();
builder.ConfigureFunctionsWebApplication();

builder.UseMiddleware<ExceptionHandlingMiddleware>();

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddLogging();

var logger = Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

var appLogger = new SerilogLoggerFactory(logger)
    .CreateLogger<Program>();

var hosting = Environment.GetEnvironmentVariable("HOST_ON");

switch (hosting.ToUpper())
{
    case "AZURE":
        builder.AddAzureAdapters();
        break;
    case "AGNOSTIC":
        builder.AddAgnosticAdapters();
        break;
    default:
        throw new ArgumentException($"Unknown hosting option {hosting}");
}

builder.Services
    .AddAuthConfigs(appLogger, builder)
    .AddStickerlandiaUserManagement();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .Configure<LoggerFilterOptions>(options =>
    {
        // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
        // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service#ilogger-logs
        LoggerFilterRule? toRemove = options.Rules.FirstOrDefault(rule =>
            rule.ProviderName
            == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider"
        );

        if (toRemove is not null)
        {
            options.Rules.Remove(toRemove);
        }
    });

appLogger.LogInformation("Application started"); ;

var app = builder.Build();

var database = app.Services.GetRequiredService<IUsers>();
await database.MigrateAsync();

await app.RunAsync();