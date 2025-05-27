using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Saunter;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.AspNet;
using Stickerlandia.UserManagement.AspNet.Configurations;
using Stickerlandia.UserManagement.AspNet.Middlewares;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Login;
using Stickerlandia.UserManagement.Core.RegisterUser;
using Stickerlandia.UserManagement.Auth;
using Stickerlandia.UserManagement.Azure;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

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
    .AddStickerlandiaUserManagement();

var logger = Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
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
// Add response compression for improved performance
builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });

builder.Services.AddHostedService<OutboxWorker>();
//builder.Services.AddHostedService<StickerClaimedWorker>();

builder.Services.AddControllers();
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

app
    .UseAuthentication()
    .UseAuthorization();

app.MapControllers();

var api = app.NewVersionedApi("api");
var v1ApiEndpoints = api.MapGroup("api/users/v{version:apiVersion}")
    .HasApiVersion(1.0);
v1ApiEndpoints.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
});

v1ApiEndpoints.MapGet("details", GetUserDetails.HandleAsync)
    .RequireAuthorization()
    .WithDescription("Get the current authenticated users details")
    .Produces<ApiResponse<UserAccountDTO>>(200)
    .ProducesProblem(401);

v1ApiEndpoints.MapPut("details", UpdateUserDetailsEndpoint.HandleAsync)
    .RequireAuthorization()
    .WithDescription("Update the user details")
    .Produces<ApiResponse<string>>(200)
    .ProducesProblem(401);

v1ApiEndpoints.MapPost("login", LoginEndpoint.HandleAsync)
    .AllowAnonymous()
    .WithDescription("Login")
    .Produces<ApiResponse<LoginResponse>>(200)
    .ProducesProblem(401)
    .ProducesProblem(404);

v1ApiEndpoints.MapPost("register", RegisterUserEndpoint.HandleAsync)
    .AllowAnonymous()
    .WithDescription("RegisterUser as a new user")
    .Produces<ApiResponse<RegisterResponse>>(200)
    .ProducesProblem(400);

var scope = app.Services.GetRequiredService<IServiceScopeFactory>();
using (var serviceScope = scope.CreateScope())
{
    var userStore = serviceScope.ServiceProvider.GetRequiredService<IUsers>();
    await userStore.MigrateAsync();
}

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
