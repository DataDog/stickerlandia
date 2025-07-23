using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Stickerlandia.UserManagement.Api.Helpers;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.GetUserDetails;

namespace Stickerlandia.UserManagement.Api;

internal static class GetUserDetails
{
    public static async Task<ApiResponse<UserAccountDto?>> HandleAsync(
        HttpContext context,
        ClaimsPrincipal? user,
        [FromServices] IAuthService authService,
        [FromServices] GetUserDetailsQueryHandler handler)
    {
        if (user?.GetUserId() == null)
        {
            return new ApiResponse<UserAccountDto?>(false, null, "User not authenticated", HttpStatusCode.Unauthorized);
        }
        
        var result = await handler.Handle(new GetUserDetailsQuery(new AccountId(user.GetUserId()!)));

        return new ApiResponse<UserAccountDto?>(result);
    }
}