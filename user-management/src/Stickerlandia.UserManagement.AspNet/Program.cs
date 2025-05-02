using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Saunter;
using Saunter.AsyncApiSchema.v2;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Json;
using Stickerlandia.UserManagement.AspNet;
using Stickerlandia.UserManagement.Azure;
using Stickerlandia.UserManagement.AspNet.Configurations;
using Stickerlandia.UserManagement.AspNet.Middlewares;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Login;
using Stickerlandia.UserManagement.Core.RegisterUser;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddLogging();

var logger = Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
        .Enrich.FromLogContext()
        .WriteTo.Console(new JsonFormatter())
    .CreateLogger();
builder.Host.UseSerilog((_, config) =>
{
    config.MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(new JsonFormatter());
});

var appLogger = new SerilogLoggerFactory(logger)
    .CreateLogger<Program>();

builder.AddAzureAdapters();

builder.Services
    .AddAuthConfigs(appLogger, builder)
    .AddStickerlandiaUserManagement()
    .AddHealthChecks();
// Add API versioning
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning();

builder.Services.AddAsyncApiSchemaGeneration(options =>
{
    options.AssemblyMarkerTypes = new[] { typeof(ServiceBusEventPublisher) };
    options.Middleware.UiTitle = "Users API";
    options.AsyncApi = new AsyncApiDocument
    {
        Info = new Info("Users Service", "1.0.0")
    };
});

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
builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddHostedService<StickerClaimedWorker>();

// Add API documentation
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "User Management API", Version = "v1" });

    // Include XML comments for Swagger
    var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
    foreach (var xmlFile in xmlFiles) options.IncludeXmlComments(xmlFile);
});

// Add response compression for improved performance
builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
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
    app.MapAsyncApiUi();
    app.UseSwaggerUI(options =>
    {
        var url = $"/swagger/v1/swagger.yaml";
        var name = "V1";
        options.SwaggerEndpoint(url, name);
    });

app.UseCors("AllowAll");

app
    .UseAuthentication()
    .UseAuthorization();

var api = app.NewVersionedApi("api");
var v1 = api.MapGroup("api/users/v{version:apiVersion}")
    .HasApiVersion(1.0);
v1.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
});


v1.MapGet("details", GetUserDetails.HandleAsync)
    .RequireAuthorization()
    .WithDescription("Get the current authenticated users details")
    .Produces<ApiResponse<UserAccountDTO>>(200)
    .ProducesProblem(401);

v1.MapPut("details", UpdateUserDetailsEndpoint.HandleAsync)
    .RequireAuthorization()
    .WithDescription("Update the user details")
    .Produces<ApiResponse<string>>(200)
    .ProducesProblem(401);

v1.MapPost("login", LoginEndpoint.HandleAsync)
    .AllowAnonymous()
    .WithDescription("Login")
    .Produces<ApiResponse<LoginResponse>>(200)
    .ProducesProblem(401)
    .ProducesProblem(404);

v1.MapPost("register", RegisterUserEndpoint.HandleAsync)
    .AllowAnonymous()
    .WithDescription("RegisterUser as a new user")
    .Produces<ApiResponse<RegisterResponse>>(200)
    .ProducesProblem(400);

var database = app.Services.GetRequiredService<IUsers>();
await database.MigrateAsync();

await app.RunAsync();

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
