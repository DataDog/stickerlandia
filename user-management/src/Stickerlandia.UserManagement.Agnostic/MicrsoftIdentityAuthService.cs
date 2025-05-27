// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.Agnostic;

public class MicrosoftIdentityAuthService(
    ILogger<MicrosoftIdentityAuthService> logger,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    UserManagementDbContext dbContext,
    SignInManager<PostgresUserAccount> signInManager,
    UserManager<PostgresUserAccount> userManager) : IAuthService
{
    public async Task<ClaimsIdentity?> VerifyClient(string clientId)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            OpenIddictConstants.Claims.Name, OpenIddictConstants.Claims.Role);

        var application = await applicationManager.FindByClientIdAsync(clientId) ??
                          throw new InvalidOperationException("The application cannot be found.");

        // Use the client_id as the subject identifier.
        identity.SetClaim(OpenIddictConstants.Claims.Subject,
            await applicationManager.GetClientIdAsync(application));
        identity.SetClaim(OpenIddictConstants.Claims.Name,
            await applicationManager.GetDisplayNameAsync(application));

        identity.SetDestinations(static claim => claim.Type switch
        {
            // Allow the "name" claim to be stored in both the access and identity tokens
            // when the "profile" scope was granted (by calling principal.SetScopes(...)).
            OpenIddictConstants.Claims.Name when claim.Subject.HasScope(OpenIddictConstants.Permissions.Scopes
                    .Profile)
                => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],

            // Otherwise, only store the claim in the access tokens.
            _ => [OpenIddictConstants.Destinations.AccessToken]
        });

        return identity;
    }

    public async Task<ClaimsIdentity?> VerifyPassword(string username, string password, ImmutableArray<string> scopes)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            OpenIddictConstants.Claims.Name, OpenIddictConstants.Claims.Role);
        PostgresUserAccount? user = await userManager.FindByEmailAsync(username);
        AuthenticationProperties properties = new();

        if (user == null)
            return null;

        // Check that the user can sign in and is not locked out.
        // If two-factor authentication is supported, it would also be appropriate to check that 2FA is enabled for the user
        if (!await signInManager.CanSignInAsync(user) ||
            (userManager.SupportsUserLockout && await userManager.IsLockedOutAsync(user)))
            // Return bad request is the user can't sign in
            return null;

        // Validate the username/password parameters and ensure the account is not locked out.
        var result = await signInManager.CheckPasswordSignInAsync(user, password, false);
        if (!result.Succeeded)
        {
            if (result.IsNotAllowed)
                return null;

            if (result.RequiresTwoFactor)
                return null;

            if (result.IsLockedOut)
                return null;
            else
                return null;
        }

        // The user is now validated, so reset lockout counts, if necessary
        if (userManager.SupportsUserLockout) await userManager.ResetAccessFailedCountAsync(user);

        //// Getting scopes from user parameters (TokenViewModel) and adding in Identity 
        identity.SetScopes(scopes);

        var resources = await scopeManager.ListResourcesAsync(scopes).ToListAsync();
        identity.SetResources(resources);

        // Add Custom claims
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Audience, "Resource"));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Username, user.Id));

        // Setting destinations of claims i.e. identity token or access token
        identity.SetDestinations(GetDestinations);

        return identity;
    }

    public async Task EnsureStoreCreatedAsync()
    {
        await dbContext.Database.MigrateAsync();
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        return claim.Type switch
        {
            OpenIddictConstants.Claims.Name or
                OpenIddictConstants.Claims.Subject
                => new[]
                {
                    OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken
                },

            _ => new[] { OpenIddictConstants.Destinations.AccessToken }
        };
    }
}