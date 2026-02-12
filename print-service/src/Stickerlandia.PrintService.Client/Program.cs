// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Formatting.Json;
using Stickerlandia.PrintService.Client.Components;
using Stickerlandia.PrintService.Client.Configuration;
using Stickerlandia.PrintService.Client.Services;
using Stickerlandia.PrintService.Client.Telemetry;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// Configure OpenTelemetry
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(
        serviceName: PrintClientInstrumentation.ServiceName,
        serviceVersion: PrintClientInstrumentation.ServiceVersion)
    .AddAttributes([
        new KeyValuePair<string, object>("deployment.environment",
            builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "development")
    ]);

builder.Services.AddSingleton<PrintClientInstrumentation>();
var otel = builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(resourceBuilder)
            .AddSource(PrintClientInstrumentation.ServiceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation(options =>
            {
                options.FilterHttpRequestMessage = (httpRequestMessage) =>
                {
                    // Exclude any requests to the Datadog agent or localhost as they are likely related to telemetry export and not relevant for application tracing
                    var isDatadogHost = httpRequestMessage.RequestUri?.Host.Contains("datadog-agent", StringComparison.OrdinalIgnoreCase) ?? true;
                    var isLocalHost = httpRequestMessage.RequestUri?.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase) ?? true;

                    return !isDatadogHost && !isLocalHost;
                };
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(resourceBuilder)
            .AddMeter(PrintClientInstrumentation.ServiceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

if (otlpEndpoint != null)
{
    otel.UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri(otlpEndpoint));
}

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add configuration service (singleton - shared across app)
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();

// Add client status service (singleton - shared state for UI)
builder.Services.AddSingleton<ClientStatusService>();

// Add local storage service (singleton - caches jobs in memory)
builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();

// Configure HTTP client with retry policy
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

builder.Services.AddHttpClient<IPrintServiceApiClient, PrintServiceApiClient>(client =>
{
    // BaseAddress not set here - URLs are built dynamically from configuration
    // to support runtime URL changes without restarting the application
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(retryPolicy);

// Add background polling service
builder.Services.AddHostedService<PrintJobPollingService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Create default configuration if it doesn't exist and is configured
if (!string.IsNullOrEmpty(app.Configuration["DEFAULT_PRINTER_KEY"]))
{
    var configService = app.Services.GetRequiredService<IConfigurationService>();
    if (!ConfigurationService.ConfigurationExists())
    {
        await configService.SaveAsync(new PrinterClientConfig()
        {
            ApiKey = app.Configuration["DEFAULT_PRINTER_KEY"]!,
            BackendUrl = app.Configuration["DEFAULT_BACKEND_URL"] ?? "http://localhost:8080",
            PollingIntervalSeconds = 10,
            MaxJobsPerPoll = 10
        });
    }
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

