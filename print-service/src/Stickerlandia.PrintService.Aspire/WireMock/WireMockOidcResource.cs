/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.IdentityModel.Tokens;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;

namespace Stickerlandia.PrintService.Aspire.WireMock;

/// <summary>
/// Custom Aspire resource that simulates OpenIddict OIDC discovery and JWKS endpoints using WireMock.
/// </summary>
internal sealed class WireMockOidcResource : Resource, IDisposable
{
    private WireMockServer? _server;
    private RsaKeyProvider? _keyProvider;

    public WireMockOidcResource(string name) : base(name)
    {
    }

    public string? BaseUrl => _server?.Url;

    public RsaKeyProvider? KeyProvider => _keyProvider;

    public void Start()
    {
        if (_server != null)
        {
            return;
        }

        _keyProvider = new RsaKeyProvider();
        _server = WireMockServer.Start();
        ConfigureEndpoints();

        Console.WriteLine($"WireMock OIDC Server started at {_server.Url}");
    }

    private void ConfigureEndpoints()
    {
        if (_server == null || _keyProvider == null)
        {
            return;
        }

        var baseUrl = _server.Url!;

        // OpenID Configuration (Discovery Document)
        _server.Given(
            Request.Create()
                .WithPath("/.well-known/openid-configuration")
                .UsingGet()
        )
        .RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(GenerateDiscoveryDocument(baseUrl))
        );

        // JWKS Endpoint
        _server.Given(
            Request.Create()
                .WithPath("/.well-known/jwks")
                .UsingGet()
        )
        .RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(_keyProvider.GenerateJwksJson())
        );

        // Alternative JWKS path (OpenIddict default)
        _server.Given(
            Request.Create()
                .WithPath("/connect/jwks")
                .UsingGet()
        )
        .RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(_keyProvider.GenerateJwksJson())
        );

        // Dev token generation endpoint for manual testing (e.g., Postman)
        _server.Given(
            Request.Create()
                .WithPath("/dev/token")
                .UsingGet()
        )
        .RespondWith(
            Response.Create()
                .WithCallback(request =>
                {
                    var userId = "test-user";
                    var rolesParam = "admin";

                    if (request.Query != null)
                    {
                        if (request.Query.TryGetValue("userId", out var userIdValues))
                        {
                            userId = userIdValues.FirstOrDefault() ?? userId;
                        }
                        if (request.Query.TryGetValue("roles", out var rolesValues))
                        {
                            rolesParam = rolesValues.FirstOrDefault() ?? rolesParam;
                        }
                    }

                    var roles = rolesParam.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var token = GenerateToken(userId, roles, baseUrl);

                    return new global::WireMock.ResponseMessage
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, global::WireMock.Types.WireMockList<string>>
                        {
                            ["Content-Type"] = new global::WireMock.Types.WireMockList<string>("application/json")
                        },
                        BodyData = new BodyData()
                        {
                            DetectedBodyType = BodyType.Json,
                            BodyAsJson = new { access_token = token, token_type = "Bearer", expires_in = 3600 }
                        }
                    };
                })
        );
    }

    private string GenerateToken(string userId, string[] roles, string issuer)
    {
        if (_keyProvider == null)
        {
            throw new InvalidOperationException("Key provider not initialized");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: "print-service",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: _keyProvider.GetSigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateDiscoveryDocument(string baseUrl)
    {
        var document = new
        {
            issuer = baseUrl,
            authorization_endpoint = $"{baseUrl}/connect/authorize",
            token_endpoint = $"{baseUrl}/connect/token",
            userinfo_endpoint = $"{baseUrl}/connect/userinfo",
            jwks_uri = $"{baseUrl}/.well-known/jwks",
            scopes_supported = new[] { "openid", "profile", "email", "roles" },
            response_types_supported = new[] { "code", "token", "id_token" },
            grant_types_supported = new[] { "authorization_code", "refresh_token", "client_credentials" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_basic", "client_secret_post" }
        };
        return JsonSerializer.Serialize(document);
    }

    public void Dispose()
    {
        _server?.Dispose();
        _keyProvider?.Dispose();
    }
}
