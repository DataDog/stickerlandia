// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.GetUserDetails;


namespace Stickerlandia.UserManagement.FunctionApp;

public class GetUserDetailsFunction(GetUserDetailsQueryHandler handler)
{
    [Function("GetUserDetails")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "details")]
        HttpRequestData req)
    {
        // Get auth header
        var authHeader = req.Headers.GetValues("Authorization").FirstOrDefault();

        if (authHeader == null)
        {
            return await new ApiResponse<UserAccountDTO?>(false, null, "Invalid login request").WriteResponse(req, HttpStatusCode.Unauthorized);
        }

        var result = await handler.Handle(new GetUserDetailsQuery(authHeader));

        // Return successful response with token
        return await new ApiResponse<UserAccountDTO>(result).WriteResponse(req, HttpStatusCode.OK);
    }
}