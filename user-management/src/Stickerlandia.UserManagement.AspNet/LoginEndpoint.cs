using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Stickerlandia.UserManagement.Core.Login;

namespace Stickerlandia.UserManagement.AspNet;

[HttpPost("/login")]
[AllowAnonymous]
public class LoginEndpoint(LoginCommandHandler loginCommandHandler)
    : Endpoint<LoginCommand, ApiResponse<LoginResponse>?>
{
    public override async Task<LoginResponse?> HandleAsync(
        LoginCommand request,
        CancellationToken ct)
    {
        var loginResponse = await loginCommandHandler.Handle(request);
            
        Response = new ApiResponse<LoginResponse>(loginResponse);
        return loginResponse;
    }
}