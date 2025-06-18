using System.Security.Claims;
using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Login;

namespace Stickerlandia.UserManagement.Api;

internal static class LoginEndpoint
{
    public static async Task<IResult> HandleAsync(
        IAuthService authService,
        LoginCommandHandler loginCommandHandler,
        HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(authService);
        
        var request = httpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return TypedResults.BadRequest("Invalid request.");
        }

        if (request.IsPasswordGrantType())
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return TypedResults.BadRequest("Username and password must be provided.");
            }
            var identity = await authService.VerifyPassword(request.Username, request.Password, request.GetScopes());
            
            if (identity is null)
            {
                return TypedResults.Unauthorized();
            }
            
            var signInResult = TypedResults.SignIn(new ClaimsPrincipal(identity), null,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            return signInResult;
        }

        if (request.IsClientCredentialsGrantType())
        {
            if (string.IsNullOrWhiteSpace(request.ClientId))
            {
                return TypedResults.BadRequest("Client ID must be provided.");
            }
            
            var identity = await authService.VerifyClient(request.ClientId);
            
            if (identity is null)
            {
                return TypedResults.Unauthorized();
            }
            
            var signInResult = TypedResults.SignIn(new ClaimsPrincipal(identity), null,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            
            if (signInResult is null)
            {
                return TypedResults.Unauthorized();
            }
            
            return signInResult;
        }

        return TypedResults.Unauthorized();
    }
}