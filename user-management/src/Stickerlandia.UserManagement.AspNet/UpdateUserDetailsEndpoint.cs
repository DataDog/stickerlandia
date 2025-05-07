using System.Net;
using Microsoft.AspNetCore.Mvc;
using Stickerlandia.UserManagement.Core.Auth;
using Stickerlandia.UserManagement.Core.UpdateUserDetails;

namespace Stickerlandia.UserManagement.AspNet;

public static class UpdateUserDetailsEndpoint
{
    public static async Task<ApiResponse<string>> HandleAsync(
        HttpContext context,
        [FromServices] IAuthService authService,
        [FromServices] UpdateUserDetailsHandler updateHandler,
        [FromBody] UpdateUserDetailsRequest request)
    {
        var authHeader = context.Request.Headers["Authorization"][0];
        if (authHeader is null)
        {
            context.Response.StatusCode = 401;
            return new ApiResponse<string>(false,"Unauthorized", "Unauthorized", HttpStatusCode.Unauthorized);
        }
        
        var authorizedUser = authService.ValidateAuthToken(authHeader);
        request.AccountId = authorizedUser!.AccountId;
        
        await updateHandler.Handle(request);
        
        return new ApiResponse<string>("OK");
    }
}