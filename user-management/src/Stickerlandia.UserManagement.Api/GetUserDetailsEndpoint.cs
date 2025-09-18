/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Stickerlandia.UserManagement.Api.Helpers;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.GetUserDetails;

namespace Stickerlandia.UserManagement.Api;

internal static class GetUserDetails
{
    public static async Task<IResult> HandleAsync(
        string userId,
        HttpContext context,
        ClaimsPrincipal? user,
        [FromServices] IAuthService authService,
        [FromServices] GetUserDetailsQueryHandler handler)
    {
        if (user?.GetUserId() == null)
        {
            return Results.Forbid();
        }

        var jwtUserId = user.GetUserId();
        if (jwtUserId != userId)
        {
            return Results.Forbid();
        }
        
        var result = await handler.Handle(new GetUserDetailsQuery(new AccountId(user.GetUserId()!)));

        return Results.Ok(new ApiResponse<UserAccountDto>(result));
    }
}