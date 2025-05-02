using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.Azure;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.SharedSetup.Configurations;

namespace Stickerlandia.UserManagement.SharedSetup;

public static class ServiceExtensions
{
    public static IHostApplicationBuilder AddUserManagementSharedSetup(this IHostApplicationBuilder builder)
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
                throw new Exception($"Unknown driven adapters {drivenAdapters}");
        }
        
        builder.Services
            .AddAuthConfigs(builder.Configuration)
            .AddStickerlandiaUserManagement();
        
        return builder;
    }
}