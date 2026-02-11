/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Stickerlandia.PrintService.Aspire.WireMock;

/// <summary>
/// Provides RSA key pair generation and management for OIDC testing.
/// </summary>
internal sealed class RsaKeyProvider : IDisposable
{
    private readonly RSA _rsa;

    public RsaSecurityKey SecurityKey { get; }

    public string KeyId { get; }

    public RsaKeyProvider()
    {
        _rsa = RSA.Create(2048);
        KeyId = Guid.NewGuid().ToString("N")[..16];
        SecurityKey = new RsaSecurityKey(_rsa) { KeyId = KeyId };
    }

    /// <summary>
    /// Gets the signing credentials for token generation.
    /// </summary>
    public SigningCredentials GetSigningCredentials()
        => new(SecurityKey, SecurityAlgorithms.RsaSha256);

    /// <summary>
    /// Generates JWKS JSON for the public key endpoint.
    /// </summary>
    public string GenerateJwksJson()
    {
        var parameters = _rsa.ExportParameters(false);
        var jwk = new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    kid = KeyId,
                    alg = "RS256",
                    n = Base64UrlEncoder.Encode(parameters.Modulus),
                    e = Base64UrlEncoder.Encode(parameters.Exponent)
                }
            }
        };
        return System.Text.Json.JsonSerializer.Serialize(jwk);
    }

    public void Dispose() => _rsa.Dispose();
}
