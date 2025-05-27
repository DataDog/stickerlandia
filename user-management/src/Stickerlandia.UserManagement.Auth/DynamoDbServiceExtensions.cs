// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;
using Stickerlandia.UserManagement.Auth.DynamoDB;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.Auth;

/// <summary>
/// Service extensions for configuring DynamoDB as the authentication persistence layer
/// </summary>
public static class DynamoDbServiceExtensions
{
    /// <summary>
    /// Configures authentication services using DynamoDB as the persistence provider
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddDynamoDbAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure AWS DynamoDB
        services.AddAWSService<IAmazonDynamoDB>();
        services.AddSingleton<DynamoDBContext>();
        
        // Configure DynamoDB-specific Identity stores
        services.AddTransient<IUserStore<IdentityUser>, DynamoDbUserStore>();
        services.AddTransient<IRoleStore<IdentityRole>, DynamoDbRoleStore>();
        
        // Configure DynamoDB-specific OpenIddict stores
        services.AddSingleton<IOpenIddictApplicationStore<DynamoDbApplication>, DynamoDbApplicationStore>();
        services.AddSingleton<IOpenIddictScopeStore<DynamoDbScope>, DynamoDbScopeStore>();
        
        // Configure Identity with DynamoDB stores
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
            options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
            options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
            options.ClaimsIdentity.EmailClaimType = OpenIddictConstants.Claims.Email;

            // Configure password requirements
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Configure lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // Configure user settings
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
        .AddDefaultTokenProviders();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                // Configure OpenIddict to use our custom DynamoDB stores
                options.SetDefaultApplicationEntity<DynamoDbApplication>()
                       .SetDefaultScopeEntity<DynamoDbScope>();
            })
            .AddServer(options =>
            {
                // Enable the token endpoint.
                options.SetAuthorizationEndpointUris("/authorize")
                    .SetTokenEndpointUris("/api/users/v1/login")
                    .SetIntrospectionEndpointUris("/introspection")
                    .SetUserInfoEndpointUris("/userinfo")
                    .SetEndSessionEndpointUris("/logout");

                // Enable the client credentials flow.
                options.AllowClientCredentialsFlow()
                    .AllowAuthorizationCodeFlow()
                    .AllowPasswordFlow()
                    .AllowImplicitFlow()
                    .AllowHybridFlow()
                    .AllowRefreshTokenFlow();
                
                // Expose all the supported claims in the discovery document.
                options.RegisterClaims("email", "issuer", "preferred_username", "profile", "updated_at");

                // Expose all the supported scopes in the discovery document.
                options.RegisterScopes("email", "profile");

                // Register the signing and encryption credentials.
                options.AddEphemeralEncryptionKey()
                    .AddEphemeralSigningKey();

                // Register the ASP.NET Core host and configure the ASP.NET Core options.
                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
                
                options.AddEventHandler<OpenIddictServerEvents.HandleUserInfoRequestContext>(options => options.UseInlineHandler(context =>
                {
                    if (context.Principal.HasScope(OpenIddictConstants.Permissions.Scopes.Profile))
                    {
                        context.Profile = context.Principal.GetClaim(OpenIddictConstants.Claims.Profile);
                        context.PreferredUsername = context.Principal.GetClaim(OpenIddictConstants.Claims.PreferredUsername);
                        context.Claims[OpenIddictConstants.Claims.UpdatedAt] = long.Parse(
                            context.Principal.GetClaim(OpenIddictConstants.Claims.UpdatedAt)!,
                            NumberStyles.Number, CultureInfo.InvariantCulture);
                    }

                    if (context.Principal.HasScope(OpenIddictConstants.Scopes.Email))
                    {
                        context.Email = context.Principal.GetClaim(OpenIddictConstants.Claims.Email);
                        context.EmailVerified = false;
                    }

                    return default;
                }));
            })
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance.
                options.UseLocalServer();

                // Register the ASP.NET Core host.
                options.UseAspNetCore();

                // Enable authorization entry validation, which is required to be able
                // to reject access tokens retrieved from a revoked authorization code.
                options.EnableAuthorizationEntryValidation();
            });

        // Register the DynamoDB-specific authentication service
        services.AddScoped<IAuthService, DynamoDbIdentityAuthService>();
        
        // Add the hosted service for DynamoDB initialization
        services.AddHostedService<DynamoDbAuthenticationWorker>();

        return services;
    }
} 