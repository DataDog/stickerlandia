using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Register;

namespace Stickerlandia.UserManagement.AspNet;

[HttpPost("/register")]
[AllowAnonymous]
public class RegisterUserEndpoint(RegisterCommandHandler registerCommandHandler)
    : Endpoint<RegisterUserCommand, RegisterResponse?>
{
    public override async Task<RegisterResponse?> HandleAsync(
        RegisterUserCommand request,
        CancellationToken ct)
    {
        var registerResponse = await registerCommandHandler.Handle(request, AccountType.User, ct);

        Response = registerResponse;
        return registerResponse;
    }
}