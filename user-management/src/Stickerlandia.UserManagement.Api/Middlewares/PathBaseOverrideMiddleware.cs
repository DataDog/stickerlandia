// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// Catching generic exceptions is not recommended, but in this case we want to catch all exceptions so that a failure in outbox processing does not crash the application and we can return a 500 error.

using System.Diagnostics;

#pragma warning disable CA1031
// This is a global exception handler that is not intended instantiated directly, so we suppress the warning.
#pragma warning disable CA1812

namespace Stickerlandia.UserManagement.Api.Middlewares;

internal sealed class PathBaseOverrideMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ActivitySource _activitySource;

    public PathBaseOverrideMiddleware(RequestDelegate next, IConfiguration configuration, ActivitySource activitySource)
    {
        _next = next;
        _configuration = configuration;
        _activitySource = activitySource;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using (var middlewareActivity = _activitySource.StartActivity("PathBaseOverrideMiddleware.InvokeAsync"))
        {
            middlewareActivity?.AddTag("path.base", context.Request.PathBase);

            context.Request.PathBase = _configuration["DEPLOYMENT_HOST_URL"] ?? context.Request.PathBase;

            middlewareActivity?.AddTag("path.updated", context.Request.PathBase);
        }

        await _next.Invoke(context);
    }
}