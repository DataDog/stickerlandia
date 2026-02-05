/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

#pragma warning disable CA5400

namespace Stickerlandia.PrintService.Api.Configurations;

internal static class AuthenticationExtensions
{
    public static IServiceCollection AddPrintServiceAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authMode = configuration.GetValue<string>("Authentication:Mode") ?? "SymmetricKey";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (authMode.Equals("OidcDiscovery", StringComparison.OrdinalIgnoreCase))
                {
                    ConfigureOidcDiscovery(options, configuration);
                }
                else
                {
                    ConfigureSymmetricKey(options, configuration);
                }
            })
            .AddScheme<AuthenticationSchemeOptions, PrinterKeyAuthenticationHandler>(
                PrinterKeyAuthenticationHandler.SchemeName,
                _ => { });

        services.AddAuthorization();
        return services;
    }

    private static void ConfigureOidcDiscovery(JwtBearerOptions options, IConfiguration configuration)
    {
        var authority = configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication:Authority is required for OIDC mode");
        var audience = configuration["Authentication:Audience"] ?? "stickerlandia";
        var requireHttpsMetadata = configuration.GetValue<bool>("Authentication:RequireHttpsMetadata", true);

        // MetadataAddress allows using an internal URL for OIDC discovery (e.g., Docker network)
        // while validating against the external issuer URL in the token
        var metadataAddress = configuration["Authentication:MetadataAddress"];

        // Ensure authority has trailing slash to match OpenIddict's issuer format (RFC 3986)
        if (!authority.EndsWith('/'))
        {
            authority += "/";
        }

        options.Audience = audience;
        options.RequireHttpsMetadata = requireHttpsMetadata;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // If a separate metadata address is configured, use it for all OIDC fetching
        // This is needed in Docker where internal URLs differ from external issuer URLs
        if (!string.IsNullOrEmpty(metadataAddress))
        {
            if (!metadataAddress.EndsWith('/'))
            {
                metadataAddress += "/";
            }

            using var httpHandler = new HttpClientHandler();
            if (!requireHttpsMetadata)
            {
                httpHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }

            using var httpClient = new HttpClient(httpHandler);
            var internalJwksUrl = metadataAddress + ".well-known/jwks";

            // Use IssuerSigningKeyResolver to dynamically fetch keys from internal URL
            options.TokenValidationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                // Fetch JWKS from internal URL synchronously (cached by HttpClient)
                var jwksResponse = httpClient.GetStringAsync(new Uri(internalJwksUrl)).GetAwaiter().GetResult();
                var jwks = new JsonWebKeySet(jwksResponse);
                return jwks.Keys;
            };
        }
        else
        {
            options.Authority = authority;

            // For testing with WireMock over HTTP
            if (!requireHttpsMetadata)
            {
                options.BackchannelHttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
            }
        }
    }

    private static void ConfigureSymmetricKey(JwtBearerOptions options, IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"]
            ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? "https://stickerlandia.local";
        var audience = configuration["Jwt:Audience"]
            ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? "stickerlandia";
        var signingKey = configuration["Jwt:SigningKey"]
            ?? "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    }
}
