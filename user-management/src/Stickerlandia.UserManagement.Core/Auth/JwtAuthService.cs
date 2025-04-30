// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Stickerlandia.UserManagement.Core.Auth;

public class JwtAuthService : IAuthService
{
    private const string UserTypeClaimName = "role";
    private const string UserTierClaimName = "UserTier";
    private const string AccountAgeClaimName = "AccountAge";

    private readonly JwtConfiguration _configuration;

    public JwtAuthService(IOptions<JwtConfiguration> jwtConfiguration)
    {
        _configuration = jwtConfiguration.Value;
    }

    public string GenerateAuthToken(UserAccount account)
    {
        var issuer = _configuration.Issuer;
        var audience = _configuration.Audience;
        var key = Encoding.ASCII.GetBytes
            (_configuration.Key);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Id),
            new Claim(JwtRegisteredClaimNames.Email, account.EmailAddress),
            new Claim(UserTypeClaimName, account.AsAuthenticatedRole()),
            new Claim(UserTierClaimName, account.AccountTier.ToString()),
            new Claim(AccountAgeClaimName, account.AccountAge.ToString(CultureInfo.InvariantCulture))
        };

        Activity.Current?.AddTag("user.type", account.AccountType.ToString());
        Activity.Current?.AddTag("user.tier", account.AccountTier.ToString());
        Activity.Current?.AddTag("user.account_age", account.AccountAge);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials
            (new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha512Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public AuthorizedUserDetails? ValidateAuthToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration.Key);
        var parsedToken = token.Replace("Bearer ", "");

        try
        {
            tokenHandler.ValidateToken(parsedToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration.Issuer,
                ValidateAudience = true,
                ValidAudience = _configuration.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No tolerance for token expiration
            }, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
                return null;

            // Extract the account ID from the subject claim
            var accountIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub);
            if (accountIdClaim == null)
                return null;
            var roleClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == UserTypeClaimName);
            if (roleClaim == null)
                return null;
            var userTierClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == UserTierClaimName);
            if (userTierClaim == null)
                return null;

            return new AuthorizedUserDetails
            {
                AccountId = accountIdClaim.Value,
                Role = roleClaim.Value,
                UserTier = userTierClaim.Value
            };
        }
        catch
        {
            // Token validation failed - could be expired, invalid signature, etc.
            return null;
        }
    }
}