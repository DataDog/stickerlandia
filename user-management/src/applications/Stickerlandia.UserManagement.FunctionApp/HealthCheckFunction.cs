// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Stickerlandia.UserManagement.FunctionApp;

public class HealthCheckFunction(ILogger<HealthCheckFunction> logger)
{
    private readonly ILogger<HealthCheckFunction> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [Function("HealthCheck")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Processing health check");

        return await new ApiResponse<HealthCheckResponse>(new HealthCheckResponse(){Status = "OK"}).WriteResponse(req, HttpStatusCode.OK);
    }
}
public class HealthCheckResponse
{
    public string Status { get; set; } = "";
}