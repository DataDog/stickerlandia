using System.Net;
using Microsoft.AspNetCore.Mvc;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Auth;
using Stickerlandia.UserManagement.Core.GetUserDetails;

namespace Stickerlandia.UserManagement.AspNet;

public static class GetUserDetails
{
    public static async Task<ApiResponse<UserAccountDTO?>> HandleAsync(
        HttpContext context,
        [FromServices] IAuthService authService,
        [FromServices] GetUserDetailsQueryHandler handler)
    {
        var authHeader = context.Request.Headers["Authorization"][0];
        if (authHeader is null)
        {
            context.Response.StatusCode = 401;
            return new ApiResponse<UserAccountDTO?>(false, null, "Unauthorized", HttpStatusCode.Unauthorized);
        }
        
        var authorizedUser = authService.ValidateAuthToken(authHeader);

        var result = await handler.Handle(new GetUserDetailsQuery(authorizedUser.AccountId));

        return new ApiResponse<UserAccountDTO?>(result);
    }
}