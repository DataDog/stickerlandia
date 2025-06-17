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

public static class DefaultServiceExtensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.Configuration.AddEnvironmentVariables();
        builder.Services.AddLogging();
        builder.Services.ConfigureDefaultUserManagementServices(builder.Configuration);

        if (builder is WebApplicationBuilder hostBuilder)
            hostBuilder.Host.UseSerilog((_, config) =>
            {
                config.MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(new JsonFormatter());
            });

        return builder;
    }

    public static IServiceCollection ConfigureDefaultUserManagementServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var drivenAdapters = Environment.GetEnvironmentVariable("DRIVEN") ?? "";

        switch (drivenAdapters.ToUpperInvariant())
        {
            case "AZURE":
                services.AddAzureAdapters(configuration);
                break;
            case "AGNOSTIC":
                services.AddAgnosticAdapters(configuration);
                break;
            case "AWS":
                break;
            default:
                throw new ArgumentException($"Unknown driven adapters {drivenAdapters}");
        }

        services
            .AddStickerlandiaUserManagement();

        return services;
    }
}