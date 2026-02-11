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
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;
using Log = Stickerlandia.PrintService.Core.Observability.Log;

namespace Stickerlandia.PrintService.Api.Middlewares;

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
        catch (InvalidUserException ex)
        {
            Log.GenericWarning(_logger, "User not found", ex);
            await HandleExceptionAsync(context, ex);
        }
        catch (PrinterExistsException ex)
        {
            Log.GenericWarning(_logger, "Tried to create a user that already exists", ex);
            await HandleExceptionAsync(context, ex);
        }
        catch (PrinterNotFoundException ex)
        {
            Log.GenericWarning(_logger, "Printer not found", ex);
            await HandleExceptionAsync(context, ex);
        }
        catch (InvalidPrintJobException ex)
        {
            Log.GenericWarning(_logger, "Invalid print job", ex);
            await HandleExceptionAsync(context, ex);
        }
        catch (PrintJobNotFoundException ex)
        {
            Log.GenericWarning(_logger, "Print job not found", ex);
            await HandleExceptionAsync(context, ex);
        }
        catch (PrintJobOwnershipException ex)
        {
            Log.GenericWarning(_logger, "Print job ownership mismatch", ex);
            await HandleExceptionAsync(context, ex);
        }
        catch (PrintJobStatusException ex)
        {
            Log.GenericWarning(_logger, "Print job status invalid", ex);
            await HandleExceptionAsync(context, ex);
        }
        catch (PrinterHasActiveJobsException ex)
        {
            Log.GenericWarning(_logger, "Printer has active jobs", ex);
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
        PrinterHasActiveJobsException => (int)HttpStatusCode.Conflict,
        PrinterExistsException or InvalidPrintJobException or PrintJobStatusException or ArgumentException or FormatException or ArgumentNullException => (int)HttpStatusCode.BadRequest,
        InvalidUserException or PrinterNotFoundException or PrintJobNotFoundException or KeyNotFoundException or FileNotFoundException => (int)HttpStatusCode.NotFound,
        PrintJobOwnershipException or UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
        NotImplementedException => (int)HttpStatusCode.NotImplemented,
        _ => (int)HttpStatusCode.InternalServerError
    };

    private static string GetUserFriendlyMessage(Exception exception) => exception switch
    {
        PrinterHasActiveJobsException ex => ex.Message,
        PrinterNotFoundException ex => ex.Message,
        InvalidPrintJobException ex => ex.Message,
        PrintJobNotFoundException ex => ex.Message,
        PrintJobOwnershipException ex => ex.Message,
        PrintJobStatusException ex => ex.Message,
        ArgumentException or FormatException or ArgumentNullException => "Invalid input provided",
        KeyNotFoundException or FileNotFoundException => "Requested resource not found",
        UnauthorizedAccessException => "Unauthorized access",
        NotImplementedException => "This functionality is not yet implemented",
        _ => "An unexpected error occurred. Please try again later."
    };
}