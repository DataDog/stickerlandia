// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Collections.Immutable;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.RegisterUser;
using Stickerlandia.UserManagement.Core.UpdateUserDetails;

namespace Stickerlandia.UserManagement.FunctionApp;

public class UpdateUserDetailsFunction(UpdateUserDetailsHandler updateHandler, IAuthService authService)
{
    [Function("UpdateUserDetails")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/v1/details")]
        HttpRequestData req)
    {
        // Get auth header
        var authHeader = req.Headers.GetValues("Authorization").FirstOrDefault();

        if (authHeader == null)
        {
            return await new ApiResponse<UserAccountDTO?>(false, null, "Invalid login request").WriteResponse(req, HttpStatusCode.Unauthorized);
        }
        
        var authorizedUser = authService.VerifyPassword("", "", new ImmutableArray<string>());
        
        // Deserialize request
        var updateUserDetailsRequest = await JsonSerializer.DeserializeAsync<UpdateUserDetailsRequest>(req.Body);
        
        if (updateUserDetailsRequest == null)
        {
            return await new ApiResponse<RegisterUserCommand?>(false, null, "Invalid login request").WriteResponse(req, HttpStatusCode.BadRequest);
        }
        
        updateUserDetailsRequest.AccountId = new AccountId("a-random-account-id");
        await updateHandler.Handle(updateUserDetailsRequest);

        // Return successful response with token
        return await new ApiResponse<string>("OK").WriteResponse(req, HttpStatusCode.OK);
    }
}