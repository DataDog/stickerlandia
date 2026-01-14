/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

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
