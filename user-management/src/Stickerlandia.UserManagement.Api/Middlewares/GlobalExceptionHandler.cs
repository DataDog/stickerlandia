/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */


// Catching generic exceptions is not recommended, but in this case we want to catch all exceptions so that a failure in outbox processing does not crash the application and we can return a 500 error.
#pragma warning disable CA1031
// This is a global exception handler that is not intended instantiated directly, so we suppress the warning.
#pragma warning disable CA1812


using System.Net;
using System.Text.Json;
using Stickerlandia.UserManagement.Core;
using Log = Stickerlandia.UserManagement.Core.Observability.Log;

namespace Stickerlandia.UserManagement.Api.Middlewares;

internal sealed class GlobalExceptionHandler
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        try
        {
            await _next(context);
        }
        catch (LoginFailedException ex)
        {
            Log.GenericWarning(_logger, "Login failed", ex);
            await HandleExceptionAsync(context, ex);
        }
        catch (InvalidUserException ex)
        {
            Log.GenericWarning(_logger, "User not found", ex);
            await HandleExceptionAsync(context, ex);
        }
        catch (UserExistsException ex)
        {
            Log.GenericWarning(_logger, "Tried to create a user that already exists", ex);
            await HandleExceptionAsync(context, ex);
        }
        catch (ArgumentNullException ex)
        {
            Log.GenericWarning(_logger, "Failed to retrieve user details", ex);   
            await HandleExceptionAsync(context, ex);
        }
        catch (ArgumentException ex)
        {
            Log.GenericWarning(_logger, "Failed to retrieve user details", ex);   
            await HandleExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            Log.GenericWarning(_logger, "An unhandled exception occurred during request processing", ex);
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