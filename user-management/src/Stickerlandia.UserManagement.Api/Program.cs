/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenIddict.Validation.AspNetCore;
using Saunter;
using Serilog;
using Serilog.Formatting.Json;
using Stickerlandia.UserManagement.Api;
using Stickerlandia.UserManagement.Api.Configurations;
using Stickerlandia.UserManagement.Api.Middlewares;
using Stickerlandia.UserManagement.Core;
using LogMessages = Stickerlandia.UserManagement.Core.Observability.Log;
using Stickerlandia.UserManagement.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

if (EF.IsDesignTime)
{
    await builder.Build().RunAsync();
    return;
}

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
    // Log when requests are rejected due to rate limiting
    options.OnRejected = async (context, cancellationToken) =>
    {
        var requestLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var retryAfter = 60; // Default retry after 60 seconds
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterTime))
        {
            retryAfter = (int)retryAfterTime.TotalSeconds;
        }

        context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();

        // Get client IP from X-Forwarded-For (behind LB) or RemoteIpAddress
        var forwardedFor = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var clientIp = !string.IsNullOrEmpty(forwardedFor)
            ? forwardedFor.Split(',')[0].Trim()
            : context.HttpContext.Connection.RemoteIpAddress?.ToString();

        LogMessages.RateLimitExceeded(
            requestLogger,
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            clientIp,
            retryAfter,
            context.HttpContext.Request.Headers.Host.ToString());

        await context.HttpContext.Response.WriteAsync(
            $"Too many requests. Please retry after {retryAfter} seconds.",
            cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // Partition by client IP for better isolation between clients
        // Check X-Forwarded-For first (for requests behind LB/proxy), then fall back to RemoteIpAddress
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var clientIp = !string.IsNullOrEmpty(forwardedFor)
            ? forwardedFor.Split(',')[0].Trim()  // Take first IP in chain (original client)
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,    
                QueueLimit = 10,      // Allow some queuing instead of instant rejection
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
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

app.UseRouting();
app.UseStaticFiles();

app
    .UseAuthentication()
    .UseAuthorization();

app.MapRazorPages();
app.MapControllers();

var api = app.NewVersionedApi("api");
var v1ApiEndpoints = api.MapGroup("api/users/v{version:apiVersion}")
    .HasApiVersion(1.0);
v1ApiEndpoints.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
});

v1ApiEndpoints.MapGet("{userId}/details", GetUserDetails.HandleAsync)
    .RequireAuthorization(policyBuilder =>
    {
        policyBuilder.AuthenticationSchemes = new List<string>(1)
            { OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme };
        policyBuilder.RequireAuthenticatedUser()
            .RequireRole(UserTypes.Admin, UserTypes.User);
    })
    .WithDescription("Get the current authenticated users details")
    .Produces<ApiResponse<UserAccountDto>>(200)
    .ProducesProblem(401)
    .ProducesProblem(403);

v1ApiEndpoints.MapPut("{userId}/details", UpdateUserDetailsEndpoint.HandleAsync)
    .RequireAuthorization(policyBuilder =>
    {
        policyBuilder.AuthenticationSchemes = new List<string>(1)
            { OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme };
        policyBuilder.RequireAuthenticatedUser()
            .RequireRole(UserTypes.Admin, UserTypes.User);
    })
    .WithDescription("Update the user details")
    .Produces<ApiResponse<string>>(200)
    .ProducesProblem(401)
    .ProducesProblem(403);

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
