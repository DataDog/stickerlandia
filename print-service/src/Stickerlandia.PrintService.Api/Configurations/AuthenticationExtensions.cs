/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
        var audience = configuration["Authentication:Audience"] ?? "print-service";
        var requireHttpsMetadata = configuration.GetValue<bool>("Authentication:RequireHttpsMetadata", true);

        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = requireHttpsMetadata;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // For testing with WireMock over HTTP
        if (!requireHttpsMetadata)
        {
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
        }
    }

    private static void ConfigureSymmetricKey(JwtBearerOptions options, IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"]
            ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? "https://stickerlandia.local";
        var audience = configuration["Jwt:Audience"]
            ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? "print-service";
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
