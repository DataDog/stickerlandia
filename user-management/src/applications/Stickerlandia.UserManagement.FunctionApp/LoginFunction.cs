// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Stickerlandia.UserManagement.Core.Login;

namespace Stickerlandia.UserManagement.FunctionApp;

public class LoginFunction(
    LoginCommandHandler loginCommandHandler,
    ILogger<LoginFunction> logger)
{
    [Function("Login")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/v1/login")]
        HttpRequestData req)
    {
        logger.LogInformation("Processing login request");
        
        // Deserialize request
        var loginRequest = await JsonSerializer.DeserializeAsync<LoginCommand>(req.Body);

        if (loginRequest == null)
        {
            return await new ApiResponse<LoginResponse?>(false, null, "Invalid login request").WriteResponse(req, HttpStatusCode.BadRequest);
        }

        var result = await loginCommandHandler.Handle(loginRequest);

        // Return successful response with token
        return await new ApiResponse<LoginResponse>(result).WriteResponse(req, HttpStatusCode.OK);
    }
}