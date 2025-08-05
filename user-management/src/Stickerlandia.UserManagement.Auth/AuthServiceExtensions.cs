// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Stickerlandia.UserManagement.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddCoreAuthentication(this IServiceCollection services,
        Action<OpenIddictCoreBuilder> configuration, bool disableSsl)
    {
        services.AddOpenIddict()
            .AddCore(configuration)
            .AddServer(options =>
            {
                // Read explicit issuer from environment variable
                var explicitIssuer = Environment.GetEnvironmentVariable("OPENIDDICT_ISSUER");
                if (!string.IsNullOrEmpty(explicitIssuer))
                {
                    options.SetIssuer(new Uri(explicitIssuer));
                }
                
                // Enable the token endpoint.
                // Enable the authorization, logout, token and userinfo endpoints.
                options.SetAuthorizationEndpointUris("api/users/v1/connect/authorize")
                    .SetEndSessionEndpointUris("api/users/v1/connect/logout")
                    .SetTokenEndpointUris("api/users/v1/connect/token")
                    .SetUserInfoEndpointUris("api/users/v1/connect/userinfo") ;

                // Mark the "email", "profile" and "roles" scopes as supported scopes.
                options.RegisterScopes(OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.Roles);

                // Note: the sample uses the code and refresh token flows but you can enable
                // the other flows if you need to support implicit, password or client credentials.
                options.AllowAuthorizationCodeFlow()
                    .RequireProofKeyForCodeExchange()
                    .AllowRefreshTokenFlow();

                // Register the signing and encryption credentials.
                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                // Register the ASP.NET Core host and configure the ASP.NET Core options.
                if (disableSsl)
                    options.UseAspNetCore()
                        .DisableTransportSecurityRequirement()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableEndSessionEndpointPassthrough()
                        .EnableStatusCodePagesIntegration()
                        .EnableTokenEndpointPassthrough();
                else
                    options.UseAspNetCore()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableEndSessionEndpointPassthrough()
                        .EnableStatusCodePagesIntegration()
                        .EnableTokenEndpointPassthrough();
                
                options.AddEventHandler<OpenIddictServerEvents.HandleUserInfoRequestContext>(options =>
                    options.UseInlineHandler(context =>
                    {
                        if (context.Principal.HasScope(OpenIddictConstants.Permissions.Scopes.Profile))
                        {
                            context.Profile = context.Principal.GetClaim(OpenIddictConstants.Claims.Profile);
                            context.PreferredUsername =
                                context.Principal.GetClaim(OpenIddictConstants.Claims.PreferredUsername);
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
            });
        return services;
    }
}