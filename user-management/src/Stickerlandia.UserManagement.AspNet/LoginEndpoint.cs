using Microsoft.AspNetCore.Mvc;
using Stickerlandia.UserManagement.Core.Login;

namespace Stickerlandia.UserManagement.AspNet;

public static class LoginEndpoint
{
    public static async Task<ApiResponse<LoginResponse>> HandleAsync(
        [FromServices] LoginCommandHandler loginCommandHandler,
        [FromBody] LoginCommand request)
    {
        var loginResponse = await loginCommandHandler.Handle(request);
        
        return new ApiResponse<LoginResponse>(loginResponse);
    }
}