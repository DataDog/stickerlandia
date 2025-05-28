// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Globalization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;

namespace Stickerlandia.UserManagement.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddCoreAuthentication(this IServiceCollection services,
        Action<OpenIddictCoreBuilder> configuration, bool enableSsl)
    {
        services.AddOpenIddict()
            .AddCore(configuration)
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
                if (enableSsl)
                    options.UseAspNetCore()
                        .EnableTokenEndpointPassthrough();
                else
                    options.UseAspNetCore()
                        .DisableTransportSecurityRequirement()
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

                // Enable authorization entry validation, which is required to be able
                // to reject access tokens retrieved from a revoked authorization code.
                options.EnableAuthorizationEntryValidation();
            });

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        return services;
    }
}