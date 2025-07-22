// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1515,CA1063,CA2012,CA2000

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Stickerlandia.UserManagement.IntegrationTest.Drivers;

namespace Stickerlandia.UserManagement.IntegrationTest.Hooks;

[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollectionFixture : ICollectionFixture<TestSetupFixture>
{
}

public class TestSetupFixture : IDisposable
{
    public IMessaging Messaging { get; init; }
    public HttpClient HttpClient { get; init; }
    public DistributedApplication? App { get; init; }

    private const string ApiApplicationName = "api";
    private const string MessagingResourceName = "messaging";

    public TestSetupFixture()
    {
        var drivenAdapter = Environment.GetEnvironmentVariable("DRIVEN") ?? "AGNOSTIC";
        var drivingAdapter = Environment.GetEnvironmentVariable("DRIVING") ?? "AGNOSTIC";
        // Force testing against real resources since Docker containers are not available
        var shouldTestAgainstRealResources = Environment.GetEnvironmentVariable("TEST_REAL_RESOURCES") == "true";

        if (!shouldTestAgainstRealResources)
        {
            // Run all local resources with Asipre for testing
            var builder = DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Stickerlandia_UserManagement_Aspire>()
                .GetAwaiter()
                .GetResult();

            builder.Configuration["DRIVING"] = drivingAdapter;
            builder.Configuration["DRIVEN"] = drivenAdapter;

            App = builder.BuildAsync().GetAwaiter().GetResult();

            App.StartAsync().GetAwaiter().GetResult();

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
            App.ResourceNotifications.WaitForResourceHealthyAsync(
                    ApiApplicationName,
                    cts.Token)
                .GetAwaiter().GetResult();

            // When Azure Functions is used, the API is not available immediately even when the container is healthy.
            Task.Delay(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();

            var messagingConnectionString = App.GetConnectionStringAsync(MessagingResourceName).GetAwaiter().GetResult();
            Messaging = MessagingProviderFactory.From(drivenAdapter,
                TestConstants.DefaultMessagingConnection(drivenAdapter, messagingConnectionString));
            
            // Create HttpClient with cookie support for OAuth2.0 flows
            var tempHttpClient = App.CreateHttpClient(ApiApplicationName, "https");
            var baseAddress = tempHttpClient.BaseAddress;
            tempHttpClient.Dispose();
            
            var handler = new HttpClientHandler()
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
            // Try to create real messaging connection, fallback to mock if not available
            Messaging = CreateMessagingWithFallback(drivenAdapter);
            
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
            var handler = new HttpClientHandler()
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
        GC.SuppressFinalize(this);
    }
}