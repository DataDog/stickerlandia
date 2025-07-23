using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Stickerlandia.UserManagement.Api.Helpers;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.UpdateUserDetails;

namespace Stickerlandia.UserManagement.Api;

internal static class UpdateUserDetailsEndpoint
{
    public static async Task<ApiResponse<string>> HandleAsync(
        string userId,
        HttpContext context,
        ClaimsPrincipal? user,
        [FromServices] IAuthService authService,
        [FromServices] UpdateUserDetailsHandler updateHandler,
        [FromBody] UpdateUserDetailsRequest request)
    {
        if (user?.GetUserId() == null)
        {
            return new ApiResponse<string>(false, "", "User not authenticated", HttpStatusCode.Unauthorized);
        }

        var jwtUserId = user.GetUserId();
        if (jwtUserId != userId)
        {
            return new ApiResponse<string>(false, "", "Access denied: userId parameter does not match authenticated user", HttpStatusCode.Forbidden);
        }

        request.AccountId = new AccountId(user?.GetUserId()!);
        
        await updateHandler.Handle(request);
        
        return new ApiResponse<string>("OK");
    }
}