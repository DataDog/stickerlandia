using System.Net;
using Microsoft.AspNetCore.Mvc;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.GetUserDetails;

namespace Stickerlandia.UserManagement.AspNet;

public static class GetUserDetails
{
    public static async Task<ApiResponse<UserAccountDTO?>> HandleAsync(
        HttpContext context,
        [FromServices] GetUserDetailsQueryHandler handler)
    {
        var authHeader = context.Request.Headers["Authorization"][0];
        if (authHeader is null)
        {
            context.Response.StatusCode = 401;
            return new ApiResponse<UserAccountDTO?>(false, null, "Unauthorized", HttpStatusCode.Unauthorized);
        }

        var result = await handler.Handle(new GetUserDetailsQuery(authHeader));

        return new ApiResponse<UserAccountDTO?>(result);
    }
}