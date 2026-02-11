/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Stickerlandia.PrintService.JwtGenerator;

internal static class JwtTokenGenerator
{
    /// <summary>
    /// Generates a JWT token using symmetric key (HS256) - for backward compatibility.
    /// </summary>
    public static string GenerateToken(
        string userId,
        string[] roles,
        string signingKey,
        string issuer = "https://stickerlandia.local",
        string audience = "print-service")
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        return GenerateTokenInternal(userId, roles, credentials, issuer, audience);
    }

    /// <summary>
    /// Generates a JWT token using RSA key (RS256) - for OIDC testing with WireMock.
    /// </summary>
    public static string GenerateRsaToken(
        string userId,
        string[] roles,
        RsaKeyProvider keyProvider,
        string issuer,
        string audience = "print-service")
    {
        var credentials = keyProvider.GetSigningCredentials();
        return GenerateTokenInternal(userId, roles, credentials, issuer, audience);
    }

    private static string GenerateTokenInternal(
        string userId,
        string[] roles,
        SigningCredentials credentials,
        string issuer,
        string audience)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
