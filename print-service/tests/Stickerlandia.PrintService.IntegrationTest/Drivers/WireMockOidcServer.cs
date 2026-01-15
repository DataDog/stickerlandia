/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

#pragma warning disable CA1515 // Public types in test assembly need to be accessible from test fixture
#pragma warning disable CA1056 // Using string for URL as WireMock returns string

using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Stickerlandia.PrintService.IntegrationTest.Drivers;

/// <summary>
/// WireMock server that simulates OpenIddict OIDC discovery and JWKS endpoints.
/// </summary>
public sealed class WireMockOidcServer : IDisposable
{
    private readonly WireMockServer _server;
    private readonly RsaKeyProvider _keyProvider;

    public string BaseUrl => _server.Url!;

    /// <summary>
    /// The issuer URL with trailing slash to match OpenIddict's format (RFC 3986).
    /// This must match what AuthenticationExtensions expects.
    /// </summary>
    public string Issuer => BaseUrl.EndsWith('/') ? BaseUrl : BaseUrl + "/";

    public RsaKeyProvider KeyProvider => _keyProvider;

    public WireMockOidcServer()
    {
        _keyProvider = new RsaKeyProvider();
        _server = WireMockServer.Start();
        ConfigureEndpoints();
    }

    private void ConfigureEndpoints()
    {
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
                .WithBody(GenerateDiscoveryDocument())
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
    }

    private string GenerateDiscoveryDocument()
    {
        // Use Issuer (with trailing slash) for consistency with token validation
        var document = new
        {
            issuer = Issuer,
            authorization_endpoint = $"{BaseUrl}/connect/authorize",
            token_endpoint = $"{BaseUrl}/connect/token",
            userinfo_endpoint = $"{BaseUrl}/connect/userinfo",
            jwks_uri = $"{BaseUrl}/.well-known/jwks",
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
        _server.Dispose();
        _keyProvider.Dispose();
    }
}
