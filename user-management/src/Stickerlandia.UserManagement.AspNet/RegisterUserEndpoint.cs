using Microsoft.AspNetCore.Mvc;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Register;

namespace Stickerlandia.UserManagement.AspNet;

public static class RegisterUserEndpoint
{
    public static async Task<ApiResponse<RegisterResponse>> HandleAsync(
        [FromServices] RegisterCommandHandler registerCommandHandler,
        RegisterUserCommand request,
        CancellationToken ct)
    {
        var registerResponse = await registerCommandHandler.Handle(request, AccountType.User, ct);
        
        return new ApiResponse<RegisterResponse>(registerResponse);
    }
}