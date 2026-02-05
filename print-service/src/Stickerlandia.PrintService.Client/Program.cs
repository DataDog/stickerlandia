// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Formatting.Json;
using Stickerlandia.PrintService.Client.Components;
using Stickerlandia.PrintService.Client.Configuration;
using Stickerlandia.PrintService.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

