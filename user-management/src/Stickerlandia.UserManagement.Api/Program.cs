using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenIddict.Validation.AspNetCore;
using Saunter;
using Serilog;
using Serilog.Formatting.Json;
using Stickerlandia.UserManagement.Api;
using Stickerlandia.UserManagement.Api.Configurations;
using Stickerlandia.UserManagement.Api.Middlewares;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.ServiceDefaults;

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

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .SetIsOriginAllowed(origin => origin == "http://localhost:8090")
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

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
        policyBuilder.RequireAuthenticatedUser();
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
        policyBuilder.RequireAuthenticatedUser();
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
