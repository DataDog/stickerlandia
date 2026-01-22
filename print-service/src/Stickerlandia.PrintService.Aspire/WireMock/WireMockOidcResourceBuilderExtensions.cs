/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

#pragma warning disable CA2000 // Resource lifecycle is managed by Aspire host

namespace Stickerlandia.PrintService.Aspire.WireMock;

internal static class WireMockOidcResourceBuilderExtensions
{
    /// <summary>
    /// Adds a WireMock OIDC server resource to the distributed application.
    /// </summary>
    public static IResourceBuilder<WireMockOidcResource> AddWireMockOidcServer(
        this IDistributedApplicationBuilder builder,
        string name = "oidc-server")
    {
        var resource = new WireMockOidcResource(name);

        // Start the server immediately so we know the URL
        resource.Start();

        return builder.AddResource(resource);
    }

    /// <summary>
    /// Configures a project to use the WireMock OIDC server for authentication.
    /// </summary>
    public static IResourceBuilder<TDestination> WithOidcAuthentication<TDestination>(
        this IResourceBuilder<TDestination> builder,
        IResourceBuilder<WireMockOidcResource> oidcServer,
        string audience = "print-service")
        where TDestination : IResourceWithEnvironment
    {
        var resource = oidcServer.Resource;

        if (resource.BaseUrl == null)
        {
            throw new InvalidOperationException("WireMock OIDC server has not been started");
        }

        return builder
            .WithEnvironment("Authentication__Mode", "OidcDiscovery")
            .WithEnvironment("Authentication__Authority", resource.BaseUrl)
            .WithEnvironment("Authentication__Audience", audience)
            .WithEnvironment("Authentication__RequireHttpsMetadata", "false");
    }
}
