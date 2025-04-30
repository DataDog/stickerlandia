// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.RegisterUser;

namespace Stickerlandia.UserManagement.FunctionApp;

public class RegisterUserFunction(RegisterCommandHandler registerCommandHandler)
{
    [Function("RegisterUser")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/v1/register")]
        HttpRequestData req)
    {
        // Deserialize request
        var registerUserRequest = await JsonSerializer.DeserializeAsync<RegisterUserCommand>(req.Body);
        
        if (registerUserRequest == null)
        {
            return await new ApiResponse<RegisterUserCommand?>(false, null, "Invalid login request").WriteResponse(req, HttpStatusCode.BadRequest);
        }

        var result = await registerCommandHandler.Handle(registerUserRequest, AccountType.User);

        // Return successful response with token
        return await new ApiResponse<RegisterResponse>(result).WriteResponse(req, HttpStatusCode.OK);
    }
}