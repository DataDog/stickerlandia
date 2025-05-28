using System.Security.Claims;
using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Login;

namespace Stickerlandia.UserManagement.Api;

public static class LoginEndpoint
{
    public static async Task<IResult> HandleAsync(
        IAuthService authService,
        LoginCommandHandler loginCommandHandler,
        HttpContext httpContext)
    {
        var request = httpContext.GetOpenIddictServerRequest();

        if (request.IsPasswordGrantType())
        {
            var identity = await authService.VerifyPassword(request.Username, request.Password, request.GetScopes());
            var signInResult = TypedResults.SignIn(new ClaimsPrincipal(identity), null,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            return signInResult;
        }

        if (request.IsClientCredentialsGrantType())
        {
            var identity = await authService.VerifyClient(request.ClientId);
            var signInResult = TypedResults.SignIn(new ClaimsPrincipal(identity), null,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            return signInResult;
        }

        return TypedResults.Unauthorized();
    }
}