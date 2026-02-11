/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1515,CA1063,CA2012,CA2000

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Stickerlandia.PrintService.IntegrationTest.Drivers;

namespace Stickerlandia.PrintService.IntegrationTest.Hooks;

[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollectionFixture : ICollectionFixture<TestSetupFixture>
{
}

public class TestSetupFixture : IDisposable
{
    public HttpClient HttpClient { get; init; }
    public DistributedApplication? App { get; init; }

    /// <summary>
    /// The WireMock OIDC server for generating test tokens.
    /// Use this to generate JWT tokens that will be accepted by the API.
    /// </summary>
    public WireMockOidcServer? OidcServer { get; init; }

    private const string ApiApplicationName = "api";

    public TestSetupFixture()
    {
        var drivenAdapter = Environment.GetEnvironmentVariable("DRIVEN") ?? "AWS";
        var drivingAdapter = Environment.GetEnvironmentVariable("DRIVING") ?? "AWS";
        // Force testing against real resources since Docker containers are not available
        var shouldTestAgainstRealResources = Environment.GetEnvironmentVariable("TEST_REAL_RESOURCES") == "true";

        if (!shouldTestAgainstRealResources)
        {
            // Start WireMock OIDC server before Aspire so we can configure the API to use it
            OidcServer = new WireMockOidcServer();

            // Set environment variables BEFORE creating the Aspire builder
            // These are read by Program.cs via AddEnvironmentVariables()
            Environment.SetEnvironmentVariable("Authentication__Authority", OidcServer.BaseUrl);
            Environment.SetEnvironmentVariable("Authentication__Mode", "OidcDiscovery");
            Environment.SetEnvironmentVariable("Authentication__Audience", "print-service");
            Environment.SetEnvironmentVariable("Authentication__RequireHttpsMetadata", "false");

            // Run all local resources with Aspire for testing
            var builder = DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Stickerlandia_PrintService_Aspire>()
                .GetAwaiter()
                .GetResult();

            builder.Configuration["DRIVING"] = drivingAdapter;
            builder.Configuration["DRIVEN"] = drivenAdapter;

            // Also set in configuration for WithAwsApi to read
            builder.Configuration["Authentication:Mode"] = "OidcDiscovery";
            builder.Configuration["Authentication:Authority"] = OidcServer.BaseUrl;
            builder.Configuration["Authentication:Audience"] = "print-service";
            builder.Configuration["Authentication:RequireHttpsMetadata"] = "false";

            if (drivenAdapter == "GCP")
            {
                builder.Configuration["PUBSUB_PROJECT_ID"] = "my-project-id";
                builder.Configuration["PUBSUB_EMULATOR_HOST"] = "[::1]:8432";
                builder.Configuration["ConnectionStrings:messaging"] = "my-project-id";
            }

            App = builder.BuildAsync().GetAwaiter().GetResult();

            App.StartAsync().GetAwaiter().GetResult();

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
            App.ResourceNotifications.WaitForResourceHealthyAsync(
                    ApiApplicationName,
                    cts.Token)
                .GetAwaiter().GetResult();

            // When Azure Functions is used, the API is not available immediately even when the container is healthy.
            Task.Delay(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();

            // Create HttpClient with cookie support for OAuth2.0 flows
            var tempHttpClient = App.CreateHttpClient(ApiApplicationName, "https");
            var baseAddress = tempHttpClient.BaseAddress;
            tempHttpClient.Dispose();

            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CheckCertificateRevocationList = true
            };

            HttpClient = new HttpClient(handler, true)
            {
                BaseAddress = baseAddress
            };
        }
        else
        {
            // Try to create HttpClient with real API, fallback to mock if not available
            HttpClient = CreateHttpClientWithFallback();
        }
    }

    private static IMessaging CreateMessagingWithFallback(string drivenAdapter)
    {
        try
        {
            // Try to create real messaging connection
            return MessagingProviderFactory.From(drivenAdapter,
                TestConstants.DefaultMessagingConnection(drivenAdapter));
        }
        catch (InvalidOperationException)
        {
            // Real messaging not available, use mock
            return new MockMessaging();
        }
        catch (ArgumentException)
        {
            // Invalid configuration, use mock
            return new MockMessaging();
        }
    }

    private static HttpClient CreateHttpClientWithFallback()
    {
        try
        {
            // Try to connect to real API first
            using var testClient = new HttpClient();
            testClient.Timeout = TimeSpan.FromSeconds(5);
            var response = testClient.GetAsync(new Uri(TestConstants.DefaultTestUrl)).GetAwaiter().GetResult();

            // If we get here, real API is available
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CheckCertificateRevocationList = true
            };

            return new HttpClient(handler, true)
            {
                BaseAddress = new Uri(TestConstants.DefaultTestUrl)
            };
        }
        catch (HttpRequestException)
        {
            // Real API not available, use mock
            var mockHandler = new MockHttpMessageHandler();
            return new HttpClient(mockHandler)
            {
                BaseAddress = new Uri(TestConstants.DefaultTestUrl)
            };
        }
        catch (TaskCanceledException)
        {
            // Timeout, use mock
            var mockHandler = new MockHttpMessageHandler();
            return new HttpClient(mockHandler)
            {
                BaseAddress = new Uri(TestConstants.DefaultTestUrl)
            };
        }
    }

    public void Dispose()
    {
        App?.StopAsync().GetAwaiter().GetResult();
        OidcServer?.Dispose();

        // Clean up environment variables
        Environment.SetEnvironmentVariable("Authentication__Authority", null);
        Environment.SetEnvironmentVariable("Authentication__Mode", null);
        Environment.SetEnvironmentVariable("Authentication__Audience", null);
        Environment.SetEnvironmentVariable("Authentication__RequireHttpsMetadata", null);

        GC.SuppressFinalize(this);
    }
}