/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.AWS;
using Stickerlandia.UserManagement.Azure;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.GCP;

namespace Stickerlandia.UserManagement.ServiceDefaults;

public static class DefaultServiceExtensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder, bool enableDefaultUi = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.Configuration.AddEnvironmentVariables();
        builder.Services.AddLogging();
        builder.Services.ConfigureDefaultUserManagementServices(builder.Configuration, enableDefaultUi);

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
        IConfiguration configuration,
        bool enableDefaultUi)
    {
        var drivenAdapters = Environment.GetEnvironmentVariable("DRIVEN") ?? "";

        switch (drivenAdapters.ToUpperInvariant())
        {
            case "AZURE":
                services.AddAzureAdapters(configuration, enableDefaultUi);
                break;
            case "AGNOSTIC":
                services.AddAgnosticAdapters(configuration, enableDefaultUi);
                break;
            case "AWS":
                services.AddAwsAdapters(configuration, enableDefaultUi);
                break;
            case "GCP":
                services.AddGcpAdapters(configuration, enableDefaultUi);
                break;
            default:
                throw new ArgumentException($"Unknown driven adapters {drivenAdapters}");
        }

        services
            .AddStickerlandiaUserManagement();

        return services;
    }
}