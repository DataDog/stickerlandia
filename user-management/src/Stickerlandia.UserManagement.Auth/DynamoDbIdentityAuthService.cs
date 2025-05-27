// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Collections.Immutable;
using System.Security.Claims;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Stickerlandia.UserManagement.Auth.DynamoDB;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.Auth;

/// <summary>
/// DynamoDB-specific implementation of IAuthService using AWS DynamoDB for persistence
/// </summary>
public class DynamoDbIdentityAuthService(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IOpenIddictApplicationManager applicationManager,
    ILogger<DynamoDbIdentityAuthService> logger,
    IOpenIddictScopeManager scopeManager)
    : IAuthService
{
    public async Task<ClaimsIdentity?> VerifyClient(string clientId)
    {
        var application = await applicationManager.FindByClientIdAsync(clientId);
        if (application == null)
        {
            logger.LogError("Client application {ClientId} not found", clientId);
            return null;
        }

        // Create a new ClaimsIdentity containing the claims that
        // will be used to create an id_token, a token or a code.
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        // Use the client_id as the subject identifier.
        identity.SetClaim(OpenIddictConstants.Claims.Subject, await applicationManager.GetClientIdAsync(application));
        identity.SetClaim(OpenIddictConstants.Claims.Name, await applicationManager.GetDisplayNameAsync(application));

        identity.SetDestinations(static claim => claim.Type switch
        {
            // Allow the "name" claim to be stored in both the access and identity tokens
            // when the "profile" scope was granted (by calling principal.SetScopes(...)).
            OpenIddictConstants.Claims.Name when claim.Subject.HasScope(OpenIddictConstants.Permissions.Scopes.Profile)
                => new[] { OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken },

            // Otherwise, only store the claim in the access tokens.
            _ => new[] { OpenIddictConstants.Destinations.AccessToken }
        });

        return identity;
    }

    public async Task<ClaimsIdentity?> VerifyPassword(string username, string password, ImmutableArray<string> scopes)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            OpenIddictConstants.Claims.Name, OpenIddictConstants.Claims.Role);
        IdentityUser? user = await userManager.FindByEmailAsync(username);
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
        var result = await signInManager.PasswordSignInAsync(user.UserName, password, false,
            false);
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

        var applicationUser = await userManager.FindByEmailAsync(user.Email);

        // Add Custom claims
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, applicationUser.Id.ToString()));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Audience, "Resource"));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Username, applicationUser.Id.ToString()));

        // Setting destinations of claims i.e. identity token or access token
        identity.SetDestinations(GetDestinations);

        return identity;
    }

    public async Task CreateIdentityFor(UserAccount userAccount, string password)
    {
        var identityUser = new IdentityUser { UserName = userAccount.Id.Value, Email = userAccount.EmailAddress };

        var result = await userManager.CreateAsync(identityUser, password);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create user {Username}: {Errors}", userAccount.Id, errors);
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        logger.LogInformation("Successfully created user {Username} with DynamoDB storage", userAccount.Id);
    }

    public Task EnsureStoreCreatedAsync()
    {
        return Task.CompletedTask;
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
                => new[] { OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken },

            OpenIddictConstants.Claims.Email or
            OpenIddictConstants.Claims.Role or
            OpenIddictConstants.Claims.PreferredUsername
                => new[] { OpenIddictConstants.Destinations.AccessToken },

            _ => Array.Empty<string>()
        };
    }
} 