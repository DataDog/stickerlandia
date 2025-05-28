using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.Azure;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.ServiceDefaults;

public static class ServiceDefaults
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.Configuration.AddEnvironmentVariables();
        builder.Services.AddLogging();
        
        var drivenAdapters = Environment.GetEnvironmentVariable("DRIVEN") ?? "";

        switch (drivenAdapters.ToUpper())
        {
            case "AZURE":
                builder.AddAzureAdapters();
                break;
            case "AGNOSTIC":
                builder.AddAgnosticAdapters();
                break;
            case "AWS":
                break;
            default:
                throw new ArgumentException($"Unknown driven adapters {drivenAdapters}");
        }

        builder.Services
            .AddStickerlandiaUserManagement();
        
        if (builder is WebApplicationBuilder hostBuilder)
        {
            hostBuilder.Host.UseSerilog((_, config) =>
            {
                config.MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(new JsonFormatter());
            });
        }
        
        return builder;
    }
}