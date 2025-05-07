// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Net;
using Datadog.Trace;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.FunctionApp.Middlewares;

public class ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (LoginFailedException ex)
        {
            logger.LogWarning(ex, "Login failed");
            await ProcessError(ex, context,
                new ApiResponse<string>(false, "Error", "Login failed", HttpStatusCode.Unauthorized));
        }
        catch (InvalidUserException ex)
        {
            logger.LogWarning(ex, "User not found");
            await ProcessError(ex, context,
                new ApiResponse<string>(false, "Error", "User not found", HttpStatusCode.NotFound));
        }
        catch (UserExistsException ex)
        {
            logger.LogWarning(ex, "Tried to create a user that already exists");
            await ProcessError(ex, context, new ApiResponse<string>(false, "Error", "User exists", HttpStatusCode.BadRequest));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "failed to retrieve user details");
            await ProcessError(ex, context, new ApiResponse<string>(false, "Error", "Invalid request, check your input parameters", HttpStatusCode.BadRequest));
        }
        catch (Exception ex)
        {
            var activeSpan = Tracer.Instance.ActiveScope?.Span;
            activeSpan?.SetException(ex);
            activeSpan?.SetTag("error", "true");
            activeSpan?.SetTag("error.message", ex.Message);
            logger.LogError(ex, $"Unknown error: {ex.Message} : {ex.StackTrace}");
            await ProcessError(ex, context, new ApiResponse<string>(false, "Error", "Unknown error"));
        }
    }

    private async Task ProcessError(Exception ex, FunctionContext context, ApiResponse<string> apiResponse)
    {
        logger.LogError(ex, "Error processing invocation");

        var httpReqData = await context.GetHttpRequestDataAsync();

        if (httpReqData != null)
        {
            var newHttpResponse = httpReqData.CreateResponse();
            await apiResponse.WriteResponse(httpReqData, HttpStatusCode.InternalServerError);

            var invocationResult = context.GetInvocationResult();

            var httpOutputBindingFromMultipleOutputBindings = GetHttpOutputBindingFromMultipleOutputBinding(context);
            if (httpOutputBindingFromMultipleOutputBindings is not null)
            {
                httpOutputBindingFromMultipleOutputBindings.Value = newHttpResponse;
            }
            else
            {
                invocationResult.Value = newHttpResponse;
            }
        }
    }
    
    private static OutputBindingData<HttpResponseData>? GetHttpOutputBindingFromMultipleOutputBinding(FunctionContext context)
    {
        // The output binding entry name will be "$return" only when the function return type is HttpResponseData
        var httpOutputBinding = context.GetOutputBindings<HttpResponseData>()
            .FirstOrDefault(b => b.BindingType == "http" && b.Name != "$return");

        return httpOutputBinding;
    }
}