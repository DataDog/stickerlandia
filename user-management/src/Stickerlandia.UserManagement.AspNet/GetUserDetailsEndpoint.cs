using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.GetUserDetails;

namespace Stickerlandia.UserManagement.AspNet;

public static class GetUserDetails
{
    public static async Task<ApiResponse<UserAccountDTO?>> HandleAsync(
        HttpContext context,
        ClaimsPrincipal user,
        [FromServices] IAuthService authService,
        [FromServices] GetUserDetailsQueryHandler handler)
    {
        if (user.Identity.Name == null)
        {
            return new ApiResponse<UserAccountDTO?>(false, null, "User not authenticated", HttpStatusCode.Unauthorized);
        }
        
        var result = await handler.Handle(new GetUserDetailsQuery(new AccountId(user.Identity.Name)));

        return new ApiResponse<UserAccountDTO?>(result);
    }
}