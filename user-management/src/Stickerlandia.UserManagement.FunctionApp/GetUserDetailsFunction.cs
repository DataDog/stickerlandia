// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.GetUserDetails;


namespace Stickerlandia.UserManagement.FunctionApp;

public class GetUserDetailsFunction(GetUserDetailsQueryHandler handler, IAuthService authService)
{
    [Function("GetUserDetails")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/v1/details")]
        HttpRequestData req)
    {
        var result = await handler.Handle(new GetUserDetailsQuery(new AccountId("a-random-account-id")));

        // Return successful response with token
        return await new ApiResponse<UserAccountDTO>(result).WriteResponse(req, HttpStatusCode.OK);
    }
}