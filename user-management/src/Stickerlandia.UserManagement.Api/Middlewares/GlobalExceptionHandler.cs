using System.Net;
using System.Text.Json;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.Api.Middlewares;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (LoginFailedException ex)
        {
            _logger.LogWarning(ex, "Login failed");
            await HandleExceptionAsync(context, ex);
        }
        catch (InvalidUserException ex)
        {
            _logger.LogWarning(ex, "User not found");
            await HandleExceptionAsync(context, ex);
        }
        catch (UserExistsException ex)
        {
            _logger.LogWarning(ex, "Tried to create a user that already exists");
            await HandleExceptionAsync(context, ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "failed to retrieve user details");
            await HandleExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during request processing");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            Status = "Error",
            Message = GetUserFriendlyMessage(exception)
        };

        context.Response.StatusCode = DetermineStatusCode(exception);

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private static int DetermineStatusCode(Exception exception) => exception switch
    {
        LoginFailedException => (int)HttpStatusCode.Unauthorized,
        UserExistsException or ArgumentException or FormatException or ArgumentNullException => (int)HttpStatusCode.BadRequest,
        InvalidUserException or KeyNotFoundException or FileNotFoundException => (int)HttpStatusCode.NotFound,
        UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
        NotImplementedException => (int)HttpStatusCode.NotImplemented,
        _ => (int)HttpStatusCode.InternalServerError
    };
    
    private static string GetUserFriendlyMessage(Exception exception) => exception switch
    {
        ArgumentException or FormatException or ArgumentNullException => "Invalid input provided",
        KeyNotFoundException or FileNotFoundException => "Requested resource not found",
        UnauthorizedAccessException => "Unauthorized access",
        NotImplementedException => "This functionality is not yet implemented",
        _ => "An unexpected error occurred. Please try again later."
    };
}