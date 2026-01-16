/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Stickerlandia.PrintService.Agnostic;
using Stickerlandia.PrintService.AWS;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.Observability;

namespace Stickerlandia.PrintService.ServiceDefaults;

public static class DefaultServiceExtensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder, bool enableDefaultUi = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.AddEnvironmentVariables();
        builder.Services.AddLogging();
        builder.Services.ConfigureDefaultPrintServices(builder.Configuration, enableDefaultUi);

        // Configure OpenTelemetry
        builder.ConfigureOpenTelemetry();

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

    private static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        var serviceName = PrintJobInstrumentation.ServiceName;
        var serviceVersion = PrintJobInstrumentation.ServiceVersion;

        // Configure the resource for all signals
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes([
                new KeyValuePair<string, object>("deployment.environment",
                    builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "development")
            ]);

        // Get OTLP endpoint from configuration (defaults to localhost for development)
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        // Configure tracing
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource(PrintJobInstrumentation.ServiceName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Filter out health check endpoints
                        options.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
                    })
                    .AddHttpClientInstrumentation();

                // Add OTLP exporter if endpoint is configured
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
                else
                {
                    // Use console exporter for development
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddMeter(PrintJobInstrumentation.ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                // Add OTLP exporter if endpoint is configured
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
                else
                {
                    // Use console exporter for development
                    metrics.AddConsoleExporter();
                }
            });

        return builder;
    }

    public static IServiceCollection ConfigureDefaultPrintServices(this IServiceCollection services,
        IConfiguration configuration,
        bool enableDefaultUi)
    {
        var drivenAdapters = Environment.GetEnvironmentVariable("DRIVEN") ?? "";

        switch (drivenAdapters.ToUpperInvariant())
        {
            case "AZURE":
                throw new ArgumentException("Azure driven adapter is not yet implemented");
            case "AGNOSTIC":
                services.AddAgnosticAdapters(configuration);
                break;
            case "AWS":
                services.AddAwsAdapters(configuration, enableDefaultUi);
                break;
            case "GCP":
                throw new ArgumentException("GCP driven adapter is not yet implemented");
            default:
                throw new ArgumentException($"Unknown driven adapters {drivenAdapters}");
        }

        services
            .AddStickerlandiaUserManagement();

        return services;
    }
}