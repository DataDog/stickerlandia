/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Saunter;
using Serilog;
using Serilog.Formatting.Json;
using Stickerlandia.PrintService.Api;
using Stickerlandia.PrintService.Api.Configurations;
using Stickerlandia.PrintService.Api.Middlewares;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;
using Stickerlandia.PrintService.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var logger = Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.AddDocumentationEndpoints();

builder.Services
    .AddHealthChecks();

// Add API versioning
builder.Services.AddProblemDetails()
    .AddEndpointsApiExplorer()
    .AddApiVersioning();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Request.Headers.Host.ToString(),
            partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });

// Configure authentication (supports OIDC discovery or symmetric key modes)
builder.Services.AddPrintServiceAuthentication(builder.Configuration);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Use our public URL to access the service if we have been configured.
// This avoids relying on X-Forwarded-For and HTTP headers to work out
// where e.g. redirects should go.
var publicBaseUrl = builder.Configuration["DEPLOYMENT_HOST_URL"];
if (!string.IsNullOrWhiteSpace(publicBaseUrl))
{
    var publicBase = new Uri(publicBaseUrl);

    app.Use(async (ctx, next) =>
    {
        // Normalize request host/scheme for the rest of the pipeline
        ctx.Request.Scheme = publicBase.Scheme;

        // publicBase.Authority may contain host[:port]
        ctx.Request.Host = new HostString(publicBase.Authority);

        await next();
    });
}

app.UseRateLimiter();
app.UseMiddleware<GlobalExceptionHandler>();

// Enable Swagger UI
app.UseSwagger();
app.MapAsyncApiDocuments();

if (app.Environment.IsDevelopment())
{
    app.MapAsyncApiUi();
    app.UseSwaggerUI(options =>
    {
        var url = $"/swagger/v1/swagger.yaml";
        var name = "V1";
        options.SwaggerEndpoint(url, name);
    });
}

app.UseCors("AllowAll");

app.UseRouting();
app.UseStaticFiles();

app
    .UseAuthentication()
    .UseAuthorization();

app.MapRazorPages();
app.MapControllers();

var api = app.NewVersionedApi("api");
var v1ApiEndpoints = api.MapGroup("api/print/v{version:apiVersion}")
    .HasApiVersion(1.0);
v1ApiEndpoints.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
});

v1ApiEndpoints.MapGet("/event/{eventName}", GetPrintersForEvent.HandleAsync)
    .RequireAuthorization(policyBuilder =>
    {
        policyBuilder.RequireAuthenticatedUser()
            .RequireRole("admin", "user");
    })
    .WithDescription("Get all registered printers for an event")
    .Produces<ApiResponse<PrinterDTO>>(200)
    .ProducesProblem(401)
    .ProducesProblem(403);

v1ApiEndpoints.MapPost("/event/{eventName}", RegisterPrinterEndpoint.HandleAsync)
    .RequireAuthorization(policyBuilder =>
    {
        policyBuilder.RequireAuthenticatedUser()
            .RequireRole("admin");
    })
    .WithDescription("Register a new printer for an event")
    .Produces<ApiResponse<string>>(200)
    .ProducesProblem(401)
    .ProducesProblem(403);

v1ApiEndpoints.MapPost("/event/{eventName}/printer/{printerName}/jobs", SubmitPrintJobEndpoint.HandleAsync)
    .RequireAuthorization(policyBuilder =>
    {
        policyBuilder.RequireAuthenticatedUser()
            .RequireRole("admin", "user");
    })
    .WithDescription("Submit a print job to a printer")
    .Produces<ApiResponse<SubmitPrintJobResponse>>(201)
    .ProducesProblem(400)
    .ProducesProblem(401)
    .ProducesProblem(403)
    .ProducesProblem(404);

// Printer client endpoints (API Key authentication)
v1ApiEndpoints.MapGet("/printer/jobs", PollPrintJobsEndpoint.HandleAsync)
    .RequireAuthorization(policyBuilder =>
    {
        policyBuilder.AddAuthenticationSchemes(Stickerlandia.PrintService.Api.Configurations.PrinterKeyAuthenticationHandler.SchemeName)
            .RequireAuthenticatedUser();
    })
    .WithDescription("Poll for print jobs (printer client)")
    .Produces<ApiResponse<PollPrintJobsResponse>>(200)
    .Produces(204)
    .ProducesProblem(401);

try
{
    await app.StartAsync();

    var urlList = app.Urls;
    var urls = string.Join(" ", urlList);

    logger.Information("UserManagement API started on {Urls}", urls);
}
catch (Exception ex)
{
    logger.Error(ex, "Error starting the application");
    throw;
}

await app.WaitForShutdownAsync();

static Task WriteHealthCheckResponse(HttpContext context, HealthReport healthReport)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var options = new JsonWriterOptions { Indented = true };

    using var memoryStream = new MemoryStream();
    using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
    {
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("status", healthReport.Status.ToString());
        jsonWriter.WriteStartObject("results");

        foreach (var healthReportEntry in healthReport.Entries)
        {
            jsonWriter.WriteStartObject(healthReportEntry.Key);
            jsonWriter.WriteString("status",
                healthReportEntry.Value.Status.ToString());
            jsonWriter.WriteString("description",
                healthReportEntry.Value.Description);
            jsonWriter.WriteStartObject("data");

            foreach (var item in healthReportEntry.Value.Data)
            {
                jsonWriter.WritePropertyName(item.Key);

                JsonSerializer.Serialize(jsonWriter, item.Value,
                    item.Value?.GetType() ?? typeof(object));
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndObject();
    }

    return context.Response.WriteAsync(
        Encoding.UTF8.GetString(memoryStream.ToArray()));
}
