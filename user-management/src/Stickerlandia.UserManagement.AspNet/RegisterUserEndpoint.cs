using Microsoft.AspNetCore.Mvc;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.RegisterUser;

namespace Stickerlandia.UserManagement.AspNet;

public static class RegisterUserEndpoint
{
    public static async Task<ApiResponse<RegisterResponse>> HandleAsync(
        [FromServices] RegisterCommandHandler registerCommandHandler,
        [FromBody] RegisterUserCommand request)
    {
        var registerResponse = await registerCommandHandler.Handle(request, AccountType.User);
        
        return new ApiResponse<RegisterResponse>(registerResponse);
    }
}