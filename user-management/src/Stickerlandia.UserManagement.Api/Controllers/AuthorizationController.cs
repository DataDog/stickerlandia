/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1515, CA5391

using System.Security.Claims;
using Datadog.Trace;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Stickerlandia.UserManagement.Api.Helpers;
using Stickerlandia.UserManagement.Api.ViewModels;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.Api.Controllers;

/// <summary>
/// The implementation of the Authorization controller comes from samples provided by OpenIddict.
/// https://github.com/openiddict/openiddict-samples/tree/dev/samples/Balosar/Balosar.Server
/// </summary>
public class AuthorizationController(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictScopeManager scopeManager,
    SignInManager<PostgresUserAccount> signInManager,
    UserManager<PostgresUserAccount> userManager)
    : Controller
{
    /// <summary>
    /// This action is invoked when a GET/POST is sent to the authorization endpoint
    /// (e.g when the user agent is redirected to the authorization endpoint by the client application
    /// The IgnoreAntiforgeryToken attribute is used to disable the CSRF protection for this endpoint,
    /// as the connect endpoints are going to be called by an external client, which won't be able to provide an anti-forgery token.
    /// </summary>
    [HttpGet("~/api/users/v1/connect/authorize")]
    [HttpPost("~/api/users/v1/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var activeSpan = Tracer.Instance.ActiveScope?.Span;

        var request = HttpContext.GetOpenIddictServerRequest() ??
              throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var result = await HttpContext.AuthenticateAsync();
        if (result is not { Succeeded: true } ||
            ((request.HasPromptValue(OpenIddictConstants.PromptValues.Login) || request.MaxAge is 0 ||
              (request.MaxAge is not null && result.Properties?.IssuedUtc is not null &&
               TimeProvider.System.GetUtcNow() - result.Properties.IssuedUtc >
               TimeSpan.FromSeconds(request.MaxAge.Value))) &&
             TempData["IgnoreAuthenticationChallenge"] is null or false))
        {
            activeSpan?.SetTag("user.authenticated", "false");
            // If the client application requested promptless authentication,
            // return an error indicating that the user is not logged in.
            if (request.HasPromptValue(OpenIddictConstants.PromptValues.None))
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                            OpenIddictConstants.Errors.LoginRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in."
                    }));

            // To avoid endless login endpoint -> authorization endpoint redirects, a special temp data entry is
            // used to skip the challenge if the user agent has already been redirected to the login endpoint.
            TempData["IgnoreAuthenticationChallenge"] = true;

            var redirectUri = Request.PathBase + Request.Path + QueryString.Create(
                Request.HasFormContentType ? Request.Form : Request.Query);

            activeSpan?.SetTag("http.redirect_uri", redirectUri);

            return Challenge(new AuthenticationProperties
            {
                RedirectUri = redirectUri,
            });
        }

        // Retrieve the profile of the logged in user.
        var user = await userManager.GetUserAsync(result.Principal) ??
                   throw new InvalidOperationException("The user details cannot be retrieved.");

        // Retrieve the application details from the database.
        var application = await applicationManager.FindByClientIdAsync(request.ClientId!) ??
                          throw new InvalidOperationException(
                              "Details concerning the calling client application cannot be found.");

        activeSpan?.SetTag("user.authenticated", "true");
        activeSpan?.SetTag("user.id", user.Id);
        activeSpan?.SetTag("client.id", request.ClientId!);

        // Retrieve the permanent authorizations associated with the user and the calling client application.
        var authorizations = await authorizationManager.FindAsync(
            await userManager.GetUserIdAsync(user),
            await applicationManager.GetIdAsync(application),
            OpenIddictConstants.Statuses.Valid,
            OpenIddictConstants.AuthorizationTypes.Permanent,
            request.GetScopes()).ToListAsync();

        switch (await applicationManager.GetConsentTypeAsync(application))
        {
            // If the consent is external (e.g when authorizations are granted by a sysadmin),
            // immediately return an error if no authorization can be found in the database.
            case OpenIddictConstants.ConsentTypes.External when authorizations.Count is 0:
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                            OpenIddictConstants.Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The logged in user is not allowed to access this client application."
                    }));

            // If the consent is implicit or if an authorization was found,
            // return an authorization response without displaying the consent form.
            case OpenIddictConstants.ConsentTypes.Implicit:
            case OpenIddictConstants.ConsentTypes.External when authorizations.Count is not 0:
            case OpenIddictConstants.ConsentTypes.Explicit when authorizations.Count is not 0 &&
                                                                !request.HasPromptValue(OpenIddictConstants.PromptValues
                                                                    .Consent):
                // Create the claims-based identity that will be used by OpenIddict to generate tokens.
                var identity = new ClaimsIdentity(
                    TokenValidationParameters.DefaultAuthenticationType,
                    OpenIddictConstants.Claims.Name,
                    OpenIddictConstants.Claims.Role);

                // Add the claims that will be persisted in the tokens.
                identity.SetClaim(OpenIddictConstants.Claims.Subject, await userManager.GetUserIdAsync(user))
                    .SetClaim(OpenIddictConstants.Claims.Email, await userManager.GetEmailAsync(user))
                    .SetClaim(OpenIddictConstants.Claims.Name, $"{user.FirstName} {user.LastName}")
                    .SetClaim(OpenIddictConstants.Claims.GivenName, user.FirstName)
                    .SetClaim(OpenIddictConstants.Claims.FamilyName, user.LastName)
                    .SetClaim(OpenIddictConstants.Claims.PreferredUsername, await userManager.GetUserNameAsync(user))
                    .SetClaim("signup_date", user.DateCreated.ToString("o"))
                    .SetClaims(OpenIddictConstants.Claims.Role, [.. await userManager.GetRolesAsync(user)]);

                identity.SetScopes(request.GetScopes());
                identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());
                identity.SetAudiences("stickerlandia");

                // Automatically create a permanent authorization to avoid requiring explicit consent
                // for future authorization or token requests containing the same scopes.
                var authorization = authorizations.LastOrDefault();
                authorization ??= await authorizationManager.CreateAsync(
                    identity,
                    await userManager.GetUserIdAsync(user),
                    (await applicationManager.GetIdAsync(application))!,
                    OpenIddictConstants.AuthorizationTypes.Permanent,
                    identity.GetScopes());

                identity.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));
                identity.SetDestinations(GetDestinations);

                return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // At this point, no authorization was found in the database and an error must be returned
            // if the client application specified prompt=none in the authorization request.
            case OpenIddictConstants.ConsentTypes.Explicit
                when request.HasPromptValue(OpenIddictConstants.PromptValues.None):
            case OpenIddictConstants.ConsentTypes.Systematic
                when request.HasPromptValue(OpenIddictConstants.PromptValues.None):
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                            OpenIddictConstants.Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Interactive user consent is required."
                    }));

            // In every other case, render the consent form.
            default:
                return View(new AuthorizeViewModel
                {
                    ApplicationName = await applicationManager.GetLocalizedDisplayNameAsync(application),
                    Scope = request.Scope
                });
        }
    }

    [HttpGet("~/api/users/v1/connect/logout")]
    public IActionResult Logout()
    {
        return View();
    }

    [ActionName(nameof(Logout))]
    [HttpPost("~/api/users/v1/connect/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutPost()
    {
        // Ask ASP.NET Core Identity to delete the local and external cookies created
        // when the user agent is redirected from the external identity provider
        // after a successful authentication flow (e.g Google or Facebook).
        await signInManager.SignOutAsync();

        // Returning a SignOutResult will ask OpenIddict to redirect the user agent
        // to the post_logout_redirect_uri specified by the client application or to
        // the RedirectUri specified in the authentication properties if none was set.
        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = "/"
            });
    }

    /// <summary>
    /// This action is invoked when a POST is sent to the token endpoint as part of the OAuth 2.0
    /// authorization code or refresh token flows, to exchange an authorization code or a refresh token
    /// The IgnoreAntiforgeryToken attribute is used to disable the CSRF protection for this endpoint,
    /// as the endpoints is going to be called by an external client, which won't be able to provide an anti-forgery token.
    /// </summary>
    [HttpPost("~/api/users/v1/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal stored in the authorization code/refresh token.
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // Retrieve the user profile corresponding to the authorization code/refresh token.
            var user = await userManager.FindByIdAsync(
                result.Principal!.GetClaim(OpenIddictConstants.Claims.Subject)!);
            if (user is null)
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                            OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The token is no longer valid."
                    }));

            // Ensure the user is still allowed to sign in.
            if (!await signInManager.CanSignInAsync(user))
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                            OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The user is no longer allowed to sign in."
                    }));

            var identity = new ClaimsIdentity(result.Principal!.Claims,
                TokenValidationParameters.DefaultAuthenticationType,
                OpenIddictConstants.Claims.Name,
                OpenIddictConstants.Claims.Role);

            // Override the user claims present in the principal in case they
            // changed since the authorization code/refresh token was issued.
            identity.SetClaim(OpenIddictConstants.Claims.Subject, await userManager.GetUserIdAsync(user))
                .SetClaim(OpenIddictConstants.Claims.Email, await userManager.GetEmailAsync(user))
                .SetClaim(OpenIddictConstants.Claims.Name, $"{user.FirstName} {user.LastName}")
                .SetClaim(OpenIddictConstants.Claims.GivenName, user.FirstName)
                .SetClaim(OpenIddictConstants.Claims.FamilyName, user.LastName)
                .SetClaim(OpenIddictConstants.Claims.PreferredUsername, await userManager.GetUserNameAsync(user))
                .SetClaim("signup_date", user.DateCreated.ToString("o"))
                .SetClaims(OpenIddictConstants.Claims.Role, [.. await userManager.GetRolesAsync(user)]);

            identity.SetDestinations(GetDestinations);
            identity.SetAudiences("stickerlandia");

            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.PreferredUsername
                or OpenIddictConstants.Claims.GivenName or OpenIddictConstants.Claims.FamilyName:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject!.HasScope(OpenIddictConstants.Permissions.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject!.HasScope(OpenIddictConstants.Permissions.Scopes.Email))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject!.HasScope(OpenIddictConstants.Permissions.Scopes.Roles))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case "signup_date":
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject!.HasScope(OpenIddictConstants.Permissions.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}
